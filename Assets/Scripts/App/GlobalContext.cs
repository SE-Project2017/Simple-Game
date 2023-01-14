using UnityEngine;

using Utils;

namespace App
{
    public class GlobalContext : Singleton<GlobalContext>
    {
        public Sprite[] BlockItemSprites;
        public Sprite BlockDefaultSprite;

        public GameObject AlertDialogPrefab;
        public GameObject AlertDialogButtonPrefab;

        public GameObject GameResultDialogPrefab;

        public bool RelaxMode = false;

        public readonly int[] LevelGravity = new int[2000];
        public readonly int[] LevelEntryDelay = new int[2000];
        public readonly int[] LevelClearEntryDelay = new int[2000];
        public readonly int[] LevelDasDelay = new int[2000];
        public readonly int[] LevelLockDelay = new int[2000];
        public readonly int[] LevelClearDelay = new int[2000];
        public readonly int[] LevelAdvance = { 0, 1, 2, 4, 6 };

        public const int MaxInternalGrade = 31;
        public const int MaxCombo = 10;

        private readonly int[] mInternalGradePointDecayRates = new int[MaxInternalGrade + 1];
        private readonly int[,] mInternalGradePointAwards = new int[5, MaxInternalGrade + 1];
        private readonly int[,] mInternalGradePointComboMultiplier = new int[5, MaxCombo + 1];
        private readonly int[] mInternalGradeBoost = new int[MaxInternalGrade + 1];

        private readonly string[] mGradeText = new string[18];

