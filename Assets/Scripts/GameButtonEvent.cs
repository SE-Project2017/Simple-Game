namespace Assets.Scripts
{
    public class GameButtonEvent
    {
        public EventType Type;
        public ButtonType Button;

        public enum EventType
        {
            ButtonDown,
            ButtonUp,
        }

        public enum ButtonType
        {
            Left,
            Right,
            Up,
            Down,
            RotateLeft,
            RotateRight,
            Hold,
        }
    }
}
