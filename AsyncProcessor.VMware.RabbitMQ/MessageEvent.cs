using System;
using AsyncProcessor;
using RabbitMQ.Client;

namespace AsyncProcessor.VMware.RabbitMQ
{
    public class MessageEvent : IMessageEvent
    {
        private readonly ProcessMessageEventArgs _args;

        internal MessageEvent(ProcessMessageEventArgs processMessageEventArgs)
        {
            this._args = processMessageEventArgs ??
                throw new ArgumentNullException(nameof(processMessageEventArgs));
        }

        public object EventData => this._args;

        public IMessage Message => new Message(this._args.Message);


        internal static ProcessMessageEventArgs ParseArgs(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);


            if (messageEvent.EventData == null ||
                !(messageEvent.EventData is ProcessMessageEventArgs))
                throw new MessageEventException("Missing or invalid EventData");

            return (ProcessMessageEventArgs)messageEvent.EventData;
        }

    }
}
