using System;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;


namespace AsyncProcessor.Registration
{
    public static class ServiceCollectionExtension
    {
      
        public static IServiceCollection AddAsyncProcessor(this IServiceCollection services)
        {
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtension).Assembly));
            return services;
        }
    }
}
