using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using static AsyncProcessor.Confluent.Kafka.Services.ProcessService;

namespace AsyncProcessor.Confluent.Kafka.Services
{
    internal interface IProcessService
    {
        Func<ConsumeResult<Ignore, string>, Task> ProcessEvent { get; set; }
        Func<Error, Task> ProcessError { get; set; }
        Task ConsumeEvents(IConsumer<Ignore, string> client, CancellationToken cancellationToken);
    }
}
