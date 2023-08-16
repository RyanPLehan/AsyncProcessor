using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using AsyncProcessor;
using AsyncProcessor.Example.Models;
using Microsoft.Extensions.Logging;

namespace AsyncProcessor.Azure.EventHub.Example.Consumer.Handlers.Notification
{
    internal class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification<Customer>>
    {
        private readonly ILogger _logger;

        public MessageReceivedHandler(ILogger<MessageReceivedHandler> logger)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }


        public Task Handle(MessageReceivedNotification<Customer> notification, CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Processing Customer: {0}", notification.Message.Name);
            return Task.CompletedTask;
        }
    }
}
