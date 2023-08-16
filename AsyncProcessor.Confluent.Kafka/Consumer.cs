using System;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Confluent.Kafka.Configuration;
using AsyncProcessor.Confluent.Kafka.Services;
using static AsyncProcessor.Confluent.Kafka.Services.ProcessService;

namespace AsyncProcessor.Confluent.Kafka
{
    /// <summary>
    /// Consumer to receive messages from a Kafka Event hub
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <remarks>
    /// According to Confluent documentation, the consumer uses a looping mechanism to look for and consume messages off the hub
    /// </remarks>
    public class Consumer<TMessage> :  IConsumer<TMessage>, IDisposable
    {
        private bool DisposedValue = false;
        private string SubscribedTo = null;
        private CancellationTokenSource ProcessServiceCancellationTokenSource;
        private IProcessService _clientProcessService;

        private readonly ILogger _logger;
        private readonly ConsumerSettings _settings;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConsumer<Ignore, string> _client;


        public Consumer(ILogger<Consumer<TMessage>> logger,
                        IOptions<ConsumerSettings> settings,
                        IServiceScopeFactory serviceScopeFactory)
            : this(logger, settings?.Value, serviceScopeFactory)
        { }

        public Consumer(ILogger<Consumer<TMessage>> logger,
                        ConsumerSettings settings,
                        IServiceScopeFactory serviceScopeFactory)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this._settings = settings ??
                throw new ArgumentNullException(nameof(settings));

            this._serviceScopeFactory = serviceScopeFactory ??
                throw new ArgumentNullException(nameof(serviceScopeFactory));

            this._client = CreateClient(settings);

            // Set Default Delegate, just in case
            this.ProcessError = this.ProcessErrorDefault;
        }


        #region Consumer
        public Func<IMessageEvent, Task> ProcessMessage { get; set; }

        public Func<IErrorEvent, Task> ProcessError { get; set; }

        public TMessage GetMessage(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            var result = MessageEvent.ParseResult(messageEvent);
            var message = result.Message;
            var json = message.Value;
            return Json.Deserialize<TMessage>(json);
        }
        #endregion


        #region Subscription Management
        public async Task Attach(string topic)
        {
            this._client.Subscribe(topic);
            this.SubscribedTo = topic;
            await Resume();
        }

        public async Task Attach(string topic, string subscription)
        {
            await this.Attach(topic);
        }

        public async Task Detach()
        {
            await Pause();
            this._client.Close();
        }

        public Task Pause()
        {
            this.ProcessServiceCancellationTokenSource.Cancel();
            this._clientProcessService.ProcessEvent -= this.ExecuteProcessEvent;
            this._clientProcessService.ProcessError -= this.ExecuteProcessError;

            return Task.CompletedTask;
        }

        public async Task Resume()
        {
            this.ProcessServiceCancellationTokenSource = new CancellationTokenSource();

            // Since the Kafka client uses a polling mechanism, we need to run that mechanism in the background to prevent any blocking operations
            // Using Scope Services, this will create a background running Task (ie thread) without the thread managment
            // This approach allows the worker process to use Subscription Management calls to pause/cancel the polling operation
            using (var scope = this._serviceScopeFactory.CreateScope())
            {
                this._clientProcessService = scope.ServiceProvider.GetRequiredService<IProcessService>();
                this._clientProcessService.ProcessEvent += this.ExecuteProcessEvent;
                this._clientProcessService.ProcessError += this.ExecuteProcessError;

                await this._clientProcessService.ConsumeEvents(this._client, this.ProcessServiceCancellationTokenSource.Token);
            }
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
            if (!DisposedValue)
            {
                if (disposing)
                {
                    this.Detach().GetAwaiter().GetResult();
                    this.ProcessMessage = null;
                    this.ProcessError = null;
                    this._client.Dispose();
                }

                DisposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Kafka Consumer Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>

        private IConsumer<Ignore, string> CreateClient(ConnectionSettings settings)
        {
            var config = new ConsumerConfig(settings.ConnectionProperties);
            var builder = new ConsumerBuilder<Ignore, string>(config);

            return builder.Build();
        }



        private async Task ExecuteProcessEvent(ConsumeResult<Ignore, string> result)
        {
            if (ProcessMessage != null)
            {
                await ProcessMessage(new MessageEvent(result));
            }
        }

        private async Task ExecuteProcessError(Error error)
        {
            if (ProcessError != null)
            {
                await ProcessError(new ErrorEvent(this._client, error));
            }
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="loadPostingMessage"></param>
        /// <returns></returns>
        protected virtual Task ProcessErrorDefault(IErrorEvent errorEvent)
        {
            this._logger.LogError(errorEvent.Exception, "Error while processing message on Topic: {0}", this.SubscribedTo);
            return Task.CompletedTask;
        }
    }
}
