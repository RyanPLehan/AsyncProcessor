using System;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncProcessor;
using AsyncProcessor.Example.Models;
using System.Timers;

namespace AsyncProcessor.Confluent.Kafka.Example.Producer
{
    internal class Worker : BackgroundService
    {
        private const string WorkerName = "Producer";
        private const string Topic = "proof_of_concept";            // Queue Name or Topic Name
        private readonly ILogger Logger;
        private readonly IProducer<Customer> Producer;

        private System.Timers.Timer Timer;
        private Random Random = new Random();

        public Worker(ILogger<Worker> logger,
                      IProducer<Customer> producer)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this.Producer = producer ??
                throw new ArgumentNullException(nameof(producer));
        }

        #region Override BackgroundService Methods
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var msg = String.Format("{0} starting.", WorkerName);
            this.Logger.LogInformation(msg);

            this.Timer = CreateTimer(5000);
            return base.StartAsync(cancellationToken);
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            var msg = String.Format("{0} stopping", WorkerName);
            this.Logger.LogInformation(msg);

            TeardownTimer(this.Timer);
            return base.StopAsync(cancellationToken);
        }


        /// <summary>
        /// Setup connection to TQL Pub Sub
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Use timer to slow down process of publishing messages
                this.Timer.Start();
            }

            catch (Exception ex)
            {
                this.Logger.LogError(ex, "{0} encountered an exception while publishing to Queue/Topic ({1})", WorkerName, Topic);
            }
        }
        #endregion

        private System.Timers.Timer CreateTimer(int interval)
        {
            System.Timers.Timer timer = new System.Timers.Timer(interval);
            timer.Elapsed += async(x, y) => { await this.PublishCustomers(); };
            timer.AutoReset = true;
            return timer;
        }

        private void TeardownTimer(System.Timers.Timer timer)
        {
            timer.Stop();
            timer.Dispose();
        }

        /// <summary>
        /// This will create a batch of customers (messages) to be publish.
        /// </summary>
        /// <remarks>
        /// *** Please Note ***
        /// 1. For demonstration purposes only: 
        ///     A random number of customers are created and published one message at a time, which in most cases is the norm
        /// 2. Publishing messages in a batch is avaiable
        /// </remarks>
        /// <returns></returns>
        private async Task PublishCustomers()
        {
            int numOfMsgs = this.Random.Next(30);
            IList<Customer> customers = new List<Customer>();

            for (int i = 0; i < numOfMsgs; i++)
                customers.Add(Customer.CreateCustomer());

            this.Logger.LogInformation("Publishing a batch of {0} Customers", numOfMsgs);
            //await this.Producer.Publish(Topic, customers);  // Publish as a batch

            // For demo purposes, loop and publish one at a time
            foreach (var cust in customers)
            {
                this.Logger.LogInformation("Publishing Customer: {0}", cust.Name);
                await this.Producer.Publish(Topic, cust);
            }
        }
    }
}
