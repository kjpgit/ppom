using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Globalization;

namespace ppom
{
    public class Extensions
    {
        /// <summary>
        /// Yield (index, value) for all items in input.
        /// By default, the index starts at 0.  
        /// </summary>
        public static IEnumerable<(int, T)> Enumerate<T>(
            IEnumerable<T> input, int start = 0)
        {
            int i = start;
            foreach (var t in input) {
                yield return (i++, t);
            }
        }

        /// <summary>
        /// Return the number of digits to the right of the decimal point (may be 0)
        /// </summary>
        public static int GetDecimalPlaces(decimal d)
        {
            var s = d.ToString(CultureInfo.InvariantCulture);
            var index = s.IndexOf(".");
            if (index < 0) {
                return 0;
            } else {
                return s.Length - index - 1;
            }
        }
    }
}