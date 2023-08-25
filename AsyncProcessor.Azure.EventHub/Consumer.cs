using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.EventHub.Configuration;

// TODO: Add timer to update checkpoint
// TODO: Enable/Disable timer
// TODO: Ensure update to checkpoint when pausing and detaching

namespace AsyncProcessor.Azure.EventHub
{
    /// <summary>
    /// Consumer to receive messages from an event hub
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <remarks>
    /// For performance reasons, a EventProcessorClient is used instead of the a EventHubConsumerClient.
    /// It is stated that the EventProcessClient is designed for production usage while the EventHubConsumerClient is for PoC.
    /// See: https://devblogs.microsoft.com/azure-sdk/eventhubs-clients/
    /// 
    /// ** Important Notice **
    /// With EventHub each event stored for a specific period of time (ie 7 days) wether the event has been processed or not.
    /// Further more, when starting a client, all the events could be re-processed.
    /// To avoid the re-processing of events via a specific client (within a consumer group), azure uses BlobStorage to store a small file to denote that
    /// the event has been processed by a client within a given consumer group.
    /// 
    /// In specific evironments (dev/stage), the existence of a blob storage may not be present.  A Mocked blob storage client will be substituted.
    /// However, since the mocked client will be volatile, when the app is restarted, it will reprocess all events
    /// 
    /// See: 
    /// https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-features#checkpointing
    /// https://stackoverflow.com/questions/58978369/checkpointing-with-eventprocessorclient-with-the-new-net-sdk-azure-messaging-ev
    /// </remarks>
    public class Consumer<TMessage> :  IConsumer<TMessage>, IDisposable
    {
        private object _semaphoreLock = new object();
        private bool _disposedValue = false;
        private string _subscribedTo = null;
        private ProcessEventArgs _lastEventArgs;

        private readonly ILogger _logger;
        private readonly ConsumerSettings _settings;
        private readonly EventProcessorClient _client;
        private readonly System.Timers.Timer _timer;


        public Consumer(ILogger<Consumer<TMessage>> logger,
                        IOptions<ConsumerSettings> settings)
            : this(logger, settings?.Value)
        { }

        public Consumer(ILogger<Consumer<TMessage>> logger,
                        ConsumerSettings settings)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this._settings = settings ??
                throw new ArgumentNullException(nameof(settings));

            this._client = CreateClient(settings);
            this._timer = CreateTimer(settings.CheckpointStore.CheckpointIntervalInSeconds);


            // Set Default Delegate, just in case
            this.ProcessError = this.ProcessErrorDefault;
        }


        #region Consumer
        public Func<IMessageEvent, Task> ProcessMessage { get; set; }

        public Func<IErrorEvent, Task> ProcessError { get; set; }

        public TMessage GetMessage(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            var args = MessageEvent.ParseArgs(messageEvent);
            var message = args.Data;
            var json = Encoding.UTF8.GetString(message.EventBody);
            return Json.Deserialize<TMessage>(json);
        }
        #endregion


        #region Subscription Management
        public async Task Attach(string topic, CancellationToken cancellationToken = default)
        {
            // Ensure that topic is the same as the event hub, if supplied.
            if (!String.IsNullOrWhiteSpace(topic) &&
                !this._client.EventHubName.Equals(topic.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("When topic is supplied, it must match Azure Event Hub name");

            this._client.ProcessEventAsync += this.HandleClientProcessEvent;
            this._client.ProcessErrorAsync += this.HandleClientProcessError;
            this._subscribedTo = topic;
            await Resume(cancellationToken);
        }

        public async Task Attach(string topic, string subscription, CancellationToken cancellationToken = default)
        {
            await this.Attach(topic, cancellationToken);
        }

        public async Task Detach(CancellationToken cancellationToken = default)
        {
            await Pause(cancellationToken);
            this._client.ProcessEventAsync -= this.HandleClientProcessEvent;
            this._client.ProcessErrorAsync -= this.HandleClientProcessError;
        }

        public async Task Pause(CancellationToken cancellationToken = default)
        {
            if (this._client.IsRunning)
                await this._client.StopProcessingAsync(cancellationToken);
        }

        public async Task Resume(CancellationToken cancellationToken = default)
        {
            if (!this._client.IsRunning)
                await this._client.StartProcessingAsync(cancellationToken);
        }
        #endregion


        #region Message Management
        public bool IsMessageManagementSupported { get => false; }

        public Task AcknowledgeMessage(IMessageEvent messageEvent)
        {
            return Task.CompletedTask;
        }


        public Task DenyAcknowledgement(IMessageEvent messageEvent,
                                        bool requeue = true)
        {
            return Task.CompletedTask;
        }
        #endregion


        #region Dispose
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    this.Detach().GetAwaiter().GetResult();
                    this.ProcessMessage = null;
                    this.ProcessError = null;
                }

                _disposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Event Processor Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.eventprocessorclient?view=azure-dotnet
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private EventProcessorClient CreateClient(ConsumerSettings settings)
        {
            EventProcessorClient client = null;

            var storageClient = CreateStorageClient(settings.CheckpointStore);

            if (String.IsNullOrWhiteSpace(this._settings.EventHub))
                client = new EventProcessorClient(storageClient,
                                                  this._settings.ConsumerGroup,
                                                  this._settings.ConnectionString);
            else
                client = new EventProcessorClient(storageClient,
                                                  this._settings.ConsumerGroup,
                                                  this._settings.ConnectionString,
                                                  this._settings.EventHub);

            return client;
        }


        private BlobContainerClient CreateStorageClient(CheckpointSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new BlobContainerClient(settings.StorageConnectionString, settings.BlobContainerName);
        }


        private System.Timers.Timer CreateTimer(int interval)
        {
            System.Timers.Timer timer = new System.Timers.Timer(interval * 1000);
            timer.Elapsed += async (x, y) => { await this.IssueCheckpoint(); };
            timer.AutoReset = true;
            return timer;
        }



        private async Task HandleClientProcessEvent(ProcessEventArgs processEventArgs)
        {
            if (ProcessMessage != null)
            {
                if (!this._timer.Enabled)
                    this._timer.Start();

                this._lastEventArgs = processEventArgs;
                await ProcessMessage(new MessageEvent(processEventArgs));
            }
        }

        private async Task HandleClientProcessError(ProcessErrorEventArgs processErrorEventArgs)
        {
            if (ProcessError != null)
            {
                await ProcessError(new ErrorEvent(processErrorEventArgs));
            }
        }

        /// <summary>
        /// Default process for handling an error
        /// </summary>
        /// <param name="errorEvent"></param>
        /// <returns></returns>
        protected virtual Task ProcessErrorDefault(IErrorEvent errorEvent)
        {
            this._logger.LogError(errorEvent.Exception, "Error while processing message on Event Hub: {0}", this._subscribedTo);
            return Task.CompletedTask;
        }

        private async Task IssueCheckpoint()
        {
            try
            {
                this._logger.LogInformation("Issuing a Checkpoint on Event Hub {0}", this._client.EventHubName);
                await this._lastEventArgs.UpdateCheckpointAsync();
                this._timer.Stop();
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error while issuing a Checkpoint on Event Hub {0}", this._client.EventHubName);
            }
        }
    }
}
