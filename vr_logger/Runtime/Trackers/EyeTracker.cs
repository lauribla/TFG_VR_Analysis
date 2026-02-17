using UnityEngine;
using System.Runtime.InteropServices;

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
            // LÓGICA SRANIPAL (Descomentar cuando se instale el SDK)
            /*
            if (Vive.SR.anipal.Eye.SRanipal_Eye_API.GetVerboseData(out Vive.SR.anipal.Eye.VerboseData eyeData))
            {
                var combined = eyeData.combined.eye_data;
                
                await LoggerService.LogEvent(
                    "eye_tracking",
                    "eye_frame",
                    null,
                    new
                    {
                        valid = combined.get_validity,
                        gaze_direction = combined.gaze_direction_normalized,
                        gaze_origin = combined.gaze_origin_mm,
                        pupil_diameter_left = eyeData.left.pupil_diameter_mm,
                        pupil_diameter_right = eyeData.right.pupil_diameter_mm,
                        openness_left = eyeData.left.eye_openness,
                        openness_right = eyeData.right.eye_openness
                    }
                );
            }
            */
        }
    }
}
