using UnityEngine;

namespace VRLogger
{
    /// <summary>
    /// Interface for camera control abstraction.
    /// Projects can implement this on their camera controller to integrate with VR Logger.
    /// </summary>
    public interface ICameraController
    {
        void EnableControl();
        void DisableControl();
    }
}
