using System;
using AsyncProcessor;
using Azure.Messaging.ServiceBus;

namespace AsyncProcessor.Azure.ServiceBus
{
    public class ErrorEvent : IErrorEvent
    {
        private readonly ProcessErrorEventArgs _args;

        internal ErrorEvent(ProcessErrorEventArgs processErrorEventArgs)
        {
            this._args = processErrorEventArgs ??
                throw new ArgumentNullException(nameof(processErrorEventArgs));
        }

        public object EventData => this._args;
        public Exception Exception => this._args.Exception;
        public string Partition => this._args.EntityPath;

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
