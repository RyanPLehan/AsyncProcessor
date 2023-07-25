using System;

namespace AsyncProcessor.Azure.EventHub.Configuration
{
    public class EventHubSettings
    {
        public ConsumerSettings Consumer { get; set; }
        public ProducerSettings Producer { get; set; }
    }
}
