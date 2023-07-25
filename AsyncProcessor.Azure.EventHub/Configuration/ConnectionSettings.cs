using System;


namespace AsyncProcessor.Azure.EventHub.Configuration
{
    public class ConnectionSettings
    {
        /// <summary>
        /// Event Hubs connection string
        /// </summary>
        /// <remarks>
        /// If the connection string does not contain the Event Hub name, then use the supplied EventHub property
        /// </remarks>
        public string ConnectionString { get; set; }


        /// <summary>
        /// Name of the Event Hub
        /// </summary>
        /// <remarks>
        /// Use this setting if not supplied in the connection string
        /// See: https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient.-ctor?view=azure-dotnet#azure-messaging-eventhubs-consumer-eventhubconsumerclient-ctor(system-string-system-string)
        /// See: https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.producer.eventhubproducerclient.-ctor?view=azure-dotnet#azure-messaging-eventhubs-producer-eventhubproducerclient-ctor(system-string-system-string)
        /// </remarks>
        public string EventHub { get; set; }
    }
}
