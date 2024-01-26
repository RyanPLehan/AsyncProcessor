using System;


namespace AsyncProcessor.VMware.RabbitMQ.Configuration
{
    public class ProducerSettings : ConnectionSettings
    {
        public string Exchange { get; set; } = String.Empty;

        /// <summary>
        /// Queue settings only if declaring a queue on the fly
        /// </summary>
        /// <remarks>
        /// Generally, queues should be created on the server with the appropriate settings.
        /// These settings will not override any existing queue
        /// </remarks>
        public QueueSettings Queue { get; set; } = null;
    }
}
