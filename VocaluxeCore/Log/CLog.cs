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
using System.Text.RegularExpressions;

namespace VocaluxeCore.Log
{
    /// <summary>
    ///     Minimal portable logging shim mirroring the API surface that the ported song code uses
    ///     (CLog.Warning/Error, CLog.CSongLog.*, CLog.Params). Sinks default to null (silent), so the
    ///     library is side-effect free unless a host wires them up.
    /// </summary>
    public static class CLog
    {
        public static Action<string> WarningSink;
        public static Action<string> ErrorSink;

        public static readonly CCategoryLog CSongLog = new CCategoryLog();

        public static object[] Params(params object[] values)
        {
            return values ?? new object[0];
        }

        public static void Warning(string template, object[] args = null)
        {
            WarningSink?.Invoke(Format(template, args));
        }

        public static void Error(string template, object[] args = null)
        {
            ErrorSink?.Invoke(Format(template, args));
        }

        public static void Error(Exception e, string template, object[] args = null)
        {
            ErrorSink?.Invoke(Format(template, args) + " :: " + e.Message);
        }

        private static readonly Regex _Token = new Regex(@"\{[^{}]+\}");

        internal static string Format(string template, object[] args)
        {
            if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
            {
                return template ?? "";
            }

            var i = 0;
            return _Token.Replace(template, m => i < args.Length ? (args[i++]?.ToString() ?? "null") : m.Value);
        }

        public class CCategoryLog
        {
            public void Warning(string template, object[] args = null)
            {
                CLog.Warning(template, args);
            }

            public void Error(string template, object[] args = null)
            {
                CLog.Error(template, args);
            }

            public void Error(Exception e, string template, object[] args = null)
            {
                CLog.Error(e, template, args);
            }
        }
    }
}
