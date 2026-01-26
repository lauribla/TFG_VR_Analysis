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

        void Update()
        {
            if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsPaused) return;

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackHand();
            }
        }

        private async void TrackHand()
        {
            Vector3 pos = transform.position;
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