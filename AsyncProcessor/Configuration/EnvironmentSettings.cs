using System;


namespace AsyncProcessor.Configuration
{
    /// <summary>
    /// According to MS documentation, overriding Environment Variable Name with a double underscore (__) will be translated to a dot (.).
    /// https://docs.microsoft.com/en-us/azure/azure-functions/functions-host-json#override-hostjson-values
    /// </summary>
    public static class EnvironmentSettings
    {
        public static bool ExistsVariable(string variableName)
        {
            return !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName));
        }

        public static string? GetValue(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

        public static string ApplicationEnvironment()
        {
            string envValue = String.Empty;

            // .net core defaults to production if no ASPNETCORE_ENVIRONMENT environment variable is set
            // this overriding .net cores default behvoir
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1
            envValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            return envValue;
        }

    }
}
