using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncProcessor.Configuration;
using AsyncProcessor.Registration;
using AsyncProcessor.Azure.EventHub.Configuration;
using AsyncProcessor.Azure.EventHub.Registration;


namespace AsyncProcessor.Azure.EventHub.Example.Producer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();
            host.RunAsync().GetAwaiter().GetResult();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(ApplicationSettings.LoadSettingsFile)
                // Need to run as Windows Service, not just console app. (Microsoft.Extensions.Hosting.WindowsServices)
                //.UseWindowsService(ConfigureWindowService)
                .ConfigureLogging(ConfigureLogger)
                // Use serilog as the logging provider (Serilog.Extensions.Hosting)
                //.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureServices(ConfigureServices);

            return hostBuilder;
        }

        public static void ConfigureLogger(HostBuilderContext context, ILoggingBuilder builder)
        {
            // Remove MS Logger
            builder.ClearProviders();

            // Use Specific MS Loggers
            builder.AddConsole();
            builder.AddDebug();

            // Use Serilog
            /*
            var logger = new LoggerConfiguration().ReadFrom
                                                  .Configuration(context.Configuration)
                                                  .CreateLogger();
            builder.AddSerilog(logger, true);
            */
        }


        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.Configure<ProducerSettings>(context.Configuration.GetSection("PubSub:Producer"));
            services.AddAsyncProcessor();
            services.AddAsyncProcessorProvider();
            services.AddHostedService<Worker>();
        }
    }
}
