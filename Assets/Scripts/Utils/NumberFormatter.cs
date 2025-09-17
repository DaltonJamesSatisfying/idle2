using System;
using System.Collections.Generic;
using System.Text;

namespace IdleFramework.Utils
{
    /// <summary>
    /// Formats large floating point values using short suffixes suitable for idle games.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = BuildSuffixes();

        /// <summary>
        /// Formats the provided value with suffixes such as K, M, B or aa.
        /// </summary>
        /// <param name="value">Value to format.</param>
        /// <returns>Human readable string.</returns>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "0";
            }

            var abs = Math.Abs(value);
            if (abs < 1000d)
            {
                return value.ToString(abs >= 100d ? "0" : abs >= 10d ? "0.0" : "0.00");
            }

            var index = 0;
            while (abs >= 1000d && index < Suffixes.Length)
            {
                value /= 1000d;
                abs /= 1000d;
                index++;
            }

            if (index == 0)
            {
                return value.ToString("0.00");
            }

            if (index > Suffixes.Length)
            {
                return value.ToString("0.###E+0");
            }

            var suffix = index - 1 < Suffixes.Length ? Suffixes[index - 1] : null;
            if (suffix == null)
            {
                return value.ToString("0.###E+0");
            }

            return $"{value:0.00}{suffix}";
        }

        private static string[] BuildSuffixes()
        {
            var list = new List<string> { "K", "M", "B", "T" };
            for (var first = 'a'; first <= 'z'; first++)
            {
                for (var second = 'a'; second <= 'z'; second++)
                {
                    list.Add(new string(new[] { first, second }));
                }
            }

            return list.ToArray();
        }
    }
}
