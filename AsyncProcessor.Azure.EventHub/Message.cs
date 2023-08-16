using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;

namespace AsyncProcessor.Azure.EventHub
{
    public class Message : IMessage
    {
        private readonly EventData _receivedMessage;

        internal Message(EventData message)
        {
            this._receivedMessage = message ??
                throw new ArgumentNullException(nameof(message));
        }

        public object MessageData => this._receivedMessage;

        public string MessageId => this._receivedMessage.MessageId;

        public string CorrelationId => this._receivedMessage.CorrelationId;

        public string Partition => this._receivedMessage.PartitionKey;

        public DateTime EnqueuedTimeUTC => this._receivedMessage.EnqueuedTime.UtcDateTime;


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
