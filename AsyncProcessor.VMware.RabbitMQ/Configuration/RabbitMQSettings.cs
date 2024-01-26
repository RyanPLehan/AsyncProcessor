using System;

namespace AsyncProcessor.VMware.RabbitMQ.Configuration
{
    public class RabbitMQSettings
    {
        public ConsumerSettings Consumer { get; set; }
        public ProducerSettings Producer { get; set; }
    }
}
