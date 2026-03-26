using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Calcula tiempos de reacción biológicos o mecánicos cronometrando automáticamente
    /// la vida de un Game Object. Utiliza hooks de ciclo de vida nativo de Unity.
    /// Recomendado: Añadir a prefabs de "Targets", "Dianas", "Enemigos", "Coleccionables" 
    /// temporales que desaparecen al atraparlos o golpearlos.
    /// </summary>
    public class LifecycleReactionLogger : MonoBehaviour
    {
        [Header("Configuración de la Reacción")]
        [Tooltip("Id semántico del objetivo: Diana_Cubo_1, Powerup_Extra, etc.")]
        public string targetId = "Generic_Reaction_Target";

        [Tooltip("Registrar el tiempo medido entre la llamada base Awake (cuando existió) y la llamada natural OnDestroy(). Ideal si no usas Object Pool.")]
        public bool logOnDestroy = true;

        [Tooltip("Registrar la métrica de tiempo de reacción si tu sistema de Pooling de Unity desabilita el objeto sin destruirlo.")]
        public bool logOnDisable = false;

        private float spawnedStateTime = 0f;
        private bool hasLoggedDeath = false;

        private void Awake()
        {
            if (string.IsNullOrEmpty(targetId))
                targetId = gameObject.name;

            if (!logOnDestroy && !logOnDisable)
            {
                Debug.LogWarning($"[LifecycleReactionLogger] ⚠️ {gameObject.name} tiene 'logOnDestroy' y 'logOnDisable' en false. El tiempo de reacción nunca terminará de medirse.");
            }
        }

        private void OnEnable()
        {
            // Reset state
            hasLoggedDeath = false;
            spawnedStateTime = Time.time;

            LoggerService.LogEvent(
                eventType: "metrics_reactions",
                eventName: "task_start",
                eventValue: new { taskId = this.targetId, targetId = this.targetId },
                eventContext: null
            );
        }

        private void OnDisable()
        {
            if (logOnDisable && !hasLoggedDeath)
            {
                LogReactionDeath("Disabled");
            }
        }

        private void OnDestroy()
        {
            if (logOnDestroy && !hasLoggedDeath)
            {
                LogReactionDeath("Destroyed");
            }
        }

        private void LogReactionDeath(string reasonStr)
        {
            hasLoggedDeath = true;
            float reactionTimeMs = (Time.time - spawnedStateTime) * 1000f;

            string resultStr = reasonStr == "Destroyed" ? "success" : "abandoned";
            LoggerService.LogEvent(
                eventType: "metrics_reactions",
                eventName: "task_end",
                eventValue: resultStr, // "success" o "abandoned" puro
                eventContext: new { targetId = this.targetId, reactionTime_ms = reactionTimeMs, reason = reasonStr }
            );
        }
    }
}
