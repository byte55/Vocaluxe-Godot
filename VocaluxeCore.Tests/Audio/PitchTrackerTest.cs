using System;
using System.Collections.Generic;
using System.Linq;
using VocaluxeAudio.Pitch;

namespace VocaluxeCore.Tests.Audio
{
    [TestFixture]
    public class PitchTrackerTest
    {
        // Same reference as the native tracker: tone 0 = C2 = 65.4064 Hz, one half tone = 2^(1/12).
        private const double BaseToneFreq = 65.4064;
        private const double HalftoneBase = 1.05946309436;
        private const int SampleRate = 44100;

        private static byte[] GenerateSine(int tone, int sampleCount)
        {
            var freq = BaseToneFreq * Math.Pow(HalftoneBase, tone);
            var samples = new short[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = (short)(Math.Sin(2 * Math.PI * i / SampleRate * freq) * short.MaxValue);
            }

            var bytes = new byte[sampleCount * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static CPtAKF TryCreate()
        {
            try
            {
                return new CPtAKF();
            }
            catch (DllNotFoundException)
            {
                Assert.Ignore("native libPitchTracker.so not built (run: make -C native/PitchTracker)");
                return null; // unreachable
            }
        }

        [Test]
        public void GetNumHalfTones_Is57()
        {
            using var akf = TryCreate();
            Assert.That(akf!.GetNumHalfTones(), Is.EqualTo(57));
        }

        [Test]
        public void DetectsCleanSine_A4_AsTone33()
        {
            const int tone = 33; // A4 = 440 Hz
            using var akf = TryCreate();

            // The AKF reports maxVolume ~0 for a perfectly periodic synthetic sine (its volume metric
            // is not raw amplitude), so disable the silence gate to test pure detection deterministically.
            akf!.VolumeTreshold = 0f;
            var weights = new float[akf.GetNumHalfTones()];
            var data = GenerateSine(tone, 16384);

            // sanity: the generated PCM is full-scale, not silent
            var asShorts = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, asShorts, 0, data.Length);
            var dataPeak = asShorts.Max(s => Math.Abs((int)s));
            Assert.That(dataPeak, Is.GreaterThan(30000));

            const int batchSamples = 512;
            const int batchBytes = batchSamples * 2;
            var batch = new byte[batchBytes];

            var detections = new List<int>();
            var allNotes = new List<int>();
            float maxVolSeen = 0;
            for (var offset = 0; offset + batchBytes <= data.Length; offset += batchBytes)
            {
                Buffer.BlockCopy(data, offset, batch, 0, batchBytes);
                akf.Input(batch);
                var note = akf.GetNote(out var vol, weights);
                allNotes.Add(note);
                if (vol > maxVolSeen)
                {
                    maxVolSeen = vol;
                }

                // Skip the warm-up while the analyzer's 2048-sample window fills.
                if (offset >= 2048 * 2 && note >= 0)
                {
                    detections.Add(note);
                }
            }

            var diag = $"dataPeak={dataPeak} maxVol={maxVolSeen:F4} notes=[{string.Join(",", allNotes)}]";
            Assert.That(detections, Is.Not.Empty, "no valid pitch detected; " + diag);
            var mostCommon = detections.GroupBy(n => n).OrderByDescending(g => g.Count()).First().Key;
            Assert.That(mostCommon, Is.EqualTo(tone), "clean A4 sine should be detected as tone 33 (A4); " + diag);
            // detection should be stable, not an occasional lucky frame
            Assert.That(detections.Count(n => n == tone), Is.GreaterThanOrEqualTo(detections.Count * 3 / 4),
                "detection of tone 33 should be stable; " + diag);
        }
    }
}
