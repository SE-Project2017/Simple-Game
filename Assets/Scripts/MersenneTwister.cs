using System;

using UnityEngine.Assertions;

namespace Assets.Scripts
{
    public class MersenneTwister
    {
        private const ulong N = 312;
        private const ulong M = 156;
        private const int R = 31;
        private const ulong A = 0xb5026f5aa96619e9;
        private const int U = 29;
        private const ulong D = 0x5555555555555555;
        private const int S = 17;
        private const ulong B = 0x71d67fffeda60000;
        private const int T = 37;
        private const ulong C = 0xfff7eee000000000;
        private const int L = 43;

        private const ulong LowerMask = (((ulong) 1) << R) - 1;
        private const ulong UpperMask = ~LowerMask;

        private readonly ulong[] mt = new ulong[N];
        private ulong index = N;

        public MersenneTwister(ulong[] seed)
        {
            Seed(seed);
        }

        public ulong Extract()
        {
            if (index >= N)
            {
                Twist();
            }
            ulong y = mt[index];
            y ^= (y >> U) & D;
            y ^= (y << S) & B;
            y ^= (y << T) & C;
            y ^= y >> L;
            ++index;
            return y;
        }

        public int UniformInt(int min, int max)
        {
            Assert.IsTrue(min <= max);
            ulong ret;
            ulong range = ((uint) max) - ((uint) min);
            ulong erange = range + 1;
            ulong scaling = ulong.MaxValue / erange;
            ulong past = erange * scaling;
            do
            {
                ret = Extract();
            } while (ret >= past);
            ret /= scaling;
            return (int) (ret + (ulong) min);
        }

        public int Range(int start, int stop)
        {
            Assert.IsTrue(start < stop);
            return UniformInt(start, stop - 1);
        }

        public void Seed(ulong[] seed)
        {
            Assert.IsTrue(seed.Length >= mt.Length);
            Array.Copy(seed, mt, mt.Length);
            index = N;
        }

        private void Twist()
        {
            for (ulong i = 0; i < N; ++i)
            {
                ulong x = (mt[i] & UpperMask) + (mt[(i + 1) % N] & LowerMask);
                ulong xa = x >> 1;
                if (x % 2 != 0)
                {
                    xa = xa ^ A;
                }
                mt[i] = mt[(i + M) % N] ^ xa;
            }
            index = 0;
        }
    }
}
