using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncProcessor;
using AsyncProcessor.Example.Models;


namespace AsyncProcessor.Azure.ServiceBus.Example.Producer
{
    internal class Worker : BackgroundService
    {
        private const string WORKER_NAME = "Producer";
        private const string TOPIC = "proof_of_concept";            // Queue Name or Topic Name

        private readonly ILogger _logger;
        private readonly IProducer<Customer> _producer;

        private System.Timers.Timer _timer;
        private Random _random = new Random();

        public Worker(ILogger<Worker> logger,
                      IProducer<Customer> producer)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this._producer = producer ??
                throw new ArgumentNullException(nameof(producer));
        }

        #region Override BackgroundService Methods
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var msg = String.Format("{0} starting.", WORKER_NAME);
            this._logger.LogInformation(msg);

            this._timer = CreateTimer(5000);
            return base.StartAsync(cancellationToken);
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            var msg = String.Format("{0} stopping", WORKER_NAME);
            this._logger.LogInformation(msg);

            TeardownTimer(this._timer);
            return base.StopAsync(cancellationToken);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Use timer to slow down process of publishing messages
                this._timer.Start();
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "{0} encountered an exception while publishing to Queue/Topic ({1})", WORKER_NAME, TOPIC);
            }

            return Task.CompletedTask;
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
            int numOfMsgs = this._random.Next(30);
            IList<Customer> customers = new List<Customer>();

            for (int i = 0; i < numOfMsgs; i++)
                customers.Add(Customer.CreateCustomer());

            this._logger.LogInformation("Publishing a batch of {0} Customers", numOfMsgs);
            //await this._producer.Publish(Topic, customers);  // Publish as a batch

            // For demo purposes, loop and publish one at a time
            foreach (var cust in customers)
            {
                this._logger.LogInformation("Publishing Customer: {0}", cust.Name);
                await this._producer.Publish(TOPIC, cust);
            }
        }
    }
}
