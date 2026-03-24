using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Genera hitos de progreso unidireccionales de forma automática. 
    /// Solo requiere colgarlo de un box collider isTrigger invisible en un pasillo/puerta/zona.
    /// Una vez el objetivo entra y es consumido, se deshabilita garantizando limpieza en los logs
    /// si el jugador retrocede a zonas anteriores del nivel.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointProgressionLogger : MonoBehaviour
    {
        [Header("Configuración del Checkpoint")]
        [Tooltip("Nombre semántico del hito. Ej: Nivel1_Sect03_Completado")]
        public string checkpointName;

        [Tooltip("Orden lógico o porcentaje representativo (ej: 3, o 45%)")]
        public float progressValue;

        [Tooltip("Máscaras que pueden activar este progreso (típicamente Layer del Player/Cámara)")]
        public LayerMask playerMask;

        [Tooltip("Eliminar/Apagar el script inmediatamente después de que alguien cruce el checkpoint para que sea unidireccional y de 'un solo uso' por intento.")]
        public bool consumeOnTrigger = true;

        // Compartido de forma estática pura por simplicidad de correlación espacio-temporal
        private static float _lastGlobalCheckpointTime = 0f;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[CheckpointProgressionLogger] El collider en {gameObject.name} se cambió a Trigger automáticamente.");
            }

            if (string.IsNullOrEmpty(checkpointName))
                checkpointName = gameObject.name;
        }

        private void Start()
        {
             // Para inicializar de forma segura si este fue el spawn inicial
            if (_lastGlobalCheckpointTime == 0f) _lastGlobalCheckpointTime = Time.time;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & playerMask) != 0)
            {
                float timeSinceLastCheckpointMs = (Time.time - _lastGlobalCheckpointTime) * 1000f;
                _lastGlobalCheckpointTime = Time.time;

                LoggerService.LogEvent(
                    eventType: "metrics_progression",
                    eventName: "goal_reached",
                    eventValue: new { 
                        checkpointName = this.checkpointName, 
                        progressPercent = this.progressValue,
                        timeSinceLast_ms = timeSinceLastCheckpointMs
                    },
                    eventContext: null
                );

                if (consumeOnTrigger)
                {
                    this.enabled = false;
                }
            }
        }
    }
}
