using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Asserts;
using AsyncProcessor.Formatters;
using AsyncProcessor.VMware.RabbitMQ.Configuration;
using RabbitMQ.Client.Events;

namespace AsyncProcessor.VMware.RabbitMQ
{
    /// <summary>
    /// Consumer to receive messages from a queue service
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class Consumer<TMessage> :  IConsumer<TMessage>, IDisposable
    {
        private bool _disposedValue = false;
        private AsyncEventingBasicConsumer _receiver = null;
        private string _subscribedTo = null;

        private readonly ILogger _logger;
        private readonly ConsumerSettings _settings;
        private readonly IConnection _client;

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
            this.ProcessError += this.HandleProcessErrorDefault;
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
                this._receiver.Model.IsClosed)
            {
                this._receiver = this.CreateReceiver(this._client, topic);
                this._receiver.Received += this.HandleClientProcessMessage;
                this._subscribedTo = topic;
                await Resume();
            }
        }

        public async Task Attach(string topic, string subscription, CancellationToken cancellationToken = default)
        {
            await this.Attach(topic, cancellationToken);
        }

        public async Task Detach(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null &&
                !this._receiver.Model.IsClosed)
            {
                await Pause();
                this._receiver.Received -= this.HandleClientProcessMessage;
                await this._receiver.DisposeAsync();
            }
        }

        public async Task Pause(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null)
                await this._receiver.
        }

        public async Task Resume(CancellationToken cancellationToken = default)
        {
            if (this._receiver != null)
                await this._receiver.Model.BasicConsume()
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
                    this._re
                    this._client.Dispose();

                    this._processMessage = default;
                    this._processError = default;
                }

                _disposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Connection
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://www.rabbitmq.com/dotnet-api-guide.html#connecting
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IConnection CreateClient(ConsumerSettings settings)
        {
            IConnectionFactory factory = new ConnectionFactory()
            {
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                HostName = settings.HostName,
                Port = settings.Port,
                DispatchConsumersAsync = true,
                ConsumerDispatchConcurrency = settings.ConcurrentDispatch,
                AutomaticRecoveryEnabled = true,
            };

            return factory.CreateConnection();
        }


        private AsyncEventingBasicConsumer CreateReceiver(IConnection connection, string topic)
        {
            IModel channel = connection.CreateModel();
            SetChannelProperties(channel, topic);
            return new AsyncEventingBasicConsumer(channel);
        }


        private void SetChannelProperties(IModel channel, string topic)
        {
            ArgumentNullException.ThrowIfNull(channel);

            DeclareQueue(channel, topic, this._settings.Queue);
            SetPrefetch(channel, this._settings.PrefetchCount);
        }

        private void SetPrefetch(IModel channel, int prefetchSize)
        {
            channel.BasicQos((uint)prefetchSize, (ushort)prefetchSize, false);
        }

        /// <summary>
        /// This will create/declare a queue on the fly if it does not already exist on the server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        private void DeclareQueue(IModel channel, string topic, QueueSettings settings)
        {
            if (settings == null) return;

            try
            {
                channel.QueueDeclare(topic, settings.Durable, settings.Exclusive, settings.AutoDelete);
            }

            catch
            { }
        }



        private async Task HandleClientProcessMessage(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            if (this._processMessage != default)
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



        /// <summary>
        /// Receive and process message
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
