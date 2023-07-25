using System;
using MediatR;

namespace AsyncProcessor
{
    public class MessageReceivedNotification<TMessage> : INotification
    {
        public TMessage Message { get; set; }
    }
}
