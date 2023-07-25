using System;
using AsyncProcessor;
using Azure.Messaging.ServiceBus;

namespace AsyncProcessor.Azure.ServiceBus
{
    public class MessageEvent : IMessageEvent
    {
        private readonly ProcessMessageEventArgs Args;

        internal MessageEvent(ProcessMessageEventArgs processMessageEventArgs)
        {
            this.Args = processMessageEventArgs ??
                throw new ArgumentNullException(nameof(processMessageEventArgs));
        }

        public object EventData => this.Args;

        public IMessage Message => new Message(this.Args.Message);


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
