using System;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncProcessor;
using AsyncProcessor.Example.Models;


namespace AsyncProcessor.Azure.ServiceBus.Example.Consumer
{
    internal class Worker : ConsumerWorker<Customer>
    {
        private const string Topic = "proof_of_concept";            // Queue Name or Topic Name
        private readonly ILogger Logger;

        public Worker(ILogger<Worker> logger,
                      IMediator mediator,
                      IConsumer<Customer> consumer)
            :base(logger, mediator, consumer)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        #region Override ConsumerWorker
        /// <summary>
        /// Override base WorkerName if wanting to have a different name then the actual class
        /// </summary>
        protected override string? WorkerName => "Consumer";

        /// <summary>
        /// Must implement Subscribe method to subscribe to queue or topic/subscription to receive messages
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override async Task Subscribe()
        {
            // The Consumer object is created in the DI, but needed in the base class, which is accessible via the derived class
            await this.Consumer.Attach(Topic);
        }
        #endregion
    }
}
