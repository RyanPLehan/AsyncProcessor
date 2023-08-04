using System;
using AsyncProcessor;
using Confluent.Kafka;

namespace AsyncProcessor.Confluent.Kafka
{
    public class MessageEvent : IMessageEvent
    {
        private readonly ConsumeResult<Ignore, string> _Result;

        internal MessageEvent(ConsumeResult<Ignore, string> result)
        {
            this._Result = result;
        }

        public object EventData => this._Result;

        public IMessage Message => new Message(this._Result);


        internal static ConsumeResult<Ignore, string> ParseResult(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);


            if (messageEvent.EventData == null ||
                !(messageEvent.EventData is ConsumeResult<Ignore, string>))
                throw new MessageEventException("Missing or invalid EventData");

            return (ConsumeResult<Ignore, string>)messageEvent.EventData;
        }

    }
}
