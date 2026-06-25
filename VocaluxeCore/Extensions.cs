#region license
// This file is part of Vocaluxe.
//
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VocaluxeCore
{
    public static class Extensions
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }

            if (val.CompareTo(max) > 0)
            {
                return max;
            }

            return val;
        }

        /// <summary>
        ///     Makes the list at least the given size filling it with the given default value
        /// </summary>
        public static void EnsureSize<T>(this List<T> list, int size, T defaultValue)
        {
            var curSize = list.Count;
            if (size > curSize)
            {
                list.AddRange(Enumerable.Repeat(defaultValue, size - curSize));
            }
        }

        /// <summary>
        ///     Gets all set bits starting from lowest (Bit 0). Used for UltraStar duet player bitmasks (P1/P2/P4...).
        /// </summary>
        public static IEnumerable<int> GetSetBits(this int value)
        {
            var result = new List<int>();
            var curBit = 0;
            //Evaluate as bitset
            while (value > 0)
            {
                if ((value & 1) != 0)
                {
                    result.Add(curBit);
                }

                value >>= 1;
                curBit++;
            }

            return result;
        }

        public static bool ContainsIgnoreCase(this string value, string other)
        {
            return value.IndexOf(other, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static readonly Regex _MultipleWhiteSpaceRegEx = new Regex(@" {2,}");

        public static string TrimMultipleWs(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return _MultipleWhiteSpaceRegEx.Replace(value, " ");
        }
    }
}
