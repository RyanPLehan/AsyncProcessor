using System;
using System.Text;
using AsyncProcessor;
using Confluent.Kafka;

namespace AsyncProcessor.Confluent.Kafka
{
    public class Message : IMessage
    {
        private readonly ConsumeResult<Ignore, string> _Result;
        private readonly Message<Ignore, string> _ReceivedMessage;

        internal Message(ConsumeResult<Ignore, string> message)
        {
            this._Result = message ??
                throw new ArgumentNullException(nameof(message));

            this._ReceivedMessage = this._Result.Message;
        }

        public object MessageData => this._Result;

        public string MessageId => GetHeaderValue("MessageId");

        public string CorrelationId => GetHeaderValue("CorrelationId");

        public string Partition => this._Result.Partition.Value.ToString();

        public DateTime EnqueuedTimeUTC => this._ReceivedMessage.Timestamp.UtcDateTime;


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

            IHeader header = this._ReceivedMessage.Headers
                                                  .Where(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                                                  .FirstOrDefault();

            if (header != null)
                ret = Encoding.UTF8.GetString(header.GetValueBytes());

            return ret;
        }
    }
}