        public GlobalContext()
        {
            LevelGravity.Fill(0, 30, 1024);
            LevelGravity.Fill(30, 35, 1536);
            LevelGravity.Fill(35, 40, 2048);
            LevelGravity.Fill(40, 50, 2560);
            LevelGravity.Fill(50, 60, 3072);
            LevelGravity.Fill(60, 70, 4096);
            LevelGravity.Fill(70, 80, 8192);
            LevelGravity.Fill(80, 90, 12288);
            LevelGravity.Fill(90, 100, 16384);
            LevelGravity.Fill(100, 120, 20480);
            LevelGravity.Fill(120, 140, 24576);
            LevelGravity.Fill(140, 160, 28672);
            LevelGravity.Fill(160, 170, 32768);
            LevelGravity.Fill(170, 200, 36864);
            LevelGravity.Fill(200, 220, 1024);
            LevelGravity.Fill(220, 230, 8192);
            LevelGravity.Fill(230, 233, 16384);
            LevelGravity.Fill(233, 236, 24576);
            LevelGravity.Fill(236, 239, 32768);
            LevelGravity.Fill(239, 243, 40960);
            LevelGravity.Fill(243, 247, 49152);
            LevelGravity.Fill(247, 251, 57344);
            LevelGravity.Fill(251, 300, 65536);
            LevelGravity.Fill(300, 330, 131072);
            LevelGravity.Fill(330, 360, 196608);
            LevelGravity.Fill(360, 400, 262144);
            LevelGravity.Fill(400, 420, 327680);
            LevelGravity.Fill(420, 450, 262144);
            LevelGravity.Fill(450, 500, 196608);
            LevelGravity.Fill(500, LevelGravity.Length, 1310720);

            LevelEntryDelay.Fill(0, 700, 25);
            LevelEntryDelay.Fill(700, 800, 16);
            LevelEntryDelay.Fill(800, 1000, 12);
            LevelEntryDelay.Fill(1000, 1100, 6);
            LevelEntryDelay.Fill(1100, 1200, 5);
            LevelEntryDelay.Fill(1200, LevelEntryDelay.Length, 4);

            LevelClearEntryDelay.Fill(0, 600, 25);
            LevelClearEntryDelay.Fill(600, 700, 16);
            LevelClearEntryDelay.Fill(700, 800, 12);
            LevelClearEntryDelay.Fill(800, 1100, 6);
            LevelClearEntryDelay.Fill(1100, 1200, 5);
            LevelClearEntryDelay.Fill(1200, LevelClearEntryDelay.Length, 4);

            LevelDasDelay.Fill(0, 500, 14);
            LevelDasDelay.Fill(500, 900, 8);
            LevelDasDelay.Fill(900, LevelDasDelay.Length, 6);

            LevelLockDelay.Fill(0, 900, 30);
            LevelLockDelay.Fill(900, 1100, 17);
            LevelLockDelay.Fill(1100, LevelLockDelay.Length, 15);

            LevelClearDelay.Fill(0, 500, 40);
            LevelClearDelay.Fill(500, 600, 25);
            LevelClearDelay.Fill(600, 700, 16);
            LevelClearDelay.Fill(700, 800, 12);
            LevelClearDelay.Fill(800, LevelClearDelay.Length, 6);

            mInternalGradePointDecayRates.Fill(0, 1, 125);
            mInternalGradePointDecayRates.Fill(1, 3, 80);
            mInternalGradePointDecayRates.Fill(3, 4, 50);
            mInternalGradePointDecayRates.Fill(4, 7, 45);
            mInternalGradePointDecayRates.Fill(7, 12, 40);
            mInternalGradePointDecayRates.Fill(12, 15, 30);
            mInternalGradePointDecayRates.Fill(15, 20, 20);
            mInternalGradePointDecayRates.Fill(20, 30, 15);
            mInternalGradePointDecayRates.Fill(30, mInternalGradePointDecayRates.Length, 10);

            mInternalGradePointAwards.Fill(1, 0, 5, 10, 1);
            mInternalGradePointAwards.Fill(1, 5, 10, 5, 1);
            mInternalGradePointAwards.Fill(1, 10, mInternalGradePointAwards.GetLength(1), 2, 1);
            mInternalGradePointAwards.Fill(2, 0, 3, 20, 1);
            mInternalGradePointAwards.Fill(2, 3, 6, 15, 1);
            mInternalGradePointAwards.Fill(2, 6, 10, 10, 1);
            mInternalGradePointAwards.Fill(2, 10, mInternalGradePointAwards.GetLength(1), 12, 1);
            mInternalGradePointAwards.Fill(3, 0, 1, 40, 1);
            mInternalGradePointAwards.Fill(3, 1, 4, 30, 1);
            mInternalGradePointAwards.Fill(3, 4, 7, 20, 1);
            mInternalGradePointAwards.Fill(3, 7, 10, 15, 1);
            mInternalGradePointAwards.Fill(3, 10, mInternalGradePointAwards.GetLength(1), 13, 1);
            mInternalGradePointAwards.Fill(4, 0, 1, 50, 1);
            mInternalGradePointAwards.Fill(4, 1, 5, 40, 1);
            mInternalGradePointAwards.Fill(4, 5, mInternalGradePointAwards.GetLength(1), 30, 1);

            mInternalGradePointComboMultiplier.Fill(1, 1,
                mInternalGradePointComboMultiplier.GetLength(1), 10, 1);
            mInternalGradePointComboMultiplier.Fill(2, 1, 2, 10, 1);
            mInternalGradePointComboMultiplier.Fill(2, 2, 4, 12, 1);
            mInternalGradePointComboMultiplier.Fill(2, 4, 8, 14, 1);
            mInternalGradePointComboMultiplier.Fill(2, 8, 10, 15, 1);
            mInternalGradePointComboMultiplier.Fill(2, 10,
                mInternalGradePointComboMultiplier.GetLength(1), 20, 1);
            mInternalGradePointComboMultiplier[3, 1] = 10;
            mInternalGradePointComboMultiplier[3, 2] = 14;
            mInternalGradePointComboMultiplier[3, 3] = 15;
            mInternalGradePointComboMultiplier[3, 4] = 16;
            mInternalGradePointComboMultiplier[3, 5] = 17;
            mInternalGradePointComboMultiplier[3, 6] = 18;
            mInternalGradePointComboMultiplier[3, 7] = 19;
            mInternalGradePointComboMultiplier[3, 8] = 20;
            mInternalGradePointComboMultiplier[3, 9] = 21;
            mInternalGradePointComboMultiplier[3, 10] = 25;
            mInternalGradePointComboMultiplier[4, 1] = 10;
            mInternalGradePointComboMultiplier[4, 2] = 15;
            mInternalGradePointComboMultiplier[4, 3] = 18;
            mInternalGradePointComboMultiplier[4, 4] = 20;
            mInternalGradePointComboMultiplier[4, 5] = 22;
            mInternalGradePointComboMultiplier[4, 6] = 23;
            mInternalGradePointComboMultiplier[4, 7] = 24;
            mInternalGradePointComboMultiplier[4, 8] = 25;
            mInternalGradePointComboMultiplier[4, 9] = 26;
            mInternalGradePointComboMultiplier[4, 10] = 30;

            mInternalGradeBoost[0] = 0;
            mInternalGradeBoost[1] = 1;
            mInternalGradeBoost[2] = 2;
            mInternalGradeBoost[3] = 3;
            mInternalGradeBoost[4] = 4;
            mInternalGradeBoost.Fill(5, 7, 5);
            mInternalGradeBoost.Fill(7, 9, 6);
            mInternalGradeBoost.Fill(9, 12, 7);
            mInternalGradeBoost.Fill(12, 15, 8);
            mInternalGradeBoost.Fill(15, 18, 9);
            mInternalGradeBoost.Fill(18, 19, 10);
            mInternalGradeBoost.Fill(19, 20, 11);
            mInternalGradeBoost.Fill(20, 23, 12);
            mInternalGradeBoost.Fill(23, 25, 13);
            mInternalGradeBoost.Fill(25, 27, 14);
            mInternalGradeBoost.Fill(27, 29, 15);
            mInternalGradeBoost.Fill(29, 31, 16);
            mInternalGradeBoost.Fill(31, mInternalGradeBoost.Length, 17);

            for (int i = 0; i < 9; ++i)
            {
                mGradeText[i] = (9 - i).ToString();
            }
            for (int i = 9; i < 18; ++i)
            {
                mGradeText[i] = "S" + (i - 8);
            }
        }

        public int InternalGradePointDecayRate(int internalGrade)
        {
            return mInternalGradePointDecayRates[internalGrade];
        }

        public int InternalGradePointAward(int linesCleared, int internalGrade, int combo,
            int level)
        {
            return (mInternalGradePointAwards[linesCleared, internalGrade] *
                    mInternalGradePointComboMultiplier[linesCleared, combo] + 9) / 10 *
                (level / 250 + 1);
        }

        public int InternalGradeBoost(int internalGrade)
        {
            return mInternalGradeBoost[internalGrade];
        }

        public string GradeText(int grade)
        {
            return mGradeText[grade];
        }
    }
}
