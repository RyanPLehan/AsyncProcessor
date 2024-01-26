using System;


namespace AsyncProcessor.VMware.RabbitMQ.Configuration
{
    public class QueueSettings
    {
        public string Name { get; set; }
        public bool Durable { get; set; } = false;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
    }
}
