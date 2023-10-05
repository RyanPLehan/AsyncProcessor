using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.ServiceBus.Configuration;


namespace AsyncProcessor.Azure.ServiceBus
{
    /// <summary>
    /// Producer to publish a message on a Queue service
    /// </summary>
    /// <remarks>
    /// For performance reasons, this will use a dictionary to hold a list of active sending agents, one agent per queue / topic
    /// </remarks>
    public class Producer<TMessage> : IProducer<TMessage>, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ProducerSettings _settings;
        private readonly ServiceBusClient _client;
        private readonly IDictionary<string, ServiceBusSender> _senders;
        private readonly object _semaphoreLock = new object();

        private bool _disposedValue = false;

        public Producer(ILogger<Producer<TMessage>> logger,
                        IOptions<ProducerSettings> settings)
            : this(logger, settings?.Value)
        {
        }


        public Producer(ILogger<Producer<TMessage>> logger,
                        ProducerSettings settings)
        {
            this._logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this._settings = settings ??
                throw new ArgumentNullException(nameof(settings));

            this._client = CreateClient(settings);

            this._senders = new Dictionary<string, ServiceBusSender>(StringComparer.OrdinalIgnoreCase);
        }


        public async Task Publish(string topic,
                                  TMessage message,
                                  CancellationToken cancellationToken = default)
        {
            await this.Publish(topic, new TMessage[] { message }, cancellationToken);
        }


        public async Task Publish(string topic,
                                  IEnumerable<TMessage> messages,
                                  CancellationToken cancellationToken = default)
        {
            if (this._disposedValue)
                throw new ObjectDisposedException(nameof(Producer<TMessage>));

            if (String.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("Missing queue or topic name");

            try
            {
                var sender = CreateSender(topic);
                foreach (TMessage message in messages)
                {
                    var json = Json.Serialize(message);
                    var msg = new ServiceBusMessage(json);
                    await sender.SendMessageAsync(msg, cancellationToken);
                }
            }

            catch (TaskCanceledException)
            { }

        }

        #region Dispose
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (ServiceBusSender sender in this._senders.Values)
                        sender.DisposeAsync().GetAwaiter().GetResult();

                    this._client.DisposeAsync().GetAwaiter().GetResult();
                }

                this._senders.Clear();
                _disposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Service Bus Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private ServiceBusClient CreateClient(ConnectionSettings settings)
        {
            return new ServiceBusClient(settings.ConnectionString);
        }


        /// <summary>
        /// Create ServiceBusSender (publisher)
        /// </summary>
        /// <remarks>
        /// The documented process of sending a message, has a sender being created and disposed of within a using statement.
        /// However, due to the overhead of creating a new Producer/Sender, it is more benefitial hold onto the object for it's entire lifetime
        /// when used in an evironment where there is a high throughput of publishing messages
        /// </remarks>
        /// <returns></returns>
        private ServiceBusSender CreateSender(string topic)
        {
            ServiceBusSender ret = null;

            try
            {
                lock (this._semaphoreLock)
                {
                    bool isConnectionClosed = true;
                    var hasSender = this._senders.ContainsKey(topic);

                    // Acquire connection and test to make sure it is still connected
                    if (hasSender)
                    {
                        ret = this._senders[topic];
                        isConnectionClosed = ret.IsClosed;

                        // Remove connection from dictionary so that it can be re-added back in
                        if (isConnectionClosed)
                            this._senders.Remove(topic);
                    }

                    // If no connection or has become disconnected, get one
                    if (!hasSender || isConnectionClosed)
                    {
                        ret = this._client.CreateSender(topic);
                        this._senders.Add(topic, ret);
                    }
                }
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "Unable to create producer for queue/topic {0}", topic.ToString());
            }

            return ret;
        }

    }
}
