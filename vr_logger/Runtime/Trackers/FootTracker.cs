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

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackFoot();
            }
        }

        private async void TrackFoot()
        {
            Vector3 pos = transform.position;
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