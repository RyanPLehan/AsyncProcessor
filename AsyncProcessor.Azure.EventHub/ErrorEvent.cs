using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace AsyncProcessor.Azure.EventHub
{
    public class ErrorEvent : IErrorEvent
    {
        private readonly ProcessErrorEventArgs _args;

        internal ErrorEvent(ProcessErrorEventArgs processErrorEventArgs)
        {
            this._args = processErrorEventArgs;
        }

        public object EventData => this._args;
        public Exception Exception => this._args.Exception;
        public string Partition => this._args.PartitionId;

        internal static ProcessErrorEventArgs ParseArgs(IErrorEvent errorEvent)
        {
            ArgumentNullException.ThrowIfNull(errorEvent);


            if (errorEvent.EventData == null ||
                !(errorEvent.EventData is ProcessErrorEventArgs))
                throw new ErrorEventException("Missing or invalid EventData");

            return (ProcessErrorEventArgs)errorEvent.EventData;
        }
    }
}
