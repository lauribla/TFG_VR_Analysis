using UnityEngine;
using System.Threading.Tasks;

namespace VRLogger
{
    public class FootTracker : MonoBehaviour
    {
        public string footName = "left";
        public float checkInterval = 0.2f;

        private Vector3 lastPos;
        private float timer = 0f;

        public Transform targetTransform;

        void Update()
        {
            if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsPaused) return;

            if (targetTransform == null)
            {
                FindTarget();
                if (targetTransform == null) return; // Esperar hasta que aparezca el tracker
            }

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackFoot();
            }
        }

        private void FindTarget()
        {
            // Búsqueda dinámica para vive trackers (pies)
            string[] possibleNames = footName == "left" 
                ? new[] { "Left Foot Tracker", "Tracker (Left Foot)", "LeftFoot", "Vive Tracker Left" } 
                : new[] { "Right Foot Tracker", "Tracker (Right Foot)", "RightFoot", "Vive Tracker Right" };

            foreach (var n in possibleNames)
            {
                GameObject obj = GameObject.Find(n);
                if (obj != null)
                {
                    targetTransform = obj.transform;
                    Debug.Log($"[FootTracker] 🦶 Vinculado dinámicamente: {footName} a {obj.name}");
                    return;
                }
            }
        }

        private async void TrackFoot()
        {
            if (targetTransform == null) return;

            Vector3 pos = targetTransform.position;
            Vector3 velocity = (pos - lastPos) / checkInterval;

            await LoggerService.LogEvent(
                "tracker",
                "foot_movement",
                null,
                new { foot = footName, position = pos, velocity = velocity }
            );

            lastPos = pos;
        }
    }
}