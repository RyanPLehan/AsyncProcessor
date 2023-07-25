using System;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;


namespace AsyncProcessor.Registration
{
    public static class ServicesConfiguration
    {
      
        public static void AddAsyncProcessor(this IServiceCollection services)
        {
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ServicesConfiguration).Assembly));
        }
    }
}
