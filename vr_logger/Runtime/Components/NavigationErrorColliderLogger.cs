using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Transforma colisiones físicas indeseadas del jugador (ej: cabeza atravesando muros,
    /// manos chocando con elementos rojos/prohibidos) en penalizaciones de navegación
    /// de cara a las métricas, controladas por un cooldown para evitar spam de FixedUpdate.
    /// </summary>
    public class NavigationErrorColliderLogger : MonoBehaviour
    {
        [Header("Configuración del Error de Navegación")]
        [Tooltip("Identificador lógico de la infracción: \"Muro_Atravesado\", \"Mesa_Prohibida\", etc.")]
        public string penaltyId;

        [Tooltip("Máscaras de colisiones que triggeran este error. Normalmente será la capa en la que estén los obstáculos de este tipo.")]
        public LayerMask allowedErrorMask;

        [Tooltip("Tiempo mínimo (en segundos) que debe pasar antes de registrar OTRO choque con este obstáculo para evitar falsos positivos por la física base.")]
        public float cooldown_msInput = 500f;

        [Tooltip("Gravedad u honor al error asociado a la métrica, opcional.")]
        public float errorGravity = 1f;

        private float lastPenalizationTime = -999f;

        private void Start()
        {
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[NavigationErrorColliderLogger] ⚠️ Falta un Collider en {gameObject.name}. Este componente usa colisiones físicas (OnCollision / OnTrigger) para detectar errores de navegación.");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessImpact(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessImpact(other.gameObject);
        }

        private void ProcessImpact(GameObject hitObject)
        {
            // Solo logeamos si la layer coincide y el cooldown lo permite
            if (((1 << hitObject.layer) & allowedErrorMask) != 0)
            {
                if ((Time.time - lastPenalizationTime) * 1000f > cooldown_msInput)
                {
                    lastPenalizationTime = Time.time;
                    LoggerService.LogEvent(
                        eventType: "metrics_errors",
                        eventName: "navigation_error",
                        eventValue: new { 
                            penaltyId = string.IsNullOrEmpty(this.penaltyId) ? hitObject.name : this.penaltyId, 
                            colliderType = "Physical_Impact",
                            gravity = this.errorGravity
                        },
                        eventContext: null
                    );
                }
            }
        }
    }
}
