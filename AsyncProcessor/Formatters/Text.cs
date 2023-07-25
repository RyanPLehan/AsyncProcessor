using System;
using System.Globalization;

namespace AsyncProcessor.Formatters
{
    public static class Text
    {
        public static string ToPascalCase(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return text;

            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            return ti.ToTitleCase(text);
        }
    }
}
