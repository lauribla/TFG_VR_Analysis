using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace VRLogger
{
    public class EyeTracker : MonoBehaviour
    {
        [Header("Config")]
        public float checkInterval = 0.04f; // ~25Hz
        private float timer = 0f;

        // Flags para controlar si tenemos el SDK
        private bool hasSRanipal = false;

        void Start()
        {
            // Intentar detectar si existe la clase del SDK mediante Reflection o try-catch simple
            // Para evitar errores de compilación si el usuario no tiene el SDK,
            // idealmente usaríamos #if USE_SRANIPAL, pero como es runtime...
            // Asumiremos que el usuario de Vive Pro SI tiene el SDK importado.

            // Comprobación segura (si el namespace no existe, esto no compilaría sin assembly definition).
            // Siendo un script suelto, dejaremos el código comentado/protegido o asumiremos que el usuario lo descomenta.

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
            // LÓGICA SRANIPAL REAL (Corregido namespace: ViveSR y clase: SRanipal_Eye)
            ViveSR.anipal.Eye.VerboseData eyeData;

            // GetVerboseData está en SRanipal_Eye (wrapper), no en API.
            bool success = ViveSR.anipal.Eye.SRanipal_Eye.GetVerboseData(out eyeData);

            if (success)
            {
                var combined = eyeData.combined.eye_data;
                var left = eyeData.left;
                var right = eyeData.right;

                // FIXED: Use GetValidity method instead of non-existent get_validity property
                bool valid = combined.GetValidity(ViveSR.anipal.Eye.SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);

                await LoggerService.LogEvent(
                    "eye_tracking",
                    "eye_frame",
                    null,
                    new
                    {
                        valid_combined = valid,
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
    }
}
