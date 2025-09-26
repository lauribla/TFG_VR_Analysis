using UnityEngine;
using System.Threading.Tasks;

namespace VRLogger
{
    public class MovementTracker : MonoBehaviour
    {
        [Header("Config")]
        public Transform player;        // El objeto que representa al usuario (ej. XR Rig)
        public float checkInterval = 0.2f; // Cada 200 ms
        public float sharpTurnThreshold = 45f; // Ángulo mínimo para considerar "giro brusco"

        private float timer = 0f;
        private Vector3 lastPosition;
        private Vector3 lastForward;
        private bool initialized = false;

        void Update()
        {
            if (player == null) return;

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackMovement();
            }
        }

        private async void TrackMovement()
        {
            Vector3 currentPos = player.position;
            Vector3 displacement = currentPos - lastPosition;
            float speed = displacement.magnitude / checkInterval;

            Vector3 currentForward = player.forward;

            // Evento de trayectoria periódica
            await LoggerService.LogEvent(
                "navigation",
                "movement_frame",
                null,
                new
                {
                    position = currentPos,
                    velocity = speed,
                    dir_forward = currentForward
                }
            );

            // Detectar giro brusco comparando vectores forward
            if (initialized)
            {
                float angle = Vector3.Angle(lastForward, currentForward);
                if (angle >= sharpTurnThreshold)
                {
                    await LoggerService.LogEvent(
                        "navigation",
                        "sharp_turn",
                        null,
                        new
                        {
                            angle_deg = angle,
                            position = currentPos,
                            time_ms = checkInterval * 1000f
                        }
                    );
                }
            }

            lastPosition = currentPos;
            lastForward = currentForward;
            initialized = true;
        }
    }
}
