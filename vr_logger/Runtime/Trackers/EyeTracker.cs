using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if USE_SRANIPAL
using ViveSR.anipal.Eye;
#endif

namespace VRLogger
{
    public class EyeTracker : MonoBehaviour
    {
        [Header("Config")]
        public Camera vrCamera;
        public float checkInterval = 0.04f; // ~25Hz
        private float timer = 0f;

        // Flags para controlar si tenemos el SDK
        private bool hasSRanipal = false;
        private string lastTarget = "";

        void Start()
        {
            if (vrCamera == null) vrCamera = Camera.main;
            Debug.Log("[EyeTracker] Inicializado. Asegúrate de tener 'Vive SRanipal' importado para datos reales.");
        }

        void Update()
        {
            if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsPaused) return;

            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                TrackEyes();
            }
        }

        private async void TrackEyes()
        {
#if USE_SRANIPAL
            try
            {
                // LÓGICA SRANIPAL REAL
                ViveSR.anipal.Eye.VerboseData eyeData;
                bool success = ViveSR.anipal.Eye.SRanipal_Eye.GetVerboseData(out eyeData);

                if (success)
                {
                    var combined = eyeData.combined.eye_data;
                    var left = eyeData.left;
                    var right = eyeData.right;

                    bool valid = combined.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);

                    // --- NUEVO: RAYCAST DEL EYE TRACKING ---
                    string targetName = "none";
                    Vector3? hitPos = null;

                    if (valid && vrCamera != null)
                    {
                        // Convertir la dirección local del ojo a dirección global basada en la rotación de la cámara (HMD)
                        // SRanipal usa coord de mano derecha, en Unity hay que invertir la X a veces, pero asumiendo que el SDK lo corrige:
                        Vector3 localGaze = new Vector3(combined.gaze_direction_normalized.x, combined.gaze_direction_normalized.y, combined.gaze_direction_normalized.z);
                        // Unity usa mano izquierda (X inverted by SDK usually). Si hay problemas, invertir X localmente.
                        // localGaze.x *= -1; // Descomentar si el eje X visual está invertido en las pruebas.

                        Vector3 worldGazeDir = vrCamera.transform.TransformDirection(localGaze);
                        Ray ray = new Ray(vrCamera.transform.position, worldGazeDir);

                        if (Physics.Raycast(ray, out RaycastHit hit))
                        {
                            targetName = hit.collider.gameObject.name;
                            hitPos = hit.point;
                        }
                    }

                    if (targetName != lastTarget && targetName != "none")
                    {
                        lastTarget = targetName;
                        await LoggerService.LogEvent("eye_tracking", "eye_target_change", null, new { target = targetName });
                    }

                    await LoggerService.LogEvent(
                        "eye_tracking",
                        "eye_frame",
                        null,
                        new
                        {
                            valid_combined = valid,
                            target = targetName,
                            hit_position = hitPos.HasValue ? new { x = hitPos.Value.x, y = hitPos.Value.y, z = hitPos.Value.z } : null,
                            gaze_direction_normalized = new { x = combined.gaze_direction_normalized.x, y = combined.gaze_direction_normalized.y, z = combined.gaze_direction_normalized.z },
                            gaze_origin_mm = new { x = combined.gaze_origin_mm.x, y = combined.gaze_origin_mm.y, z = combined.gaze_origin_mm.z },
                            pupil_diameter_left = left.pupil_diameter_mm,
                            pupil_diameter_right = right.pupil_diameter_mm,
                            openness_left = left.eye_openness,
                            openness_right = right.eye_openness
                        }
                    );
                }
            }
            catch (System.Exception)
            {
                // Ignorar error si no hay Vive SR
            }
#else
            Debug.LogWarning("[EyeTracker] ⚠️ Intentando usar EyeTracking pero NO tienes el SDK SRanipal. Ve a Edit > Project Settings > Player y añade 'USE_SRANIPAL' en Scripting Define Symbols.");
            this.enabled = false;
            await System.Threading.Tasks.Task.Yield(); // Just to suppress async warning
#endif
        }
    }
}
