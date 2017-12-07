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

        public readonly int[] LevelGravity = new int[2000];
        public readonly int[] LevelEntryDelay = new int[2000];
        public readonly int[] LevelClearEntryDelay = new int[2000];
        public readonly int[] LevelDasDelay = new int[2000];
        public readonly int[] LevelLockDelay = new int[2000];
        public readonly int[] LevelClearDelay = new int[2000];
        public readonly int[] LevelAdvance = {0, 1, 2, 4, 6};

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
        }
    }
}
