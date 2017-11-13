using System.Collections.Generic;

namespace Assets.Scripts.Utils
{
    public class RandomTetrominoGenerator
    {
        public RandomTetrominoGenerator() { }

        public RandomTetrominoGenerator(Seed seed) { }

        public IEnumerator<Tetromino> Generate() { }

        public struct Seed
        {
            public ulong[] Data;
        }
    }
}
