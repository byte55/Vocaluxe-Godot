using System;
using System.Collections.Generic;
using System.Linq;
using VocaluxeAudio.Record;

namespace VocaluxeCore.Tests.Audio
{
    [TestFixture]
    public class MultiPlayerRoutingTest
    {
        private const double BaseToneFreq = 65.4064;
        private const double HalftoneBase = 1.05946309436;
        private const int SampleRate = 44100;

        /// <summary>Two-channel interleaved 16-bit PCM: left = toneL, right = toneR.</summary>
        private static byte[] GenInterleavedStereo(int toneL, int toneR, int frames)
        {
            var fL = BaseToneFreq * Math.Pow(HalftoneBase, toneL);
            var fR = BaseToneFreq * Math.Pow(HalftoneBase, toneR);
            var s = new short[frames * 2];
            for (var i = 0; i < frames; i++)
            {
                s[i * 2] = (short)(Math.Sin(2 * Math.PI * i / SampleRate * fL) * short.MaxValue);
                s[i * 2 + 1] = (short)(Math.Sin(2 * Math.PI * i / SampleRate * fR) * short.MaxValue);
            }

            var b = new byte[s.Length * 2];
            Buffer.BlockCopy(s, 0, b, 0, b.Length);
            return b;
        }

        private sealed class CTestRecord : CRecordBase
        {
            public void Feed(CRecordDevice device, byte[] data)
            {
                _HandleData(device, data);
            }
        }

        [Test]
        public void StereoDevice_RoutesEachChannelToItsPlayer()
        {
            const int toneP1 = 33; // A4 on channel 0 -> player 1
            const int toneP2 = 24; // C4 on channel 1 -> player 2

            var rec = new CTestRecord();
            try
            {
                Assert.That(rec.Init(), Is.True);
            }
            catch (DllNotFoundException)
            {
                Assert.Ignore("native libPitchTracker.so not built (run: make -C native/PitchTracker)");
                return;
            }

            // synthetic steady sines read ~0 on the AKF volume metric -> disable the silence gate
            rec.SetVolumeThreshold(0, 0f);
            rec.SetVolumeThreshold(1, 0f);

            var device = new CRecordDevice(0, "test-stereo", "test", 2);
            device.PlayerChannel[0] = 1; // channel 0 -> player 1
            device.PlayerChannel[1] = 2; // channel 1 -> player 2

            const int frames = 512;
            const int chunks = 32;
            const int chunkBytes = frames * 2 /*channels*/ * 2 /*bytes*/;
            var full = GenInterleavedStereo(toneP1, toneP2, frames * chunks);
            var chunk = new byte[chunkBytes];

            var p0 = new List<int>();
            var p1 = new List<int>();
            for (var c = 0; c < chunks; c++)
            {
                Buffer.BlockCopy(full, c * chunkBytes, chunk, 0, chunkBytes);
                rec.Feed(device, chunk);
                rec.AnalyzeBuffer(0);
                rec.AnalyzeBuffer(1);
                if (c < 4)
                {
                    continue; // warm-up while the 2048-sample windows fill
                }

                if (rec.ToneValid(0))
                {
                    p0.Add(rec.GetToneAbs(0));
                }

                if (rec.ToneValid(1))
                {
                    p1.Add(rec.GetToneAbs(1));
                }
            }

            rec.Close();

            Assert.That(p0, Is.Not.Empty, "player 1 detected no tone");
            Assert.That(p1, Is.Not.Empty, "player 2 detected no tone");

            int MostCommon(List<int> l) => l.GroupBy(n => n).OrderByDescending(g => g.Count()).First().Key;
            Assert.That(MostCommon(p0), Is.EqualTo(toneP1), "player 1 should hear channel 0's tone (A4)");
            Assert.That(MostCommon(p1), Is.EqualTo(toneP2), "player 2 should hear channel 1's tone (C4)");
        }
    }
}
