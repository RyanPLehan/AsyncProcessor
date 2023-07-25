using System;

namespace AsyncProcessor.Azure.ServiceBus.Configuration
{
    public class ServiceBusSettings
    {
        public ConsumerSettings Consumer { get; set; }
        public ProducerSettings Producer { get; set; }
    }
}
