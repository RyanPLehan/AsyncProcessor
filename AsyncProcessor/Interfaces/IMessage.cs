using System;

namespace AsyncProcessor
{
    public interface IMessage
    {
        object MessageData { get; }
        string MessageId { get; }
        string CorrelationId { get; }
        string Partition { get; }
        DateTime EnqueuedTimeUTC { get; }
    }
}
