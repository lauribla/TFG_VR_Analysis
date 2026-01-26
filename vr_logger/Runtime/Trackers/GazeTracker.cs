using UnityEngine;
using System.Threading.Tasks;

namespace VRLogger
{
    public class GazeTracker : MonoBehaviour
    {
        [Header("Config")]
        public Camera vrCamera;
        public float checkInterval = 0.1f; // cada 100ms
        private float timer = 0f;

        private string lastTarget = "";

        void Update()
        {
            // PAUSE CHECK
            if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsPaused) return;

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackGaze();
            }
        }

        private async void TrackGaze()
        {
            if (vrCamera == null) return;

            Vector3 camPos = vrCamera.transform.position;
            Vector3 camForward = vrCamera.transform.forward;

            // ----------- Raycast al frente de la cámara -----------
            Ray ray = new Ray(camPos, camForward);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string targetName = hit.collider.gameObject.name;

                // 1) Evento de cambio de objetivo de mirada
                if (targetName != lastTarget)
                {
                    lastTarget = targetName;

                    await LoggerService.LogEvent(
                        "gaze",
                        "gaze_target_change",
                        null,
                        new
                        {
                            target = targetName,
                            hit_position = hit.point,
                            camera_position = camPos,
                            camera_forward = camForward
                        }
                    );
                }

                // 2) Evento de trayectoria de mirada (muestreo periódico)
                await LoggerService.LogEvent(
                    "gaze",
                    "gaze_frame",
                    null,
                    new
                    {
                        target = targetName,
                        hit_position = hit.point,
                        camera_position = camPos,
                        camera_forward = camForward
                    }
                );
            }
            else
            {
                // 3) Cuando no hay objetivo (mirando al vacío)
                lastTarget = "none";

                await LoggerService.LogEvent(
                    "gaze",
                    "gaze_frame",
                    null,
                    new
                    {
                        target = "none",
                        camera_position = camPos,
                        camera_forward = camForward
                    }
                );
            }
        }
    }
}
