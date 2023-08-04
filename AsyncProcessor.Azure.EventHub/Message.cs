using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;

namespace AsyncProcessor.Azure.EventHub
{
    public class Message : IMessage
    {
        private readonly EventData _ReceivedMessage;

        internal Message(EventData message)
        {
            this._ReceivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this._ReceivedMessage;

        public string MessageId => this._ReceivedMessage.MessageId;

        public string CorrelationId => this._ReceivedMessage.CorrelationId;

        public string Partition => this._ReceivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this._ReceivedMessage.EnqueuedTime.UtcDateTime;


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
