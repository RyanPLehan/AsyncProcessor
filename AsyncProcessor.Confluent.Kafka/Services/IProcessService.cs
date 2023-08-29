using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using static AsyncProcessor.Confluent.Kafka.Services.ProcessService;

namespace AsyncProcessor.Confluent.Kafka.Services
{
    internal interface IProcessService
    {
        event Func<ConsumeResult<Ignore, string>, Task> ProcessEvent;
        event Func<Error, Task> ProcessError;

        Task StartConsumeEvents(IConsumer<Ignore, string> client, CancellationToken cancellationToken);
        void StopConsumeEvents();
    }
}
