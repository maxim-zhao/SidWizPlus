using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibSidWiz
{
    public static class Extensions
    {
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            // Materialise the collection if necessary
            var list = source.ToList();
            // Find the longest sequence of digits
            var max = list
                .SelectMany(i => Regex
                    .Matches(selector(i), @"\d+")
                    .Cast<Match>()
                    .Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            // Pad all number sequences to that length, then order by this padded string
            return list.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }

    }
}
