using NUnit.Framework;
using VocaluxeCore;

namespace VocaluxeCore.Tests
{
    public class SmokeTest
    {
        [Test]
        public void CoreIsReferenced()
        {
            Assert.That(Smoke.Hello(), Is.EqualTo("VocaluxeCore alive"));
        }
    }
}
