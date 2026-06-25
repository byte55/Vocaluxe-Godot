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

using System.Collections.Generic;
using System.Linq;

namespace VocaluxeCore
{
    public static class Extensions
    {
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
    }
}
