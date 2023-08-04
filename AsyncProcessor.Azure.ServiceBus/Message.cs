using System;
using AsyncProcessor;
using Azure.Messaging.ServiceBus;

namespace AsyncProcessor.Azure.ServiceBus
{
    public class Message : IMessage
    {
        private readonly ServiceBusReceivedMessage _ReceivedMessage;

        internal Message(ServiceBusReceivedMessage message)
        {
            this._ReceivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this._ReceivedMessage;

        public string MessageId => this._ReceivedMessage.MessageId;

        public string CorrelationId => this._ReceivedMessage.CorrelationId;

        public string Partition => this._ReceivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this._ReceivedMessage.EnqueuedTime.UtcDateTime;


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
