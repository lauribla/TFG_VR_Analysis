using UnityEngine;
using VRLogger;
using System.Threading.Tasks;

namespace VRLogger.Components
{
    /// <summary>
    /// Registra intentos de tareas basadas en zonas físicas.
    /// Inicia el log al entrar en la zona (TriggerEnter) y lo finaliza
    /// si el jugador alcanza la zona de éxito o si abandona la zona sin lograrlo.
    /// Completamente automático, sólo requiere configurar colliders en modo trigger.
    /// Funciona bien para: Puzzles espaciales, zonas de escape, tareas de navegación.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TaskZoneBoundaryLogger : MonoBehaviour
    {
        [Header("Configuración de la Tarea")]
        [Tooltip("Identificador único para esta prueba/tarea espacial.")]
        public string taskId = "Espacial_01";
        
        [Tooltip("Capas que pueden activar esta zona de tarea (ej: Player).")]
        public LayerMask validTriggerMask;

        [Header("Condiciones de Éxito/Fallo")]
        [Tooltip("Collider o zona que marca el final exitoso de la tarea. Si es nulo, esta clase asume que el éxito se marca desde otra parte.")]
        public Collider successExitZone;
        
        [Tooltip("Si el jugador sale de ESTE collider original sin haber tocado la successExitZone, se asume que falló o abandonó.")]
        public bool failOnExit = true;

        // Estado interno
        private bool isTaskActive = false;
        private float taskStartTime = 0f;
        private Collider expectedPlayerCollider = null;

        private void Awake()
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol != null && !myCol.isTrigger)
            {
                Debug.LogWarning($"[TaskZoneBoundaryLogger] El collider de {gameObject.name} debería ser isTrigger para no bloquear físicamente al jugador.");
                myCol.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTaskActive) return;

            // Verificar si el que entra coincide con la máscara de colisión
            if (((1 << other.gameObject.layer) & validTriggerMask) != 0)
            {
                isTaskActive = true;
                taskStartTime = Time.time;
                expectedPlayerCollider = other;

                LoggerService.LogEvent(
                    eventType: "metrics",
                    eventName: "TASK_ATTEMPT_START",
                    eventValue: new { taskId = this.taskId, triggerEntity = other.name },
                    eventContext: null
                );
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isTaskActive || other != expectedPlayerCollider) return;

            // El jugador salió voluntariamente de la zona de intento inicial.
            if (failOnExit)
            {
                EndTask("Aborted/Fail");
            }
        }

        private void Update()
        {
            // Vigilar pasivamente si el jugador activo ha tocado la zona de éxito
            if (isTaskActive && expectedPlayerCollider != null && successExitZone != null)
            {
                // Un chequeo de contención de bounds sirve bien para wrappers automáticos sin requerir scripts en la meta
                if (successExitZone.bounds.Intersects(expectedPlayerCollider.bounds))
                {
                    EndTask("Success");
                }
            }
        }

        private void EndTask(string result)
        {
            isTaskActive = false;
            float durationMs = (Time.time - taskStartTime) * 1000f;

            LoggerService.LogEvent(
                eventType: "metrics",
                eventName: "TASK_ATTEMPT_END",
                eventValue: new { 
                    taskId = this.taskId, 
                    result = result, 
                    duration_ms = durationMs 
                },
                eventContext: null
            );

            // Reseteamos por si quiere volver a intentarlo reentrando
            expectedPlayerCollider = null; 
        }

        /// <summary>
        /// Hook manual por si otra lógica de juego necesita marcar el final exitoso
        /// sin depender de un choque de colisiones.
        /// </summary>
        public void ForceSuccess()
        {
            if (isTaskActive) EndTask("Success");
        }
    }
}
