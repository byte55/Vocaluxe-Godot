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
using System.IO;
using System.Text.RegularExpressions;

namespace VocaluxeCore.Log
{
    /// <summary>
    ///     Portable logger. Writes timestamped, levelled lines to a log file (thread-safe, so the
    ///     PortAudio capture thread can log too) and optionally echoes to host sinks (e.g. the Godot
    ///     console). Also serves the structured API the ported song code expects (CSongLog, Params).
    /// </summary>
    public static class CLog
    {
        // Optional host echoes (e.g. wire to GD.Print / GD.PrintErr).
        public static Action<string> InfoSink;
        public static Action<string> WarningSink;
        public static Action<string> ErrorSink;

        public static readonly CCategoryLog CSongLog = new CCategoryLog();

        private static readonly object _Lock = new object();
        private static StreamWriter _Writer;

        /// <summary>Open (or replace) the log file at the given path.</summary>
        public static void InitFile(string path)
        {
            lock (_Lock)
            {
                try
                {
                    _Writer?.Dispose();
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    _Writer = new StreamWriter(path, false) { AutoFlush = true };
                    _Write("INFO", "log started: " + path);
                }
                catch
                {
                    _Writer = null;
                }
            }
        }

        public static void Close()
        {
            lock (_Lock)
            {
                _Writer?.Dispose();
                _Writer = null;
            }
        }

        public static object[] Params(params object[] values)
        {
            return values ?? new object[0];
        }

        public static void Debug(string template, object[] args = null)
        {
            _Write("DEBUG", Format(template, args));
        }

        public static void Info(string template, object[] args = null)
        {
            var msg = Format(template, args);
            _Write("INFO", msg);
            InfoSink?.Invoke(msg);
        }

        public static void Warning(string template, object[] args = null)
        {
            var msg = Format(template, args);
            _Write("WARN", msg);
            WarningSink?.Invoke(msg);
        }

        public static void Error(string template, object[] args = null)
        {
            var msg = Format(template, args);
            _Write("ERROR", msg);
            ErrorSink?.Invoke(msg);
        }

        public static void Error(Exception e, string template, object[] args = null)
        {
            var msg = Format(template, args) + " :: " + e;
            _Write("ERROR", msg);
            ErrorSink?.Invoke(msg);
        }

        private static void _Write(string level, string msg)
        {
            lock (_Lock)
            {
                _Writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {msg}");
            }
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
            public void Debug(string template, object[] args = null)
            {
                CLog.Debug(template, args);
            }

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
