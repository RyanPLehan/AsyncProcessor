using System;
using Confluent.Kafka;
using AsyncProcessor;

namespace AsyncProcessor.Confluent.Kafka
{
    public class ErrorEvent : IErrorEvent
    {
        private readonly IConsumer<Ignore, string> _Consumer;
        private readonly Error _Error;

        internal ErrorEvent(IConsumer<Ignore, string> consumer, Error error)
        {
            this._Consumer = consumer;
            this._Error = error;
        }

        public object EventData => this._Consumer;
        public Exception Exception => new Exception(this._Error.Code.ToString());
        public string Partition => String.Empty;

        internal static IConsumer<Ignore, string> ParseConsumer(IErrorEvent errorEvent)
        {
            ArgumentNullException.ThrowIfNull(errorEvent);


            if (errorEvent == null ||
                !(errorEvent.Exception is IConsumer<Ignore, string>))
                throw new ErrorEventException("Missing or invalid EventData");

            return (IConsumer<Ignore, string>)errorEvent.EventData;
        }
    }
}
