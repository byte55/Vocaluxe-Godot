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
using System.Runtime.InteropServices;
using PortAudioSharp;
using VocaluxeCore.Log;

namespace VocaluxeAudio.Record
{
    /// <summary>
    ///     PortAudio capture backend (PortAudioSharp2, OO API). Opens one input stream per device that
    ///     has at least one channel assigned to a player; the stream callback forwards interleaved PCM
    ///     to CRecordBase._HandleData, which de-interleaves and routes channels to per-player buffers.
    /// </summary>
    public class CPortAudioRecord : CRecordBase, IDisposable
    {
        private bool _PaInitialized;
        private readonly List<Stream> _Streams = new List<Stream>();

        // Keep references so the GC does not collect the delegates while native code holds them.
        private readonly List<Stream.Callback> _Callbacks = new List<Stream.Callback>();

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            try
            {
                PortAudio.LoadNativeLibrary();
                PortAudio.Initialize();
                _PaInitialized = true;

                var count = PortAudio.DeviceCount;
                for (var i = 0; i < count; i++)
                {
                    var info = PortAudio.GetDeviceInfo(i);
                    if (info.maxInputChannels > 0)
                    {
                        _Devices.Add(new CRecordDevice(i, info.name, info.name + i, info.maxInputChannels));
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                CLog.Error("Error initializing PortAudio: " + e.Message);
                Close();
                return false;
            }
        }

        /// <summary>
        ///     Returns the system default input device (or the first input device), or null if none.
        /// </summary>
        public CRecordDevice DefaultInputDevice()
        {
            if (_Devices == null || _Devices.Count == 0)
            {
                return null;
            }

            int def;
            try
            {
                def = PortAudio.DefaultInputDevice;
            }
            catch
            {
                def = -1;
            }

            foreach (var d in _Devices)
            {
                if (d.Id == def)
                {
                    return d;
                }
            }

            return _Devices[0];
        }

        /// <summary>
        ///     Opens and starts a capture stream for every device that has a player assigned.
        /// </summary>
        public bool Start()
        {
            if (!_PaInitialized)
            {
                return false;
            }

            Stop();

            foreach (var buffer in _Buffer)
            {
                buffer.Reset();
            }

            for (var dev = 0; dev < _Devices.Count; dev++)
            {
                var device = _Devices[dev];

                var used = false;
                for (var ch = 0; ch < device.Channels; ch++)
                {
                    if (device.PlayerChannel[ch] > 0)
                    {
                        used = true;
                    }
                }

                if (!used)
                {
                    continue;
                }

                var info = PortAudio.GetDeviceInfo(device.Id);
                StreamParameters? inParams = new StreamParameters
                {
                    device = device.Id,
                    channelCount = device.Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = info.defaultLowInputLatency,
                    hostApiSpecificStreamInfo = IntPtr.Zero
                };

                try
                {
                    // Per-device closure over `device`; avoids PortAudioSharp2's object-userData marshalling
                    // (it hands the callback a GCHandle pointer, not our value).
                    Stream.Callback cb = (IntPtr input, IntPtr output, uint frameCount,
                        ref StreamCallbackTimeInfo ti, StreamCallbackFlags flags, IntPtr userData) => _OnData(device, input, frameCount);
                    _Callbacks.Add(cb);
                    var stream = new Stream(inParams, null, 44100, 882, StreamFlags.NoFlag, cb, null);
                    stream.Start();
                    _Streams.Add(stream);
                }
                catch (Exception e)
                {
                    CLog.Error("Error starting PortAudio stream for device " + device.Name + ": " + e.Message);
                    Stop();
                    return false;
                }
            }

            return true;
        }

        public bool Stop()
        {
            foreach (var stream in _Streams)
            {
                try
                {
                    if (!stream.IsStopped)
                    {
                        stream.Stop();
                    }

                    stream.Dispose();
                }
                catch (Exception e)
                {
                    CLog.Error("Error stopping PortAudio stream: " + e.Message);
                }
            }

            _Streams.Clear();
            _Callbacks.Clear();
            return true;
        }

        public override void Close()
        {
            Stop();

            if (_PaInitialized)
            {
                try
                {
                    PortAudio.Terminate();
                }
                catch (Exception e)
                {
                    CLog.Error("Error terminating PortAudio: " + e.Message);
                }

                _PaInitialized = false;
            }

            base.Close();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        private StreamCallbackResult _OnData(CRecordDevice device, IntPtr input, uint frameCount)
        {
            try
            {
                if (frameCount > 0 && input != IntPtr.Zero)
                {
                    var numBytes = (int)frameCount * device.Channels * 2;
                    var recBuffer = new byte[numBytes];
                    Marshal.Copy(input, recBuffer, 0, numBytes);
                    _HandleData(device, recBuffer);
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error on PortAudio record callback: " + e);
            }

            return StreamCallbackResult.Continue;
        }
    }
}
