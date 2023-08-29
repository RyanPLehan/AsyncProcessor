using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;

namespace AsyncProcessor
{
    public abstract class ConsumerWorker<TMessage> : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IConsumer<TMessage> _consumer;

        protected ConsumerWorker(ILogger logger,
                                 IMediator mediator,
                                 IConsumer<TMessage> consumer)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this._mediator = mediator ??
                throw new ArgumentNullException(nameof(mediator));

            this._consumer = consumer ??
                throw new ArgumentNullException(nameof(consumer));

            this.Consumer.ProcessMessage += this.HandleProcessMessage;
            this.Consumer.ProcessError += this.HandleProcessError;
        }

        #region Abstraction Definitions
        protected abstract Task Subscribe(CancellationToken cancellationToken);
        #endregion


        #region Accessors/Mutators
        protected virtual string? WorkerName => this.GetType().FullName;

        protected virtual bool RequeueMessageOnFailure => false;

        protected ILogger Logger => this._logger;

        protected IMediator Mediator => this._mediator;

        protected IConsumer<TMessage> Consumer => this._consumer;
        #endregion


        #region Override BackgroundService Methods
        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            var msg = String.Format("{0} started.", this.WorkerName);
            this._logger.LogInformation(msg);
            return base.StartAsync(cancellationToken);
        }


        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            this.Consumer.Detach();
            var msg = String.Format("{0} stopped", this.WorkerName);
            this._logger.LogInformation(msg);
            return base.StopAsync(cancellationToken);
        }


        /// <summary>
        /// Entry point for main execution
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger.LogInformation("{0} attaching to subscription", this.WorkerName);
                await this.Subscribe(cancellationToken);
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "{0} encountered an exception while subscribing to Queue/Topic", this.WorkerName);
            }
        }
        #endregion


        #region Methods
        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="messageEvent"></param>
        /// <returns></returns>
        protected virtual async Task HandleProcessMessage(IMessageEvent messageEvent)
        {
            try
            {
                TMessage message = this.Consumer.GetMessage(messageEvent);

                this._logger.LogInformation("{0} received a  message with ID {1}.", this.WorkerName, messageEvent.Message.MessageId);

                var notification = new MessageReceivedNotification<TMessage> { Message = message };
                await this.Mediator.Publish(notification);

                if (this.Consumer.IsMessageManagementSupported)
                    await this.Consumer.AcknowledgeMessage(messageEvent);  // Manual Acknowledgement

                this._logger.LogInformation("{0} processed a message with ID {1}.  Returning Successful Acknowledgment.", this.WorkerName, messageEvent.Message.MessageId);
            }

            catch (OperationCanceledException ex)
            {
                this._logger.LogError(ex, "{0} message processing operation has been cancelled", this.WorkerName);
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "Exception while handling event.  Returning Deny Acknowledgment");

                if (this.Consumer.IsMessageManagementSupported)
                    await this.Consumer.DenyAcknowledgement(messageEvent, this.RequeueMessageOnFailure);  // Manual Acknowledgement
            }
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        /// <param name="messageEvent"></param>
        /// <returns></returns>
        protected virtual Task HandleProcessError(IErrorEvent errorEvent)
        {
            try
            {
                this._logger.LogError(errorEvent.Exception, "{0} encountered an unexpected error.", this.WorkerName);
            }

            catch
            { }

            return Task.CompletedTask;
        }
        #endregion

    }
}
