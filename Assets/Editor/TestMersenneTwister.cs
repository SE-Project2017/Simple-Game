using System;

using Assets.Scripts;

using NUnit.Framework;

namespace Assets.Editor
{
    public class TestMersenneTwister
    {
        [Test]
        public void TestMersenneTwisterSimplePasses()
        {
            var mt = new ulong[312];
            mt[0] = 5489;
            const ulong f = 6364136223846793005;
            for (int i = 1; i < mt.Length; ++i)
            {
                mt[i] = f * (mt[i - 1] ^ (mt[i - 1] >> 62)) + (ulong) i;
            }
            MersenneTwister mersenneTwister = new MersenneTwister(mt);
            for (int i = 0; i < 9999; ++i)
            {
                mersenneTwister.Extract();
            }
            Assert.IsTrue(mersenneTwister.Extract() == 9981545732273789042);
            Random random = new Random();
            for (int i = 0; i < 10000; ++i)
            {
                int a = random.Next(-100, 0);
                int b = random.Next(0, 100);
                int r = mersenneTwister.UniformInt(a, b);
                Assert.IsTrue(a <= r && r <= b);
            }
        }
    }
}
