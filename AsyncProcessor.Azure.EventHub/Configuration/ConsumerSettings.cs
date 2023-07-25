using System;
using Azure.Messaging.EventHubs.Consumer;

namespace AsyncProcessor.Azure.EventHub.Configuration
{
    public class ConsumerSettings : ConnectionSettings
    {
        public string ConsumerGroup { get; set; } = EventHubConsumerClient.DefaultConsumerGroupName;

        public CheckpointSettings CheckpointSettings { get; set; }
    }
}
