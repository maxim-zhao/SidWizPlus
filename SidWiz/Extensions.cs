using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SidWiz
{
    static class Extensions
    {
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            var list = source.ToList();
            int max = list.SelectMany(i => Regex.Matches(selector(i), @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length)).Max() ?? 0;

            return list.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }

    }
}
