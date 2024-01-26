using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncProcessor.Asserts;

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
    /// https://github.com/Azure/azure-event-hubs-for-kafka/blob/master/quickstart/dotnet/EventHubsForKafkaSample/Worker.cs
    /// 
    /// Regarding Commits..
    /// Commits are by default handled automatically.
    /// There is a way to disable the auto-commit functionality via configuration.  This app does not control nor look for that setting.
    /// This Process assumes the default automation.
    /// </remarks>
    internal class ProcessService : IProcessService
    {
        private bool _cancel = false;
        private readonly ILogger _logger;

        private Func<ConsumeResult<Ignore, string>, Task> _processEvent;
        private Func<Error, Task> _processError;


        public ProcessService(ILogger<ProcessService> logger)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public event Func<ConsumeResult<Ignore, string>, Task> ProcessEvent
        {
            add
            {
                Argument.AssertNotNull(value, nameof(ProcessEvent));
                Argument.AssertEventHandlerNotAssigned(this._processEvent, default, nameof(ProcessEvent));

                this._processEvent = value;
            }

            remove
            {
                Argument.AssertNotNull(value, nameof(ProcessEvent));
                Argument.AssertSameEventHandlerAssigned(this._processEvent, value, nameof(ProcessEvent));

                this._processEvent = default;
            }
        }


        public event Func<Error, Task> ProcessError
        {
            add
            {
                Argument.AssertNotNull(value, nameof(ProcessError));
                Argument.AssertEventHandlerNotAssigned(this._processError, default, nameof(ProcessError));

                this._processError = value;
            }

            remove
            {
                Argument.AssertNotNull(value, nameof(ProcessError));
                Argument.AssertSameEventHandlerAssigned(this._processError, value, nameof(ProcessError));

                this._processError = default;
            }
        }


        public async Task StartConsumeEvents(IConsumer<Ignore, string> client, CancellationToken cancellationToken)
        {
            this._cancel = false;
            ConsumeResult<Ignore, string> result = null;

            while (!this._cancel)
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
                    this._cancel = true;
                    this._logger.LogError(e, "A general Kafka error occurred while consuming events");
                    await OnProcessError(e.Error);
                }

                catch (OperationCanceledException e)
                {
                    this._cancel = true;
                }

                catch (Exception e)
                {
                    this._cancel = true;
                    this._logger.LogError(e, "An unexpected error occurred while consuming Kafka events");
                }
            }
        }

        public void StopConsumeEvents()
        {
            this._cancel = true;
        }

        private async Task OnProcessEvent(ConsumeResult<Ignore, string> result)
        {
            try
            {
                if (this._processEvent != default)
                    await this._processEvent(result);
            }
            catch
            { }
        }

        private async Task OnProcessError(Error error)
        {
            try
            {
                if (this._processError != default)
                    await this._processError(error);
            }
            catch
            { }
        }
    }
}
