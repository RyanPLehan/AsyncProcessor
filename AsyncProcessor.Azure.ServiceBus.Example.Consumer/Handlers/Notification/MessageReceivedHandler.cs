using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using AsyncProcessor;
using AsyncProcessor.Example.Models;
using Microsoft.Extensions.Logging;

namespace AsyncProcessor.Azure.ServiceBus.Example.Consumer.Handlers.Notification
{
    internal class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification<Customer>>
    {
        private readonly ILogger Logger;

        public MessageReceivedHandler(ILogger<MessageReceivedHandler> logger)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }


        public Task Handle(MessageReceivedNotification<Customer> notification, CancellationToken cancellationToken)
        {
            this.Logger.LogInformation("Processing Customer: {0}", notification.Message.Name);
            return Task.CompletedTask;
        }
    }
}
