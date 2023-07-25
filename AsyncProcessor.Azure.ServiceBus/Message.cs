using System;
using AsyncProcessor;
using Azure.Messaging.ServiceBus;

namespace AsyncProcessor.Azure.ServiceBus
{
    public class Message : IMessage
    {
        private readonly ServiceBusReceivedMessage ReceivedMessage;

        internal Message(ServiceBusReceivedMessage message)
        {
            this.ReceivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this.ReceivedMessage;

        public string MessageId => this.ReceivedMessage.MessageId;

        public string CorrelationId => this.ReceivedMessage.CorrelationId;

        public string Partition => this.ReceivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this.ReceivedMessage.EnqueuedTime.UtcDateTime;


        internal static ServiceBusReceivedMessage ParseMessage(IMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (message.MessageData == null ||
                !(message.MessageData is ServiceBusReceivedMessage))
                throw new MessageEventException("Missing or invalid MessageData");

            return (ServiceBusReceivedMessage)message.MessageData;
        }
    }
}
