using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.EventHub.Configuration;
using Azure.Messaging.EventHubs.Producer;
using System.Collections.Concurrent;

namespace AsyncProcessor.Azure.EventHub
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
        private readonly EventHubProducerClient _client;

        private bool DisposedValue = false;

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
        }


        /// <summary>
        /// For Azure Event Hub, the connection is directly tied to the topic and therefore not needed
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Publish(string topic,
                                  TMessage message,
                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.Publish(topic, new TMessage[] { message }, cancellationToken);
        }


        /// <summary>
        /// For Azure Event Hub, the connection is directly tied to the topic and therefore not needed
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="messages"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task Publish(string topic,
                                  IEnumerable<TMessage> messages,
                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.DisposedValue)
                throw new ObjectDisposedException(nameof(Producer<TMessage>));

            if (!messages.Any())
                return;

            // Ensure that topic is the same as the event hub, if supplied.
            if (!String.IsNullOrWhiteSpace(topic) &&
                !this._client.EventHubName.Equals(topic.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("When topic is supplied, it must match Azure Event Hub name");

            var eventData = CreateEventData(messages);
            await this._client.SendAsync(eventData, cancellationToken);
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
            if (!DisposedValue)
            {
                if (disposing)
                {
                    this._client.DisposeAsync().GetAwaiter().GetResult();
                }

                DisposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Event Hub Producer Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.producer.eventhubproducerclient?view=azure-dotnet
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private EventHubProducerClient CreateClient(ConnectionSettings settings)
        {
            EventHubProducerClient client = null;

            if (String.IsNullOrWhiteSpace(this._settings.EventHub))
                client = new EventHubProducerClient(this._settings.ConnectionString);
            else
                client = new EventHubProducerClient(this._settings.ConnectionString,
                                                    this._settings.EventHub);

            return client;
        }


        private IEnumerable<EventData> CreateEventData<T>(IEnumerable<T> messages)
        {
            IList<EventData> eventData = new List<EventData>();

            foreach (T message in messages)
                eventData.Add(new EventData(Json.Serialize(message)));

            return eventData;
        }
    }
}
