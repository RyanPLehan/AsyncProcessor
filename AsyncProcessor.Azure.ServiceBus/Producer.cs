﻿using System;
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
        private readonly ILogger Logger;
        private readonly ProducerSettings Settings;
        private readonly ServiceBusClient Client;
        private readonly IDictionary<string, ServiceBusSender> Senders;
        private readonly object SemaphoreLock = new object();

        private bool disposedValue = false;

        public Producer(ILogger<Producer<TMessage>> logger,
                        IOptions<ProducerSettings> settings)
            : this(logger, settings?.Value)
        {
        }


        public Producer(ILogger<Producer<TMessage>> logger,
                        ProducerSettings settings)
        {
            this.Logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            this.Settings = settings ??
                throw new ArgumentNullException(nameof(settings));

            this.Client = CreateClient(settings);

            this.Senders = new Dictionary<string, ServiceBusSender>(StringComparer.OrdinalIgnoreCase);
        }


        public async Task Publish(string topic,
                                  TMessage message,
                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.Publish(topic, new TMessage[] { message }, cancellationToken);
        }


        public async Task Publish(string topic,
                                  IEnumerable<TMessage> messages,
                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.disposedValue)
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (ServiceBusSender sender in this.Senders.Values)
                        sender.DisposeAsync().GetAwaiter().GetResult();

                    this.Client.DisposeAsync().GetAwaiter().GetResult();
                }

                this.Senders.Clear();
                disposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Service Bus Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// See https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements?tabs=net-standard-sdk-2#reusing-factories-and-clients
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private ServiceBusClient CreateClient(ConnectionSettings settings)
        {
            return new ServiceBusClient(this.Settings.ConnectionString);
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
                lock (this.SemaphoreLock)
                {
                    bool isConnectionClosed = true;
                    var hasSender = this.Senders.ContainsKey(topic);

                    // Acquire connection and test to make sure it is still connected
                    if (hasSender)
                    {
                        ret = this.Senders[topic];
                        isConnectionClosed = ret.IsClosed;

                        // Remove connection from dictionary so that it can be re-added back in
                        if (isConnectionClosed)
                            this.Senders.Remove(topic);
                    }

                    // If no connection or has become disconnected, get one
                    if (!hasSender || isConnectionClosed)
                    {
                        ret = this.Client.CreateSender(topic);
                        this.Senders.Add(topic, ret);
                    }
                }
            }

            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Unable to create producer for queue/topic {0}", topic.ToString());
            }

            return ret;
        }

    }
}
