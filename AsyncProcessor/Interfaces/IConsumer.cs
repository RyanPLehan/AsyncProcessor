using System;

namespace AsyncProcessor
{
    public interface IConsumer<TMessage> : ISubscriptionManagement, IMessageManagement
    {
        Func<IMessageEvent, Task> OnMessageReceived { get; set; }
        Func<IErrorEvent, Task> OnErrorReceived { get; set; }
        TMessage GetMessage(IMessageEvent messageEvent);
    }
}
