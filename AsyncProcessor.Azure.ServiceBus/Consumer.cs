using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Asserts;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.ServiceBus.Configuration;
using System.Threading;


namespace AsyncProcessor.Azure.ServiceBus
{
    /// <summary>
    /// Consumer to receive messages from a queue service
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <remarks>
    /// For performance reasons, a ServiceBusProcessor is used instead of the a ServiceBusReceiver.
    /// The ServiceBusProcessor can handle multiple concurrent message processing.  Where as the ServiceBusReceiver can only had a single message.
    /// The number of concurrent messages that can be processed simultaneously is controlled via a configurable setting
    /// </remarks>
    public class Consumer<TMessage> :  IConsumer<TMessage>, IDisposable
    {
        private bool _disposedValue = false;
        private ServiceBusProcessor _receiver = null;
        private string _subscribedTo = null;

        private readonly ILogger _logger;
        private readonly ConsumerSettings _settings;
        private readonly ServiceBusClient _client;

        private Func<IMessageEvent, Task> _processMessage;
        private Func<IErrorEvent, Task> _processError;


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

            // Set Default Delegate, just in case
            // this.ProcessError += this.HandleProcessErrorDefault;
        }


        #region Consumer
        public event Func<IMessageEvent, Task> ProcessMessage
        {
            add
            {
                Argument.AssertNotNull(value, nameof(ProcessMessage));
                Argument.AssertEventHandlerNotAssigned(this._processMessage, default, nameof(ProcessMessage));

                this._processMessage = value;
            }

            remove
            {
                Argument.AssertNotNull(value, nameof(ProcessMessage));
                Argument.AssertSameEventHandlerAssigned(this._processMessage, value, nameof(ProcessMessage));

                this._processMessage = default;
            }
        }


        public event Func<IErrorEvent, Task> ProcessError
        {
            add
            {
                Argument.AssertNotNull(value, nameof(ProcessError));
                Argument.AssertEventHandlerNotAssigned(this._processError, default, nameof(ProcessError));

                this._processError = value;
            }

            remove
            {
                Argument.AssertNotNull(value, nameof(ProcessError));
                Argument.AssertSameEventHandlerAssigned(this._processError, value, nameof(ProcessError));

                this._processError = default;
            }
        }

        public TMessage GetMessage(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            var args = MessageEvent.ParseArgs(messageEvent);
            var message = args.Message;
            var json = Encoding.UTF8.GetString(message.Body);
            return Json.Deserialize<TMessage>(json);
        }
        #endregion


        #region Subscription Management
        public async Task Attach(string topic, CancellationToken cancellationToken = default)
        {
            if (this._receiver == null ||
                this._receiver.IsClosed)
            {
                var options = CreateProcessorOptions();
                this._receiver = this._client.CreateProcessor(topic, options);
                this._receiver.ProcessMessageAsync += this.HandleClientProcessMessage;
                this._receiver.ProcessErrorAsync += this.HandleClientProcessError;
                this._subscribedTo = topic;
                await Resume(cancellationToken);
            }
        }

        public async Task Attach(string topic, string subscription, CancellationToken cancellationToken = default)
        {
            if (this._receiver == null ||
                this._receiver.IsClosed)
            {
                var options = CreateProcessorOptions();
                this._receiver = this._client.CreateProcessor(topic, subscription, options);
                this._receiver.ProcessMessageAsync += this.HandleClientProcessMessage;
                this._receiver.ProcessErrorAsync += this.HandleClientProcessError;
                this._subscribedTo = topic;
                await Resume(cancellationToken);
            }
        }

        public async Task Detach(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null &&
                !this._receiver.IsClosed)
            {
                await Pause(cancellationToken);
                this._receiver.ProcessMessageAsync -= this.HandleClientProcessMessage;
                this._receiver.ProcessErrorAsync -= this.HandleClientProcessError;
                await this._receiver.DisposeAsync();
            }
        }

        public async Task Pause(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null)
                await this._receiver.StopProcessingAsync(cancellationToken);
        }

        public async Task Resume(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null)
                await this._receiver.StartProcessingAsync(cancellationToken);
        }
        #endregion


        #region Message Management
        public bool IsMessageManagementSupported { get => true; }

        public async Task AcknowledgeMessage(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            if (this._receiver != null &&
                this._receiver.ReceiveMode == ServiceBusReceiveMode.ReceiveAndDelete)
                return;

            if (messageEvent != null)
            {
                var processMessageEvent = MessageEvent.ParseArgs(messageEvent);
                await processMessageEvent.CompleteMessageAsync(processMessageEvent.Message);
            }
        }


        public async Task DenyAcknowledgement(IMessageEvent messageEvent,
                                              bool requeue = true)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            if (this._receiver != null &&
                this._receiver.ReceiveMode == ServiceBusReceiveMode.ReceiveAndDelete)
                return;

            var processMessageEvent = MessageEvent.ParseArgs(messageEvent);

            if (requeue)
                await processMessageEvent.AbandonMessageAsync(processMessageEvent.Message);
            else
                await processMessageEvent.DeadLetterMessageAsync(processMessageEvent.Message);
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
                    this._client.DisposeAsync().GetAwaiter().GetResult();

                    this._processMessage = default;
                    this._processError = default;
                }

                _disposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Service Bus Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private ServiceBusClient CreateClient(ConnectionSettings settings)
        {
            return new ServiceBusClient(this._settings.ConnectionString);
        }


        private async Task HandleClientProcessMessage(ProcessMessageEventArgs processMessageEventArgs)
        {
            if (this._processMessage != null)
            {
                await this._processMessage(new MessageEvent(processMessageEventArgs));
            }
        }

        private async Task HandleClientProcessError(ProcessErrorEventArgs processErrorEventArgs)
        {
            if (this._processError != default)
            {
                await this._processError(new ErrorEvent(processErrorEventArgs));
            }
        }



        private ServiceBusReceiverOptions CreateReceiverOptions()
        {
            return new ServiceBusReceiverOptions()
            {
                PrefetchCount = this._settings.PrefetchCount,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                SubQueue = SubQueue.None,
            };
        }

        private ServiceBusProcessorOptions CreateProcessorOptions()
        {
            return new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = this._settings.ConcurrentDispatch,
                PrefetchCount = this._settings.PrefetchCount,
                ReceiveMode = this._settings.ReceiveMode,
            };
        }

        /// <summary>
        /// Default process for handling an error
        /// </summary>
        /// <param name="errorEvent"></param>
        /// <returns></returns>
        protected virtual Task HandleProcessErrorDefault(IErrorEvent errorEvent)
        {
            ProcessErrorEventArgs args = ErrorEvent.ParseArgs(errorEvent);

            // Do not log if message was locked
            bool isMessageLockLostException = (errorEvent.Exception is ServiceBusException) &&
                                               errorEvent.Exception.Message.Contains("(MessageLockLost)", StringComparison.OrdinalIgnoreCase);

            if (!isMessageLockLostException)
                this._logger.LogError(errorEvent.Exception, "Error while processing message on Queue/Topic: {0}", this._subscribedTo);

            return Task.CompletedTask;
        }
    }
}
