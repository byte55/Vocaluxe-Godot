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
using VocaluxeAudio.Pitch;

namespace VocaluxeAudio.Record
{
    /// <summary>
    ///     Per-player capture buffer: feeds recorded PCM into a pitch tracker and exposes the
    ///     detected tone / volume / weights.
    /// </summary>
    public class CBuffer : IDisposable
    {
        private CPitchTracker _PitchTracker = new CPtAKF();

        private float _MaxVolume;

        public CBuffer()
        {
            VolTreshold = 0.02f;
            ToneWeigths = new float[GetNumHalfTones()];
            Reset();
        }

        ~CBuffer()
        {
            _Dispose(false);
        }

        public int GetNumHalfTones()
        {
            return _PitchTracker.GetNumHalfTones();
        }

        public int ToneAbs { get; private set; }

        public int Tone { get; set; }

        public float MaxVolume
        {
            get { return _MaxVolume; }
        }

        public bool ToneValid { get; private set; }

        public float[] ToneWeigths { get; private set; }

        /// <summary>
        ///     Minimum volume for a tone to be valid
        /// </summary>
        public float VolTreshold
        {
            get { return _PitchTracker.VolumeTreshold; }
            set { _PitchTracker.VolumeTreshold = value; }
        }

        public void Reset()
        {
            ToneValid = false;
            ToneAbs = 0;
            Tone = 0;
            for (var i = 0; i < ToneWeigths.Length; i++)
            {
                ToneWeigths[i] = 0f;
            }
        }

        /// <summary>
        ///     Software input gain (mic boost). PortAudio opens raw ALSA hw devices, bypassing the
        ///     system mixer gain, so quiet mics need boosting here. 1 = unchanged.
        /// </summary>
        public float Gain = 1f;

        public void ProcessNewBuffer(byte[] buffer)
        {
            if (Gain != 1f)
            {
                for (var i = 0; i + 1 < buffer.Length; i += 2)
                {
                    var s = (short)(buffer[i] | (buffer[i + 1] << 8));
                    var v = (int)Math.Round(s * Gain);
                    if (v > short.MaxValue)
                    {
                        v = short.MaxValue;
                    }
                    else if (v < short.MinValue)
                    {
                        v = short.MinValue;
                    }

                    buffer[i] = (byte)(v & 0xFF);
                    buffer[i + 1] = (byte)((v >> 8) & 0xFF);
                }
            }

            _PitchTracker.Input(buffer);
        }

        public void AnalyzeBuffer()
        {
            var tone = _PitchTracker.GetNote(out _MaxVolume, ToneWeigths);
            if (tone >= 0)
            {
                ToneAbs = tone;
                Tone = ToneAbs % 12;
                ToneValid = true;
            }
            else
            {
                ToneValid = false;
            }
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void _Dispose(bool disposing)
        {
            if (disposing && _PitchTracker != null)
            {
                _PitchTracker.Dispose();
                _PitchTracker = null;
            }
        }
    }
}
