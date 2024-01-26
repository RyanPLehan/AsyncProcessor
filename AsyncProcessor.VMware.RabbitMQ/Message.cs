using System;
using AsyncProcessor;
using RabbitMQ.Client;

namespace AsyncProcessor.VMware.RabbitMQ
{
    public class Message : IMessage
    {
        private readonly ServiceBusReceivedMessage _receivedMessage;

        internal Message(ServiceBusReceivedMessage message)
        {
            this._receivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this._receivedMessage;

        public string MessageId => this._receivedMessage.MessageId;

        public string CorrelationId => this._receivedMessage.CorrelationId;

        public string Partition => this._receivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this._receivedMessage.EnqueuedTime.UtcDateTime;


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
