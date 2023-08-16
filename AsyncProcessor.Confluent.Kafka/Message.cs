using System;
using System.Text;
using AsyncProcessor;
using Confluent.Kafka;

namespace AsyncProcessor.Confluent.Kafka
{
    public class Message : IMessage
    {
        private readonly ConsumeResult<Ignore, string> _result;
        private readonly Message<Ignore, string> _receivedMessage;

        internal Message(ConsumeResult<Ignore, string> message)
        {
            this._result = message ??
                throw new ArgumentNullException(nameof(message));

            this._receivedMessage = this._result.Message;
        }

        public object MessageData => this._result;

        public string MessageId => GetHeaderValue("MessageId");

        public string CorrelationId => GetHeaderValue("CorrelationId");

        public string Partition => this._result.Partition.Value.ToString();

        public DateTime EnqueuedTimeUTC => this._receivedMessage.Timestamp.UtcDateTime;


        internal static ConsumeResult<Ignore, string> ParseResult(IMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);


            if (message.MessageData == null ||
                !(message.MessageData is ConsumeResult<Ignore, string>))
                throw new MessageEventException("Missing or invalid MessageData");

            return (ConsumeResult<Ignore, string>)message.MessageData;
        }

        internal static Message<Ignore, string> ParseMessage(IMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            ConsumeResult<Ignore, string> result = ParseResult(message);
            return result.Message;
        }

        private string GetHeaderValue(string key, string defaultValue = null)
        {
            string ret = defaultValue;

            IHeader header = this._receivedMessage.Headers
                                                  .Where(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                                                  .FirstOrDefault();

            if (header != null)
                ret = Encoding.UTF8.GetString(header.GetValueBytes());

            return ret;
        }
    }
}
