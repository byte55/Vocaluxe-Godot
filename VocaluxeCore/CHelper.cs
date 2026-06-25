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
using System.Globalization;
using System.IO;
using System.Linq;

namespace VocaluxeCore
{
    public static class CHelper
    {
        public static bool TryParse<T>(string value, out T result, bool ignoreCase = false)
            where T : struct
        {
            result = default(T);
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch { }

            return false;
        }

        public static bool TryParse(string value, out float result)
        {
            value = value.Replace(',', '.');
            return Single.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }

        private static readonly string[] _ImageFileExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        /// <summary>
        ///     Returns the file names (not full paths) of all image files in the given folder.
        /// </summary>
        public static IEnumerable<string> ListImageFiles(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(path)
                .Where(f => _ImageFileExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(Path.GetFileName);
        }
    }
}
