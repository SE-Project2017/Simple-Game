using System;

using CnControls;

using UnityEngine;

namespace App
{
    public class InputController : MonoBehaviour
    {
        public event Action<Button> ButtonDown;
        public event Action<Button> ButtonUp;

        private AxisState mPrevHorizontalState = AxisState.Zero;
        private AxisState mPrevVerticalState = AxisState.Zero;

        public void Update()
        {
            foreach (Button button in new[]
                {Button.RotateRight, Button.RotateLeft, Button.Hold})
            {
                if (CnInputManager.GetButtonDown(button.ToString()))
                {
                    ButtonDown?.Invoke(button);
                }
                if (CnInputManager.GetButtonUp(button.ToString()))
                {
                    ButtonUp?.Invoke(button);
                }
            }

            var state = AxisToState(CnInputManager.GetAxis("Horizontal"));
            if (state != mPrevHorizontalState)
            {
                switch (mPrevHorizontalState)
                {
                    case AxisState.Zero:
                        break;
                    case AxisState.Positive:
                        ButtonUp?.Invoke(Button.Right);
                        break;
                    case AxisState.Negative:
                        ButtonUp?.Invoke(Button.Left);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                switch (state)
                {
                    case AxisState.Zero:
                        break;
                    case AxisState.Positive:
                        ButtonDown?.Invoke(Button.Right);
                        break;
                    case AxisState.Negative:
                        ButtonDown?.Invoke(Button.Left);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                mPrevHorizontalState = state;
            }
            state = AxisToState(CnInputManager.GetAxis("Vertical"));
            if (state != mPrevVerticalState)
            {
                switch (mPrevVerticalState)
                {
                    case AxisState.Zero:
                        break;
                    case AxisState.Positive:
                        ButtonUp?.Invoke(Button.Up);
                        break;
                    case AxisState.Negative:
                        ButtonUp?.Invoke(Button.Down);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                switch (state)
                {
                    case AxisState.Zero:
                        break;
                    case AxisState.Positive:
                        ButtonDown?.Invoke(Button.Up);
                        break;
                    case AxisState.Negative:
                        ButtonDown?.Invoke(Button.Down);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                mPrevVerticalState = state;
            }
        }

        private static AxisState AxisToState(float axis)
        {
            if (axis > 0.95)
            {
                return AxisState.Positive;
            }
            if (axis < -0.95)
            {
                return AxisState.Negative;
            }
            return AxisState.Zero;
        }

        public enum Button
        {
            RotateLeft,
            RotateRight,
            Left,
            Right,
            Hold,
            Up,
            Down,
        }

        private enum AxisState
        {
            Zero,
            Positive,
            Negative,
        }
    }
}
