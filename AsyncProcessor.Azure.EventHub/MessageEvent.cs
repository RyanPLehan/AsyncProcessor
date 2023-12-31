﻿using System;
using AsyncProcessor;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace AsyncProcessor.Azure.EventHub
{
    public class MessageEvent : IMessageEvent
    {
        private readonly ProcessEventArgs _args;

        internal MessageEvent(ProcessEventArgs processEventArgs)
        {
            this._args = processEventArgs;
        }

        public object EventData => this._args;

        public IMessage Message => new Message(this._args.Data);


        internal static ProcessEventArgs ParseArgs(IMessageEvent messageEvent)
        {
            ArgumentNullException.ThrowIfNull(messageEvent);


            if (messageEvent.EventData == null ||
                !(messageEvent.EventData is ProcessEventArgs))
                throw new MessageEventException("Missing or invalid EventData");

            return (ProcessEventArgs)messageEvent.EventData;
        }

    }
}
