using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace AsyncProcessor.Azure.EventHub
{
    public class ErrorEvent : IErrorEvent
    {
        private readonly ProcessErrorEventArgs _Args;

        internal ErrorEvent(ProcessErrorEventArgs processErrorEventArgs)
        {
            this._Args = processErrorEventArgs;
        }

        public object EventData => this._Args;
        public Exception Exception => this._Args.Exception;
        public string Partition => this._Args.PartitionId;

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
