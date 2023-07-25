using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.ServiceBus.Configuration;


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
        private ServiceBusProcessor Receiver = null;
        private bool disposedValue = false;
        private readonly ILogger Logger;
        private readonly ConsumerSettings Settings;
        private readonly ServiceBusClient Client;


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
            var message = args.Message;
            var json = Encoding.UTF8.GetString(message.Body);
            return Json.Deserialize<TMessage>(json);
        }
        #endregion


        #region Subscription Management
        public async Task Attach(string topic)
        {
            if (this.Receiver == null ||
                this.Receiver.IsClosed)
            {
                var options = CreateProcessorOptions();
                this.Receiver = this.Client.CreateProcessor(topic, options);
                this.Receiver.ProcessMessageAsync += this.ExecuteProcessMessage;
                this.Receiver.ProcessErrorAsync += this.ExecuteProcessError;
                await Resume();
            }
        }

        public async Task Attach(string topic, string subscription)
        {
            if (this.Receiver == null ||
                this.Receiver.IsClosed)
            {
                var options = CreateProcessorOptions();
                this.Receiver = this.Client.CreateProcessor(topic, subscription, options);
                this.Receiver.ProcessMessageAsync += this.ExecuteProcessMessage;
                this.Receiver.ProcessErrorAsync += this.ExecuteProcessError;
                await Resume();
            }
        }

        public async Task Detach()
        {
            if (this.Receiver != null &&
                !this.Receiver.IsClosed)
            {
                await Pause();
                this.Receiver.ProcessMessageAsync -= this.ExecuteProcessMessage;
                this.Receiver.ProcessErrorAsync -= this.ExecuteProcessError;
                await this.Receiver.DisposeAsync();
            }
        }

        public async Task Pause()
        {
            if (this.Receiver != null)
                await this.Receiver.StopProcessingAsync();
        }

        public async Task Resume()
        {
            if (this.Receiver != null)
                await this.Receiver.StartProcessingAsync();
        }
        #endregion


        #region Message Management
        public async Task AcknowledgeMessage(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);

            if (this.Receiver != null &&
                this.Receiver.ReceiveMode == ServiceBusReceiveMode.ReceiveAndDelete)
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

            if (this.Receiver != null &&
                this.Receiver.ReceiveMode == ServiceBusReceiveMode.ReceiveAndDelete)
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Detach().GetAwaiter().GetResult();
                    this.Client.DisposeAsync().GetAwaiter().GetResult();

                    this.OnMessageReceived = null;
                    this.OnErrorReceived = null;
                }

                disposedValue = true;
            }
        }
        #endregion


        /// <summary>
        /// Create Service Bus Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private ServiceBusClient CreateClient(ConnectionSettings settings)
        {
            return new ServiceBusClient(this.Settings.ConnectionString);
        }


        private async Task ExecuteProcessMessage(ProcessMessageEventArgs processMessageEventArgs)
        {
            if (OnMessageReceived != null)
            {
                await OnMessageReceived(new MessageEvent(processMessageEventArgs));
            }
        }

        private async Task ExecuteProcessError(ProcessErrorEventArgs processErrorEventArgs)
        {
            if (OnErrorReceived != null)
            {
                await OnErrorReceived(new ErrorEvent(processErrorEventArgs));
            }
        }



        private ServiceBusReceiverOptions CreateReceiverOptions()
        {
            return new ServiceBusReceiverOptions()
            {
                PrefetchCount = this.Settings.PrefetchCount,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                SubQueue = SubQueue.None,
            };
        }

        private ServiceBusProcessorOptions CreateProcessorOptions()
        {
            return new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = this.Settings.ConcurrentDispatch,
                PrefetchCount = this.Settings.PrefetchCount,
                ReceiveMode = this.Settings.ReceiveMode,
            };
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="errorEvent"></param>
        /// <returns></returns>
        protected virtual async Task OnErrorReceivedDefault(IErrorEvent errorEvent)
        {
            try
            {
                ProcessErrorEventArgs args = ErrorEvent.ParseArgs(errorEvent);

                // Do not log if message was locked
                bool isMessageLockLostException = (errorEvent.Exception is ServiceBusException) &&
                                                   errorEvent.Exception.Message.Contains("(MessageLockLost)", StringComparison.OrdinalIgnoreCase);

                if (!isMessageLockLostException)
                    this.Logger.LogError(errorEvent.Exception, "Error while processing message on {0}", args.EntityPath);
            }

            catch 
            { }
        }
    }
}
