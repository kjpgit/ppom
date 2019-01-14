using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ppom
{
    class Extensions
    {
        public static IEnumerable<(int, T)> Enumerate<T>(IEnumerable<T> input)
        {
            int i = 0;
            foreach (var t in input) {
                yield return (i++, t);
            }
        }

        public static int GetDecimalPlaces(decimal d)
        {
            var s = d.ToString();
            var index = s.IndexOf(".");
            if (index < 0) {
                return 0;
            } else {
                return s.Length - index - 1;
            }
        }
    }
}