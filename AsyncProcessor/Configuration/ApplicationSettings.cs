using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsyncProcessor.Configuration
{
    public static class ApplicationSettings
    {
        public static void LoadSettingsFile(HostBuilderContext context, IConfigurationBuilder builder)
        {
            string appEnv = EnvironmentSettings.ApplicationEnvironment();

            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var file = $"appsettings.json";
            builder.AddJsonFile(Path.Combine(path, file), true, true);


            if (!String.IsNullOrWhiteSpace(appEnv))
            {
                file = $"appsettings.{appEnv}.json";
                builder.AddJsonFile(Path.Combine(path, file), true, true);
            }

            // See following reference for incorporating user secrets
            // https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows#register-the-user-secrets-configuration-source
            // https://blog.elmah.io/asp-net-core-not-that-secret-user-secrets-explained/
            if (context.HostingEnvironment.IsDevelopment() ||
                appEnv.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddUserSecrets(Assembly.GetEntryAssembly(), true, true);
            }
        }
    }
}
