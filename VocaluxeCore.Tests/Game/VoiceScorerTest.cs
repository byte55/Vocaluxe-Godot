using System;
using System.IO;
using System.Linq;
using VocaluxeCore.Game;
using VocaluxeCore.Songs;

namespace VocaluxeCore.Tests.Game
{
    [TestFixture]
    public class VoiceScorerTest
    {
        private static (bool valid, int tone) ExpectedToneAt(CVoice v, int beat)
        {
            var li = v.FindPreviousLine(beat);
            if (li < 0)
            {
                return (false, 0);
            }

            var line = v.Lines[li];
            if (line.EndBeat < beat)
            {
                return (false, 0);
            }

            var ni = line.FindPreviousNote(beat);
            if (ni < 0)
            {
                return (false, 0);
            }

            var n = line.Notes[ni];
            if (n.EndBeat < beat)
            {
                return (false, 0);
            }

            return (n.PointsForBeat > 0, n.Tone);
        }

        /// <summary>Sing every expected note exactly (perfect performance).</summary>
        private static CVoiceScorer ScorePerfect(CVoice voice, EGameDifficulty diff = EGameDifficulty.TR_CONFIG_NORMAL)
        {
            var scorer = new CVoiceScorer(voice, diff);
            var lastBeat = voice.Lines.Last().LastNoteBeat;
            for (var beat = 0; beat <= lastBeat; beat++)
            {
                var (valid, tone) = ExpectedToneAt(voice, beat);
                scorer.Update(beat, valid, tone);
            }

            return scorer;
        }

        [Test]
        public void PerfectSingleNote_ScoresFullMaxScore()
        {
            var voice = new CVoice();
            voice.AddNote(new CSongNote(0, 4, 10, "la", ENoteType.Normal));

            var score = ScorePerfect(voice).Score;

            // All note points (MaxScore - LinebonusScore = 9000) + full line bonus (1000) = MaxScore.
            Assert.That(score.Points, Is.EqualTo(CSettings.MaxScore).Within(0.01));
            Assert.That(score.PointsLineBonus, Is.EqualTo(CSettings.LinebonusScore).Within(0.01));
            Assert.That(score.SungLines[0].PerfectLine, Is.True);
        }

        [Test]
        public void WrongTone_ScoresNothing_ButRecordsMiss()
        {
            var voice = new CVoice();
            voice.AddNote(new CSongNote(0, 4, 10, "la", ENoteType.Normal));

            var scorer = new CVoiceScorer(voice, EGameDifficulty.TR_CONFIG_NORMAL);
            for (var beat = 0; beat <= 3; beat++)
            {
                scorer.Update(beat, true, 0); // expected tone 10, sung 0 -> diff 10 > tolerance
            }

            Assert.That(scorer.Score.Points, Is.EqualTo(0));
            Assert.That(scorer.Score.SungLines, Is.Not.Empty);
            Assert.That(scorer.Score.SungLines[0].Notes.All(n => !n.Hit), "miss notes must not be marked as hits");
        }

        [Test]
        public void RapNote_HitsRegardlessOfTone_EvenOnHard()
        {
            var voice = new CVoice();
            voice.AddNote(new CSongNote(0, 4, 0, "yo", ENoteType.Rap));

            // Hard difficulty = 0 half-tone tolerance, yet rap ignores pitch entirely.
            var scorer = new CVoiceScorer(voice, EGameDifficulty.TR_CONFIG_HARD);
            for (var beat = 0; beat <= 3; beat++)
            {
                scorer.Update(beat, true, 42); // wildly wrong tone
            }

            Assert.That(scorer.Score.Points, Is.EqualTo(CSettings.MaxScore).Within(0.01));
        }

        [Test]
        public void HitTolerance_DependsOnDifficulty()
        {
            // Expected tone 10, sung tone 12 -> diff 2: hit on Easy (<=2), miss on Normal (<=1).
            CVoiceScorer Run(EGameDifficulty diff)
            {
                var voice = new CVoice();
                voice.AddNote(new CSongNote(0, 2, 10, "a", ENoteType.Normal));
                var scorer = new CVoiceScorer(voice, diff);
                for (var beat = 0; beat <= 1; beat++)
                {
                    scorer.Update(beat, true, 12);
                }

                return scorer;
            }

            Assert.That(Run(EGameDifficulty.TR_CONFIG_EASY).Score.Points, Is.GreaterThan(0));
            Assert.That(Run(EGameDifficulty.TR_CONFIG_NORMAL).Score.Points, Is.EqualTo(0));
        }

        [Test]
        public void PerfectRun_OnRealSampleSong_NearMaxScore()
        {
            var sample = Path.Combine(AppContext.BaseDirectory, "TestFiles", "sample.txt");
            var song = CSong.LoadSong(sample);
            Assert.That(song, Is.Not.Null);
            Assert.That(song!.LoadNotes(), Is.True);

            var score = ScorePerfect(song.Notes.GetVoice(0)).Score;

            Assert.That(score.Points, Is.GreaterThan(9900), "perfect run should approach MaxScore");
            Assert.That(score.Points, Is.LessThanOrEqualTo(CSettings.MaxScore + 0.01));
            Assert.That(score.PointsGoldenNotes, Is.GreaterThan(0), "the golden note should contribute");
            Assert.That(score.SungLines.Any(l => l.PerfectLine), Is.True, "at least the fully-sung line is perfect");
        }
    }
}
