using UnityEngine;

namespace Utils
{
    public enum Tetromino
    {
        I,
        O,
        T,
        J,
        L,
        S,
        Z,
        Undefined = -1,
    }

    public static class TetrominoColor
    {
        private static readonly Color[] Colors = new Color[7];

        static TetrominoColor()
        {
            ColorUtility.TryParseHtmlString("#FF4000FF", out Colors[0]);
            ColorUtility.TryParseHtmlString("#FFFF00FF", out Colors[1]);
            ColorUtility.TryParseHtmlString("#00FAFFFF", out Colors[2]);
            ColorUtility.TryParseHtmlString("#0000FFFF", out Colors[3]);
            ColorUtility.TryParseHtmlString("#FFAE00FF", out Colors[4]);
            ColorUtility.TryParseHtmlString("#FF47FEFF", out Colors[5]);
            ColorUtility.TryParseHtmlString("#00DD00FF", out Colors[6]);
        }

        public static Color Color(this Tetromino tetromino)
        {
            return Colors[(int) tetromino];
        }
    }
}
