using UnityEngine;
using System.Threading.Tasks;

namespace VRLogger
{
    public class HandTracker : MonoBehaviour
    {
        public string handName = "left";
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
                if (targetTransform == null) return; // Esperar hasta que aparezca el mando
            }

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackHand();
            }
        }

        private void FindTarget()
        {
            // Búsqueda dinámica basada en nombre de la mano
            string[] possibleNames = handName == "left" 
                ? new[] { "Left Controller", "LeftHand Controller", "LeftHand", "Left Controller [XR]" } 
                : new[] { "Right Controller", "RightHand Controller", "RightHand", "Right Controller [XR]" };

            foreach (var n in possibleNames)
            {
                GameObject obj = GameObject.Find(n);
                if (obj != null)
                {
                    targetTransform = obj.transform;
                    Debug.Log($"[HandTracker] 🖐️ Vinculado dinámicamente: {handName} a {obj.name}");
                    return;
                }
            }
        }

        private async void TrackHand()
        {
            if (targetTransform == null) return;

            Vector3 pos = targetTransform.position;
            Vector3 velocity = (pos - lastPos) / checkInterval;

            await LoggerService.LogEvent(
                "tracker",
                "hand_movement",
                null,
                new { hand = handName, position = pos, velocity = velocity }
            );

            lastPos = pos;
        }
    }
}