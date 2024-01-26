using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.VMware.RabbitMQ.Configuration;
using System.Text;

namespace AsyncProcessor.VMware.RabbitMQ
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
        private readonly IConnection _client;
        private readonly IDictionary<string, IModel> _senders;
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

            this._senders = new Dictionary<string, IModel>(StringComparer.OrdinalIgnoreCase);
        }


        public async Task Publish(string topic,
                                  TMessage message,
                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.Publish(topic, new TMessage[] { message }, cancellationToken);
        }


        public Task Publish(string topic,
                            IEnumerable<TMessage> messages,
                            CancellationToken cancellationToken = default(CancellationToken))
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
                    var msg = Encoding.UTF8.GetBytes(json);
                    sender.BasicPublish(this._settings.Exchange, topic, null, msg);
                }
            }

            catch (TaskCanceledException)
            { }

            return Task.CompletedTask;
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
                    foreach (IModel sender in this._senders.Values)
                        sender.Dispose();

                    this._client.Dispose();
                }

                this._senders.Clear();
                _disposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Connection
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://www.rabbitmq.com/dotnet-api-guide.html#connecting
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IConnection CreateClient(ConnectionSettings settings)
        {
            IConnectionFactory factory = new ConnectionFactory()
            {
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                HostName = settings.HostName,
                Port = settings.Port,
            };

            return factory.CreateConnection();
        }


        /// <summary>
        /// Create IModel (publisher)
        /// </summary>
        /// <remarks>
        /// The documented process of sending a message, has a sender being created and disposed of within a using statement.
        /// However, due to the overhead of creating a new Producer/Sender, it is more benefitial hold onto the object for it's entire lifetime
        /// when used in an evironment where there is a high throughput of publishing messages
        /// </remarks>
        /// <returns></returns>
        private IModel CreateSender(string topic)
        {
            IModel channel = null;

            try
            {
                lock (this._semaphoreLock)
                {
                    bool isConnectionClosed = true;
                    var hasSender = this._senders.ContainsKey(topic);

                    // Acquire connection and test to make sure it is still connected
                    if (hasSender)
                    {
                        channel = this._senders[topic];
                        isConnectionClosed = channel.IsClosed;

                        // Remove connection from dictionary so that it can be re-added back in
                        if (isConnectionClosed)
                            this._senders.Remove(topic);
                    }

                    // If no connection or has become disconnected, get one
                    if (!hasSender || isConnectionClosed)
                    {
                        channel = this._client.CreateModel();
                        SetChannelProperties(channel, topic);
                        this._senders.Add(topic, channel);
                    }
                }
            }

            catch (Exception ex)
            {
                this._logger.LogError(ex, "Unable to create producer for queue/topic {0}", topic.ToString());
            }

            return channel;
        }


        private void SetChannelProperties(IModel channel, string topic)
        {
            ArgumentNullException.ThrowIfNull(channel);
            DeclareQueue(channel, topic, this._settings.Queue);
        }


        /// <summary>
        /// This will create/declare a queue on the fly if it does not already exist on the server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        private void DeclareQueue(IModel channel, string topic, QueueSettings settings)
        {
            if (settings == null) return;

            try
            {
                channel.QueueDeclare(topic, settings.Durable, settings.Exclusive, settings.AutoDelete);
            }

            catch
            { }
        }
    }
}
