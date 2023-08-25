using System;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncProcessor;
using AsyncProcessor.Example.Models;


namespace AsyncProcessor.Azure.EventHub.Example.Consumer
{
    internal class Worker : ConsumerWorker<Customer>
    {
        private const string TOPIC = "proof_of_concept";            // Queue Name or Topic Name

        public Worker(ILogger<Worker> logger,
                      IMediator mediator,
                      IConsumer<Customer> consumer)
            :base(logger, mediator, consumer)
        {
        }

        #region Override ConsumerWorker
        /// <summary>
        /// Override base WorkerName if wanting to have a different name then the actual class
        /// </summary>
        protected override string? WorkerName => "Azure Event Hub Consumer";

        /// <summary>
        /// Must implement Subscribe method to subscribe to queue or topic/subscription to receive messages
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override async Task Subscribe(CancellationToken cancellationToken)
        {
            // The Consumer object is created in the DI, but needed in the base class, which is accessible to the derived class
            await this.Consumer.Attach(TOPIC, cancellationToken);
        }
        #endregion
    }
}
