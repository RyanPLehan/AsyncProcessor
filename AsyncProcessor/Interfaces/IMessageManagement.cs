using System;
using System.Threading.Tasks;


namespace AsyncProcessor
{
    public interface IMessageManagement
    {
        Task AcknowledgeMessage(IMessageEvent messageEvent);

        Task DenyAcknowledgement(IMessageEvent messageEvent,
                                 bool requeue = true);
    }
}
