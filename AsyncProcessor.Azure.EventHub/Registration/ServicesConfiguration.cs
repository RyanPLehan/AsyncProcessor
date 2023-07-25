using System;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using AsyncProcessor;


namespace AsyncProcessor.Azure.EventHub.Registration
{
    public static class ServicesConfiguration
    {
        public static void AddConsumer<TMessage>(this IServiceCollection services)
        {
            services.AddSingleton<IConsumer<TMessage>, Consumer<TMessage>>();
        }

        public static void AddProducer<TMessage>(this IServiceCollection services)
        {
            services.AddSingleton<IProducer<TMessage>, Producer<TMessage>>();
        }

        
        public static void AddAsyncProcessorProvider(this IServiceCollection services)
        {
            // This allows a specific type to be defined at the constructor (ie ILogger<mytype>)
            services.AddSingleton(typeof(IConsumer<>), typeof(Consumer<>));
            services.AddSingleton(typeof(IProducer<>), typeof(Producer<>));
        }
    }
}
