using System;

namespace AsyncProcessor
{
    public interface IProducer<TMessage>
    {

        Task Publish(string topic,
                     TMessage message,
                     CancellationToken cancellationToken = default(CancellationToken));

        Task Publish(string topic,
                     IEnumerable<TMessage> messages,
                     CancellationToken cancellationToken = default(CancellationToken));
    }
}
