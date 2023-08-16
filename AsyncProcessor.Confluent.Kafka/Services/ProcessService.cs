using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncProcessor.Confluent.Kafka.Services
{
    /// <summary>
    /// This is a Task background service needed to consume the events from kafka
    /// Going by examples of consumer clients with Kafka, a looping mechanism is used to consume each event
    /// To prevent blocking, this service will run on a separate thread and the CancellationToken will be
    /// used to exit the loop.
    /// When an event is read in, it will raise the ProcessEvent event.
    /// If an error occurs, then it will raise the ProcessError event.
    /// </summary>
    /// <remarks>
    /// According to MS, this is the best practice without using direct thread management
    /// See: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio#consuming-a-scoped-service-in-a-background-task
    /// 
    /// Examples of consuming events in Kafka
    /// https://docs.confluent.io/kafka-clients/dotnet/current/overview.html#auto-offset-commit
    /// https://developer.confluent.io/get-started/dotnet/?utm_medium=sem&utm_source=google&utm_campaign=ch.sem_br.nonbrand_tp.prs_tgt.dsa_mt.dsa_rgn.namer_lng.eng_dv.all_con.confluent-developer&utm_term=&creative=&device=c&placement=&gad=1&gclid=EAIaIQobChMI6rjf0_qsgAMV7MnjBx2m1gBrEAAYASAAEgKCTPD_BwE#build-consumer
    ///
    /// Regarding Commits..
    /// Commits are by default handled automatically.
    /// There is a way to disable the auto-commit functionality via configuration.  This app does not control nor look for that setting.
    /// This Process assumes the default automation.
    /// </remarks>
    internal class ProcessService : IProcessService
    {
        private readonly ILogger _logger;

        public ProcessService(ILogger<ProcessService> logger)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public Func<ConsumeResult<Ignore, string>, Task> ProcessEvent { get; set; }
        public Func<Error, Task> ProcessError { get; set; }


        public async Task ConsumeEvents(IConsumer<Ignore, string> client, CancellationToken cancellationToken)
        {
            bool cancel = false;
            ConsumeResult<Ignore, string> result = null;

            while (!cancel)
            {
                try
                {
                    result = client.Consume(cancellationToken);
                    await OnProcessEvent(result);
                }

                catch (ConsumeException e)
                {
                    this._logger.LogError(e, "A Kafka consumption error occurred while consuming events");
                    await OnProcessError(e.Error);
                }

                catch (KafkaException e)
                {
                    cancel = true;
                    this._logger.LogError(e, "A general Kafka error occurred while consuming events");
                    await OnProcessError(e.Error);
                }

                catch (OperationCanceledException e)
                {
                    cancel = true;
                }

                catch (Exception e)
                {
                    cancel = true;
                    this._logger.LogError(e, "An unexpected error occurred while consuming Kafka events");
                }
            }
        }

        private async Task OnProcessEvent(ConsumeResult<Ignore, string> result)
        {
            try
            {
                if (ProcessEvent != null)
                    await ProcessEvent(result);
            }
            catch
            { }
        }

        private async Task OnProcessError(Error error)
        {
            try
            {
                if (ProcessError != null)
                    await ProcessError(error);
            }
            catch
            { }
        }
    }
}
