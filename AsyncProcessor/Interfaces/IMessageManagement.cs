using System;
using System.Threading.Tasks;


namespace AsyncProcessor
{
    public interface IMessageManagement
    {
        bool IsMessageManagementSupported { get; }

        Task AcknowledgeMessage(IMessageEvent messageEvent);

        Task DenyAcknowledgement(IMessageEvent messageEvent,
                                 bool requeue = true);
    }
}
