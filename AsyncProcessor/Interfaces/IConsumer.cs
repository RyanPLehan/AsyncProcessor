using System;
using System.Threading.Tasks;

namespace AsyncProcessor
{
    public interface IConsumer<TMessage> : ISubscriptionManagement, IMessageManagement
    {
        Func<IMessageEvent, Task> ProcessMessage { get; set; }
        Func<IErrorEvent, Task> ProcessError { get; set; }
        TMessage GetMessage(IMessageEvent messageEvent);
    }
}
