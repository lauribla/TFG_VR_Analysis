using UnityEngine;

namespace VRLogger
{
    /// <summary>
    /// Simple wrapper around Unity Input to allow easier testing or future replacement.
    /// Default implementation just calls Unity Input.
    /// </summary>
    public static class InputWrapper
    {
        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return Input.GetKeyUp(key);
        }
    }
}
