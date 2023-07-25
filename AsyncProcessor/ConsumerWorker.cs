using System;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;

namespace AsyncProcessor
{
    public abstract class ConsumerWorker<TMessage> : BackgroundService
    {
        protected readonly ILogger Logger;
        protected readonly IMediator Mediator;
        protected readonly IConsumer<TMessage> Consumer;

        protected ConsumerWorker(ILogger logger,
                                 IMediator mediator,
                                 IConsumer<TMessage> consumer)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this.Mediator = mediator ??
                throw new ArgumentNullException(nameof(mediator));

            this.Consumer = consumer ??
                throw new ArgumentNullException(nameof(consumer));

            this.Consumer.OnMessageReceived = this.OnMessageReceived;
        }

        #region Abstraction Definitions
        protected abstract Task Subscribe();
        #endregion


        #region Accessors/Mutators
        protected virtual string? WorkerName
        {
            get => this.GetType().FullName;
        }
        #endregion


        #region Override BackgroundService Methods
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var msg = String.Format("{0} started.", this.WorkerName);
            this.Logger.LogInformation(msg);
            return base.StartAsync(cancellationToken);
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.Consumer.Detach();
            var msg = String.Format("{0} stopped", this.WorkerName);
            this.Logger.LogInformation(msg);
            return base.StopAsync(cancellationToken);
        }


        /// <summary>
        /// Setup connection to TQL Pub Sub
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                this.Logger.LogInformation("{0} attaching to subscription", this.WorkerName);
                await this.Subscribe();
            }

            catch (Exception ex)
            {
                this.Logger.LogError(ex, "{0} encountered an exception while subscribing to Queue/Topic", this.WorkerName);
            }
        }
        #endregion


        #region Methods
        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="messageEvent"></param>
        /// <returns></returns>
        protected virtual async Task OnMessageReceived(IMessageEvent messageEvent)
        {
            try
            {
                TMessage message = this.Consumer.GetMessage(messageEvent);

                this.Logger.LogInformation("{0} received a  message with ID {1}.", this.WorkerName, messageEvent.Message.MessageId);

                var notification = new MessageReceivedNotification<TMessage> { Message = message };
                await this.Mediator.Publish(notification);
                await this.Consumer.AcknowledgeMessage(messageEvent);  // Manual Acknowledgement

                this.Logger.LogInformation("{0} processed a message with ID {1}.  Returning Successful Acknowledgment.", this.WorkerName, messageEvent.Message.MessageId);
            }

            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Exception while handling event: {0}.  Returning Deny Acknowledgment", ex.Message);
                await this.Consumer.DenyAcknowledgement(messageEvent, true);  // Manual Acknowledgement
            }
        }
        #endregion

    }
}
