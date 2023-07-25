using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;

namespace AsyncProcessor.Azure.EventHub
{
    public class Message : IMessage
    {
        private readonly EventData ReceivedMessage;

        internal Message(EventData message)
        {
            this.ReceivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this.ReceivedMessage;

        public string MessageId => this.ReceivedMessage.MessageId;

        public string CorrelationId => this.ReceivedMessage.CorrelationId;

        public string Partition => this.ReceivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this.ReceivedMessage.EnqueuedTime.UtcDateTime;


        internal static EventData ParseMessage(IMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (message.MessageData == null ||
                !(message.MessageData is EventData))
                throw new MessageEventException("Missing or invalid MessageData");

            return (EventData)message.MessageData;
        }
    }
}
