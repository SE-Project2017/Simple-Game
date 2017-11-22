using UnityEngine;

namespace Assets.Scripts.Utils
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
        private static readonly Color[] sColors = new Color[7];

        static TetrominoColor()
        {
            ColorUtility.TryParseHtmlString("#FF4000FF", out sColors[0]);
            ColorUtility.TryParseHtmlString("#FFFF00FF", out sColors[1]);
            ColorUtility.TryParseHtmlString("#00FAFFFF", out sColors[2]);
            ColorUtility.TryParseHtmlString("#0000FFFF", out sColors[3]);
            ColorUtility.TryParseHtmlString("#FFAE00FF", out sColors[4]);
            ColorUtility.TryParseHtmlString("#FF47FEFF", out sColors[5]);
            ColorUtility.TryParseHtmlString("#00DD00FF", out sColors[6]);
        }

        public static Color Color(this Tetromino tetromino)
        {
            return sColors[(int) tetromino];
        }
    }
}
