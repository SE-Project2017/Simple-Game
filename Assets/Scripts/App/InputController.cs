using System;

using CnControls;

using UnityEngine;

namespace App
{
    public class InputController : MonoBehaviour
    {
        public event Action<Button> ButtonDown;
        public event Action<Button> ButtonUp;

        public void Update()
        {
            foreach (Button button in Enum.GetValues(typeof(Button)))
            {
                if (CnInputManager.GetButtonDown(button.ToString()))
                {
                    if (ButtonDown != null)
                    {
                        ButtonDown.Invoke(button);
                    }
                }
                if (CnInputManager.GetButtonUp(button.ToString()))
                {
                    if (ButtonUp != null)
                    {
                        ButtonUp.Invoke(button);
                    }
                }
            }
        }

        public enum Button
        {
            Up,
            Down,
            Left,
            Right,
            RotateLeft,
            RotateRight,
            Hold,
        }
    }
}
