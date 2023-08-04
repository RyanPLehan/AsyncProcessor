using System;

namespace AsyncProcessor.Confluent.Kafka.Configuration
{
    public class KafkaSettings
    {
        public ConsumerSettings Consumer { get; set; }
        public ProducerSettings Producer { get; set; }
    }
}
