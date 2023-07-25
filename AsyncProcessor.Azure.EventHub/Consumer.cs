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
    /// Consumer to receive messages from a queue service
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
        private bool disposedValue = false;
        private ProcessEventArgs LastEventArgs;

        private readonly ILogger Logger;
        private readonly ConsumerSettings Settings;
        private readonly EventProcessorClient Client;


        public Consumer(ILogger<Consumer<TMessage>> logger,
                        IOptions<ConsumerSettings> settings)
            : this(logger, settings?.Value)
        { }

        public Consumer(ILogger<Consumer<TMessage>> logger,
                        ConsumerSettings settings)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this.Settings = settings ??
                throw new ArgumentNullException(nameof(settings));

            this.Client = CreateClient(settings);

            // Set Default Delegate, just in case
            this.OnErrorReceived = this.OnErrorReceivedDefault;
        }


        #region Consumer
        public Func<IMessageEvent, Task> OnMessageReceived { get; set; }

        public Func<IErrorEvent, Task> OnErrorReceived { get; set; }

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
        public async Task Attach(string topic)
        {
            // Ensure that topic is the same as the event hub, if supplied.
            if (!String.IsNullOrWhiteSpace(topic) &&
                !this.Client.EventHubName.Equals(topic.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("When topic is supplied, it must match Azure Event Hub name");

            this.Client.ProcessEventAsync += this.ExecuteProcessEvent;
            this.Client.ProcessErrorAsync += this.ExecuteProcessError;
            await Resume();
        }

        public async Task Attach(string topic, string subscription)
        {
            await this.Attach(topic);
        }

        public async Task Detach()
        {
            await Pause();
            this.Client.ProcessEventAsync -= this.ExecuteProcessEvent;
            this.Client.ProcessErrorAsync -= this.ExecuteProcessError;
        }

        public async Task Pause()
        {
            if (this.Client.IsRunning)
                await this.Client.StopProcessingAsync();
        }

        public async Task Resume()
        {
            if (!this.Client.IsRunning)
                await this.Client.StartProcessingAsync();
        }
        #endregion


        #region Message Management
        public async Task AcknowledgeMessage(IMessageEvent messageEvent)
        {
        }


        public async Task DenyAcknowledgement(IMessageEvent messageEvent,
                                              bool requeue = true)
        {
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Detach().GetAwaiter().GetResult();
                    this.OnMessageReceived = null;
                    this.OnErrorReceived = null;
                }

                disposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Event Processor Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.eventprocessorclient?view=azure-dotnet
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private EventProcessorClient CreateClient(ConsumerSettings settings)
        {
            EventProcessorClient client = null;

            var storageClient = CreateStorageClient(settings.CheckpointSettings);

            if (String.IsNullOrWhiteSpace(this.Settings.EventHub))
                client = new EventProcessorClient(storageClient,
                                                  this.Settings.ConsumerGroup,
                                                  this.Settings.ConnectionString);
            else
                client = new EventProcessorClient(storageClient,
                                                  this.Settings.ConsumerGroup,
                                                  this.Settings.ConnectionString,
                                                  this.Settings.EventHub);

            return client;
        }


        private BlobContainerClient CreateStorageClient(CheckpointSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new BlobContainerClient(settings.StorageConnectionString, settings.BlobContainerName);
        }


        private async Task ExecuteProcessEvent(ProcessEventArgs processEventArgs)
        {
            if (OnMessageReceived != null)
            {
                this.LastEventArgs = processEventArgs;
                await OnMessageReceived(new MessageEvent(processEventArgs));
            }
        }

        private async Task ExecuteProcessError(ProcessErrorEventArgs processErrorEventArgs)
        {
            if (OnErrorReceived != null)
            {
                await OnErrorReceived(new ErrorEvent(processErrorEventArgs));
            }
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="loadPostingMessage"></param>
        /// <returns></returns>
        protected virtual async Task OnErrorReceivedDefault(IErrorEvent errorEvent)
        {
            try
            {
                ProcessErrorEventArgs args = ErrorEvent.ParseArgs(errorEvent);
                this.Logger.LogError(args.Exception, "Error while processing message on partition: {0}", args.PartitionId);
            }

            catch 
            { }
        }
    }
}
