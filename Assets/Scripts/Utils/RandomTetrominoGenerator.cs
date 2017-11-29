using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Utils
{
    public class RandomTetrominoGenerator
    {
        private readonly MersenneTwister mRandomNumberGenerator;

        public RandomTetrominoGenerator()
        {
            var provider = new RNGCryptoServiceProvider();
            var bytes = new byte[2496];
            provider.GetBytes(bytes);
            var seed = new ulong[312];
            for (int i = 0; i < seed.Length; ++i)
            {
                seed[i] = BitConverter.ToUInt64(bytes, i * 8);
            }
            mRandomNumberGenerator = new MersenneTwister(seed);
        }

        public RandomTetrominoGenerator(ulong[] seed)
        {
            mRandomNumberGenerator = new MersenneTwister(seed);
        }

        public IEnumerator<Tetromino> Generate()
        {
            var bag = new Tetromino[35];
            for (int i = 0; i < 5; ++i)
            {
                bag[i * 7] = Tetromino.J;
                bag[i * 7 + 1] = Tetromino.I;
                bag[i * 7 + 2] = Tetromino.Z;
                bag[i * 7 + 3] = Tetromino.L;
                bag[i * 7 + 4] = Tetromino.O;
                bag[i * 7 + 5] = Tetromino.T;
                bag[i * 7 + 6] = Tetromino.S;
            }
            var history = new LinkedList<Tetromino>();
            history.AddLast(Tetromino.S);
            history.AddLast(Tetromino.Z);
            history.AddLast(Tetromino.S);
            history.AddLast(Tetromino.Z);
            var droughtOrder = new List<Tetromino>
            {
                Tetromino.J,
                Tetromino.I,
                Tetromino.Z,
                Tetromino.L,
                Tetromino.O,
                Tetromino.T,
                Tetromino.S,
            };
            var count = new Dictionary<Tetromino, int>
            {
                {Tetromino.J, 0},
                {Tetromino.I, 0},
                {Tetromino.Z, 0},
                {Tetromino.L, 0},
                {Tetromino.O, 0},
                {Tetromino.T, 0},
                {Tetromino.S, 0},
            };

            Tetromino[] firstTetrominos = {Tetromino.J, Tetromino.I, Tetromino.L, Tetromino.T};
            Tetromino first =
                firstTetrominos[mRandomNumberGenerator.Range(0, firstTetrominos.Length)];
            history.RemoveFirst();
            history.AddLast(first);
            yield return first;

            while (true)
            {
                Tetromino tetromino = Tetromino.I;
                int roll;
                int i = 0;
                for (roll = 0; roll < 6; ++roll)
                {
                    i = mRandomNumberGenerator.Range(0, 35);
                    tetromino = bag[i];
                    if (!history.Contains(tetromino))
                    {
                        break;
                    }
                    if (roll < 5)
                    {
                        bag[i] = droughtOrder[0];
                    }
                }
                count[tetromino] += 1;
                bool emulateBug = tetromino == droughtOrder[0] && roll > 0 &&
                    !count.Values.Contains(0);
                if (!emulateBug)
                {
                    bag[i] = droughtOrder[0];
                }
                droughtOrder.Remove(tetromino);
                droughtOrder.Add(tetromino);
                history.RemoveFirst();
                history.AddLast(tetromino);
                yield return tetromino;
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}
