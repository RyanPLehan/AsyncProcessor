﻿using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Asserts;
using AsyncProcessor.Formatters;
using AsyncProcessor.Azure.EventHub.Configuration;
using Azure.Messaging.EventHubs.Producer;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;

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
        private readonly IDictionary<string, EventHubProducerClient> _clients;

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

            this._clients = new Dictionary<string, EventHubProducerClient>(StringComparer.OrdinalIgnoreCase);
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
                                  CancellationToken cancellationToken = default)
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
                                  CancellationToken cancellationToken = default)
        {
            if (this._disposedValue)
                throw new ObjectDisposedException(nameof(Producer<TMessage>));

            Argument.AssertNotEmptyOrWhiteSpace(topic, nameof(topic));

            if (!messages.Any())
                return;

            EventHubProducerClient client = null;
            if (!this._clients.TryGetValue(topic, out client) ||
                client.IsClosed)
            {
                this._clients.Remove(topic);
                client = CreateClient(this._settings, topic);
                this._clients.Add(topic, client);
            }

            var eventData = CreateEventData(messages);
            await client.SendAsync(eventData, cancellationToken);
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
                    foreach(KeyValuePair<string, EventHubProducerClient> kvp in this._clients)
                        kvp.Value.DisposeAsync().GetAwaiter().GetResult();
                }

                _disposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Event Hub Producer Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.producer.eventhubproducerclient?view=azure-dotnet
        /// Since this class is registered as a singleton, we can safely initialize the client once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private EventHubProducerClient CreateClient(ConnectionSettings settings, string topic)
        {
            return new EventHubProducerClient(settings.ConnectionString,
                                              topic);
        }


        private IEnumerable<EventData> CreateEventData(IEnumerable<TMessage> messages)
        {
            IList<EventData> eventData = new List<EventData>();

            foreach (TMessage message in messages)
                eventData.Add(new EventData(Json.Serialize(message)));

            return eventData;
        }
    }
}
