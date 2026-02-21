using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRLogger
{
    /// <summary>
    /// Simple wrapper around Unity Input to allow easier testing or future replacement.
    /// Supports both Legacy Input System and New Input System.
    /// </summary>
    public static class InputWrapper
    {
        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            Key inputSystemKey = MapKeyCode(key);
            if (inputSystemKey == Key.None) return false;
            return Keyboard.current[inputSystemKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool GetKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            Key inputSystemKey = MapKeyCode(key);
            if (inputSystemKey == Key.None) return false;
            return Keyboard.current[inputSystemKey].isPressed;
#else
            return Input.GetKey(key);
#endif
        }

        public static bool GetKeyUp(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            Key inputSystemKey = MapKeyCode(key);
            if (inputSystemKey == Key.None) return false;
            return Keyboard.current[inputSystemKey].wasReleasedThisFrame;
#else
            return Input.GetKeyUp(key);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Key MapKeyCode(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Return: return Key.Enter;
                default: return Key.None;
            }
        }
#endif
    }
}
