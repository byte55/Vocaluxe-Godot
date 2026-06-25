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

namespace VocaluxeAudio.Record
{
    public class CRecordDevice
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string Driver;

        public readonly int Channels;

        /// <summary>
        ///     Per-channel player assignment (1-based player number; 0 = channel unused).
        /// </summary>
        public int[] PlayerChannel;

        public CRecordDevice(int id, string name, string driver, int channels)
        {
            Id = id;
            Name = name;
            Driver = driver;
            Channels = channels;
            PlayerChannel = new int[channels];
        }
    }
}
