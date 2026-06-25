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

namespace VocaluxeCore.Game
{
    /// <summary>
    ///     Portable beat &lt;-&gt; time conversion (ported from the app's CGame). Scoring logic will grow here.
    /// </summary>
    public static class CGame
    {
        public static float GetBeatFromTime(float time, float bpm, float gap)
        {
            return bpm / 60 * (time - gap);
        }

        public static float GetTimeFromBeats(float beats, float bpm)
        {
            if (bpm > 0f)
            {
                return beats / bpm * 60f;
            }

            return 0f;
        }
    }
}
