using NUnit.Framework;
using VocaluxeCore.Songs;

namespace VocaluxeCore.Tests.Songs
{
    [TestFixture]
    public class NoteModelTest
    {
        [Test]
        public void PointsForBeat_MatchesNoteType()
        {
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.Normal).PointsForBeat, Is.EqualTo(1));
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.Rap).PointsForBeat, Is.EqualTo(1));
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.Golden).PointsForBeat, Is.EqualTo(2));
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.RapGolden).PointsForBeat, Is.EqualTo(2));
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.Freestyle).PointsForBeat, Is.EqualTo(0));
        }

        [Test]
        public void Points_AreBeatsTimesDuration()
        {
            Assert.That(new CSongNote(0, 3, 5, "a", ENoteType.Normal).Points, Is.EqualTo(3));
            Assert.That(new CSongNote(0, 2, 5, "a", ENoteType.Golden).Points, Is.EqualTo(4));
        }

        [Test]
        public void EndBeat_IsStartPlusDurationMinusOne()
        {
            var note = new CSongNote(10, 4, 5, "x", ENoteType.Normal);
            Assert.That(note.EndBeat, Is.EqualTo(13));
        }

        [Test]
        public void RapNote_AlwaysReportsToneZero()
        {
            var rap = new CSongNote(0, 1, 7, "r", ENoteType.Rap);
            Assert.That(rap.Tone, Is.EqualTo(0));
            Assert.That(rap.IsRapNote, Is.True);
            Assert.That(rap.IsGoldenNote, Is.False);
        }

        [Test]
        public void Tone_OutOfRange_IsIgnored()
        {
            // Constructor sets tone 5, then an out-of-range assignment must be rejected (kept at 5).
            var note = new CSongNote(0, 1, 5, "n", ENoteType.Normal) { Tone = CSettings.ToneMax + 100 };
            Assert.That(note.Tone, Is.EqualTo(5));
            note.Tone = CSettings.ToneMin - 100;
            Assert.That(note.Tone, Is.EqualTo(5));
        }

        [Test]
        public void Line_LyricsAggregateNoteText_AndPointsSum()
        {
            var line = new CSongLine();
            Assert.That(line.AddNote(new CSongNote(0, 2, 5, "Hel", ENoteType.Normal)), Is.True);
            Assert.That(line.AddNote(new CSongNote(2, 2, 5, "lo", ENoteType.Normal)), Is.True);
            Assert.That(line.Lyrics, Is.EqualTo("Hello"));
            Assert.That(line.Points, Is.EqualTo(4));
        }

        [Test]
        public void Line_RejectsOverlappingNote()
        {
            var line = new CSongLine();
            Assert.That(line.AddNote(new CSongNote(0, 4, 5, "a", ENoteType.Normal)), Is.True);
            // Starts at beat 2, but previous note occupies beats 0..3 -> overlap -> rejected.
            Assert.That(line.AddNote(new CSongNote(2, 2, 5, "b", ENoteType.Normal)), Is.False);
        }

        [Test]
        public void Voice_SumsPointsAcrossLines()
        {
            var voice = new CVoice();
            voice.AddNote(new CSongNote(0, 2, 5, "a", ENoteType.Normal));
            voice.AddNote(new CSongNote(10, 2, 5, "b", ENoteType.Golden));
            // Normal(2*1) + Golden(2*2) = 6
            Assert.That(voice.Points, Is.EqualTo(6));
        }
    }
}
