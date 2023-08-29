using System;
using System.Threading.Tasks;

namespace AsyncProcessor
{
    public interface IConsumer<TMessage> : ISubscriptionManagement, IMessageManagement
    {
        event Func<IMessageEvent, Task> ProcessMessage;
        event Func<IErrorEvent, Task> ProcessError;

        TMessage GetMessage(IMessageEvent messageEvent);
    }
}
