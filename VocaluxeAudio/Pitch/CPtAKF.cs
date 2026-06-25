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
using System.Reflection;
using System.Runtime.InteropServices;

namespace VocaluxeAudio.Pitch
{
    /// <summary>
    ///     Pitchtracker (Pt) that uses autocorrelation (AKF) and AMDF
    ///     Quite fast and perfect in artificial tests, but may fail in real world scenarios (e.g. missing fundamental)
    /// </summary>
    public class CPtAKF : CPitchTracker
    {
        static CPtAKF()
        {
            // Hosts like Godot run the app with a base dir that is not the managed-assembly dir, so the
            // default DllImport probing misses our bundled native lib. Resolve it explicitly: it sits next
            // to this assembly (Content-copied) under the platform-specific file name.
            NativeLibrary.SetDllImportResolver(typeof(CPtAKF).Assembly, _ResolveNative);
        }

        private static IntPtr _ResolveNative(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != NativeLib)
            {
                return IntPtr.Zero;
            }

            string fileName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "PitchTracker.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fileName = "libPitchTracker.dylib";
            }
            else
            {
                fileName = "libPitchTracker.so";
            }

            foreach (var dir in new[] { Path.GetDirectoryName(typeof(CPtAKF).Assembly.Location), AppContext.BaseDirectory })
            {
                if (string.IsNullOrEmpty(dir))
                {
                    continue;
                }

                var path = Path.Combine(dir, fileName);
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                {
                    return handle;
                }
            }

            return IntPtr.Zero; // fall back to default OS probing
        }

        #region Imports
        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PtAKF_Create(uint step);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_Free(IntPtr analyzer);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PtAKF_GetNumHalfTones();

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_InputByte(IntPtr analyzer, [In] byte[] data, int sampleCt);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_SetVolumeThreshold(IntPtr analyzer, float threshold);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern float PtAKF_GetVolumeThreshold(IntPtr analyzer);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PtAKF_GetNote(IntPtr analyzer, [Out] out float maxVolume, [Out] float[] weights);
        #endregion

        private IntPtr _Instance;

        public CPtAKF(uint step = 1024)
        {
            _Instance = PtAKF_Create(step);
        }

        public override int GetNumHalfTones()
        {
            return PtAKF_GetNumHalfTones();
        }

        public override float VolumeTreshold
        {
            get { return PtAKF_GetVolumeThreshold(_Instance); }
            set { PtAKF_SetVolumeThreshold(_Instance, value); }
        }

        public override void Input(byte[] data)
        {
            PtAKF_InputByte(_Instance, data, data.Length / 2);
        }

        public override int GetNote(out float maxVolume, float[] weights)
        {
            return PtAKF_GetNote(_Instance, out maxVolume, weights);
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            PtAKF_Free(_Instance);
            _Instance = IntPtr.Zero;
        }
    }
}
