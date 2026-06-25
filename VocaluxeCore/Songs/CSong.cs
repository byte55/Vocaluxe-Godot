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
using System.IO;
using System.Text;
using VocaluxeCore.Log;

namespace VocaluxeCore.Songs
{
    [Flags]
    internal enum EHeaderFlags
    {
        Title = 1,
        Artist = 2,
        MP3 = 4,
        Instrumental = 5,
        Vocals = 6,
        Bpm = 8,
        MedleyStartBeat = 16,
        MedleyEndBeat = 32
    }

    public enum EDataSource
    {
        None = 0,
        Calculated,
        Tag
    }

    public struct SMedley
    {
        public EDataSource Source;
        public int StartBeat;
        public int EndBeat;
        public float FadeInTime;
        public float FadeOutTime;
    }

    public struct SShortEnd
    {
        public EDataSource Source;
        public int EndBeat;
    }

    public struct SPreview
    {
        public EDataSource Source;
        public float StartTime;
    }

    /// <summary>
    ///     Portable subset of the Vocaluxe song model. Engine-coupled features from the original
    ///     (cover textures, DB lookups, game-mode list, medley/preview/short-end auto-calculation)
    ///     are intentionally omitted/stubbed for the rewrite's loading slice. All file-format data
    ///     fields are kept so round-trip and the remaining features can be added later.
    /// </summary>
    public partial class CSong
    {
        public SMedley Medley;

        private bool _CalculateMedley = true;
        public SPreview Preview;

        public SShortEnd ShortEnd;

        public Encoding Encoding = new UTF8Encoding();
        public bool ManualEncoding;
        public string Folder = string.Empty;
        public string FolderName = string.Empty;
        public string FileName = string.Empty;
        public bool Relative;

        public string Audio = string.Empty;
        public string Instrumental = string.Empty;
        public string Vocals = string.Empty;
        public string Cover = string.Empty;
        public readonly List<string> BackgroundFileNames = new();
        public string Video = string.Empty;

        public EAspect VideoAspect = EAspect.Automatic;

        public bool NotesLoaded { get; private set; }

        public string Title = string.Empty;
        public string Artist = string.Empty;

        public string TitleSorting = string.Empty;
        public string ArtistSorting = string.Empty;

        public string Version = "";
        public string Length = ""; //Length set in song file, SHOULD match actual song length but is more a hint
        public string Source = "";
        public readonly List<string> UnknownTags = new();

        /// <summary>
        ///     Start of the song in s (s in txt)
        /// </summary>
        public float Start;
        /// <summary>
        ///     End of the song in s (ms in txt)
        /// </summary>
        public float End;

        public float Bpm = 1f;
        /// <summary>
        ///     Gap of the mp3 in s (ms in txt)
        /// </summary>
        public float Gap;
        /// <summary>
        ///     Gap of the video in s (s in txt)
        /// </summary>
        public float VideoGap;

        private string _Comment = "";

        // Sorting
        public int Id;
        public bool IsDuet => Notes.VoiceCount > 1;
        public bool IsRap = false;

        public readonly List<string> Creators = new();
        public readonly List<string> Editions = new();
        public readonly List<string> Genres = new();
        public readonly List<string> Tags = new();
        public readonly List<string> Languages = new();
        public string Album = "";
        public string Year = "";

        public int DataBaseSongId = -1;
        public DateTime DateAdded = DateTime.Today;
        public int NumPlayed;
        public int NumPlayedSession;

        // Notes
        public readonly CNotes Notes = new();

        //No point creating a song without a text file --> Use factory method LoadSong
        private CSong() { }

        public static CSong LoadSong(string filePath)
        {
            var song = new CSong();
            var loader = new CSongLoader(song);
            return loader.InitPaths(filePath) && loader.ReadHeader() ? song : null;
        }

        public bool LoadNotes()
        {
            var loader = new CSongLoader(this);
            return loader.ReadNotes();
        }

        public string GetMP3()
        {
            return Path.Combine(Folder, Audio);
        }

        public string GetInstrumental()
        {
            return Path.Combine(Folder, Instrumental);
        }

        public bool HasInstrumental()
        {
            return !string.IsNullOrEmpty(Instrumental);
        }

        public string GetVocals()
        {
            return Path.Combine(Folder, Vocals);
        }

        public bool HasVocals()
        {
            return !string.IsNullOrEmpty(Vocals);
        }

        public string GetVideo()
        {
            return Path.Combine(Folder, Video);
        }

        private void _CheckFiles()
        {
            if (Cover == "")
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[CO]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        Cover = file;
                    }
                }
            }

            if (BackgroundFileNames.Count == 0)
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[BG]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        BackgroundFileNames.Add(file);
                    }
                }
            }
        }

        private void _CheckDuet()
        {
            for (var i = 0; i < Notes.VoiceCount; i++)
            {
                if (!Notes.VoiceNames.IsSet(i))
                {
                    CLog.Error("Warning: Can't find #P" + (i + 1) + "-tag for duets in \"" + Artist + " - " + Title + "\".");
                }
            }
        }

        // TODO(godot-rework): port medley/preview/short-end auto-calculation. These depend on the app's
        // CGame/CSettings beat math and are not needed for the loading slice. Explicit header tags
        // (#MEDLEYSTARTBEAT/#PREVIEWSTART/#ENDSHORT) are still parsed in ReadHeader.
        private void _CalcMedley() { }
        private void _CheckPreview() { }
        private void _FindShortEnd() { }
    }
}
