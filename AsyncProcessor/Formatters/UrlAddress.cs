using System;


namespace AsyncProcessor.Formatters
{
    public static class UrlAddress
    {
        public static string CreateEndpoint(string baseUri, string relativeUri)
        {
            const char FORWARD_SLASH = '/';

            // Strip trailing / from base uri
            if (!String.IsNullOrWhiteSpace(baseUri) &&
                baseUri.EndsWith(FORWARD_SLASH))
                baseUri = baseUri.Substring(0, baseUri.Length - 1);

            // Strip leading / from relative uri
            if (!String.IsNullOrWhiteSpace(relativeUri) &&
                relativeUri.StartsWith(FORWARD_SLASH))
                relativeUri = relativeUri.Substring(1, relativeUri.Length - 1);

            // Create uri
            return String.Format("{0}{1}{2}", baseUri, FORWARD_SLASH, relativeUri);
        }
    }
}
