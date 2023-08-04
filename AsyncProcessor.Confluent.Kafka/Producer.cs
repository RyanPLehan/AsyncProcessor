﻿using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessor;
using AsyncProcessor.Formatters;
using AsyncProcessor.Confluent.Kafka.Configuration;

namespace AsyncProcessor.Confluent.Kafka
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
        private readonly IProducer<Null, string> Client;

        private bool DisposedValue = false;

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

            DeliveryResult<Null, string> result;
            foreach (TMessage message in messages)
            {
                result = await this.Client.ProduceAsync(topic, CreateMessage(message), cancellationToken);
            }
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
                    this.Client.Dispose();
                }

                DisposedValue = true;
            }
        }
        #endregion

        /// <summary>
        /// Create Kafka Producer Client
        /// </summary>
        /// <remarks>
        /// For optimal performace the client will be instantiated once
        /// Since this class is registered as a singleton, we can safely initialize the cliet once.
        /// However, in this case we don't want to register the client via dependency injection as a singleton because the consumer and producer could have different connections
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IProducer<Null, string> CreateClient(ConnectionSettings settings)
        {
            var config = new ProducerConfig(settings.ConnectionProperties);
            var builder = new ProducerBuilder<Null, string>(config);

            return builder.Build();
        }


        private Message<Null, string> CreateMessage(TMessage message)
        {
            return new Message<Null, string>()
            {
                Value = Json.Serialize(message),
            };
        }
    }
}