using System;
using System.Collections.Generic;

namespace AsyncProcessor.Extensions
{
    public static class ListExtension
    {
        public static void AddIfNotNull<T>(this List<T> list, T value) where T : class
        {
            if (value != null)
                list.Add(value);
        }

        public static void AddRangeIfNotNull<T>(this List<T> list, IEnumerable<T> values) where T : class
        {
            if (values != null)
            {
                foreach (T value in values)
                    list.AddIfNotNull<T>(value);
            }
        }
    }
}
