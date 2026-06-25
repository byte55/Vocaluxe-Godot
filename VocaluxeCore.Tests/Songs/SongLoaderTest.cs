using System;
using System.IO;
using System.Linq;
using VocaluxeCore.Songs;

namespace VocaluxeCore.Tests.Songs
{
    [TestFixture]
    public class SongLoaderTest
    {
        private static string SampleTxt =>
            Path.Combine(AppContext.BaseDirectory, "TestFiles", "sample.txt");

        private static int TotalNotes(CSong song)
        {
            return song.Notes.GetVoice(0).Lines.Sum(l => l.NoteCount);
        }

        [Test]
        public void LoadSong_ParsesHeader()
        {
            var song = CSong.LoadSong(SampleTxt);

            Assert.That(song, Is.Not.Null, "LoadSong returned null");
            Assert.Multiple(() =>
            {
                Assert.That(song.Title, Is.EqualTo("Sample Song"));
                Assert.That(song.Artist, Is.EqualTo("Unit Tester"));
                Assert.That(song.Audio, Is.EqualTo("audio.mp3"));
                // BPM is stored multiplied by the internal _BpmFactor (4): 120 * 4 = 480
                Assert.That(song.Bpm, Is.EqualTo(480f).Within(0.001f));
                // GAP is given in ms in the file, stored in seconds: 1000 ms -> 1.0 s
                Assert.That(song.Gap, Is.EqualTo(1.0f).Within(0.001f));
                Assert.That(song.Languages, Does.Contain("English"));
                Assert.That(song.Genres, Is.EquivalentTo(new[] { "Test", "Demo" }));
            });
        }

        [Test]
        public void LoadNotes_BuildsLinesNotesAndTypes()
        {
            var song = CSong.LoadSong(SampleTxt);
            Assert.That(song, Is.Not.Null);

            Assert.That(song!.LoadNotes(), Is.True, "LoadNotes failed");

            Assert.That(song.Notes.VoiceCount, Is.EqualTo(1));
            Assert.That(song.IsDuet, Is.False);

            var voice = song.Notes.GetVoice(0);
            Assert.That(voice.NumLines, Is.EqualTo(2), "expected one line break");
            Assert.That(TotalNotes(song), Is.EqualTo(5), "5 notes total (3 + 2)");

            // First line keeps its notes in beat order with verbatim text (Lyrics is their concatenation)
            Assert.That(voice.Lines[0].Notes.Select(n => n.Text), Is.EqualTo(new[] { "Hel", "lo", "World" }));
            Assert.That(voice.Lines[0].Lyrics, Is.EqualTo("HelloWorld"));

            // The '*' note is golden, the 'F' note is freestyle (and reports tone 0)
            var allNotes = voice.Lines.SelectMany(l => l.Notes).ToList();
            Assert.That(allNotes.Count(n => n.Type == ENoteType.Golden), Is.EqualTo(1));
            var freestyle = allNotes.Single(n => n.Type == ENoteType.Freestyle);
            Assert.That(freestyle.StartBeat, Is.EqualTo(16));
        }

        // Integration check against a real UltraStar song. Skips cleanly when the local
        // (gitignored) song library is not present, so CI / clean checkouts stay green.
        [Test]
        public void LoadSong_RealUltraStarFile_Pocahontas()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var txt = Path.Combine(repoRoot, "songs", "AnnenMayKantereit - Pocahontas", "AnnenMayKantereit - Pocahontas.txt");

            if (!File.Exists(txt))
            {
                Assert.Ignore("Real song library not present at " + txt);
            }

            var song = CSong.LoadSong(txt);
            Assert.That(song, Is.Not.Null, "LoadSong returned null for real file");

            Assert.Multiple(() =>
            {
                Assert.That(song!.Title, Is.EqualTo("Pocahontas"));
                Assert.That(song.Artist, Is.EqualTo("AnnenMayKantereit"));
                Assert.That(song.Bpm, Is.EqualTo(232.17f * 4).Within(0.05f));
                Assert.That(song.Gap, Is.EqualTo(10.82f).Within(0.005f));
                Assert.That(song.Languages, Does.Contain("German"));
                Assert.That(song.Genres, Does.Contain("Rock"));
                // #MEDLEYSTARTBEAT + #MEDLEYENDBEAT are both present -> medley comes from the tags
                Assert.That(song.Medley.Source, Is.EqualTo(EDataSource.Tag));
            });

            Assert.That(song!.LoadNotes(), Is.True, "LoadNotes failed for real file");
            Assert.That(song.Notes.VoiceCount, Is.EqualTo(1));
            Assert.That(TotalNotes(song), Is.GreaterThan(100), "full song should have many notes");
        }
    }
}
