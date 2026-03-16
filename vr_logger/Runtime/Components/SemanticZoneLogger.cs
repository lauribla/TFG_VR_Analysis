using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    public enum SemanticZoneType
    {
        Decision,
        Success,
        Fail,
        Backtrack
    }

    /// <summary>
    /// Componente invisible para añadir zonas semánticas a un entorno (ej: laberintos, puzzles).
    /// Emite los eventos estándar de success/fail de VRLogger o eventos de zona personalizados,
    /// ideal para análisis espacial simplificando el esfuerzo del desarrollador.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SemanticZoneLogger : MonoBehaviour
    {
        [Header("Configuración de la Zona Semántica")]
        [Tooltip("Tipo de zona que representa este trigger.")]
        public SemanticZoneType zoneType = SemanticZoneType.Decision;

        [Tooltip("Identificador único para esta zona concreta (ej: Laberinto_Bifurcacion_01).")]
        public string zoneId = "Zona_01";

        [Tooltip("Capas que pueden activar esta zona (ej: Player).")]
        public LayerMask validTriggerMask;

        [Tooltip("Al activarse, el componente dejará de registrar más entradas, útil para evitar duplicados si el jugador se queda rondando el borde.")]
        public bool logOnlyOnce = true;
        
        private bool hasLogged = false;

        private void Awake()
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol != null && !myCol.isTrigger)
            {
                Debug.LogWarning($"[SemanticZoneLogger] El collider de {gameObject.name} debería ser isTrigger para no bloquear físicamente al jugador.");
                myCol.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (logOnlyOnce && hasLogged) return;

            // Verificar si el que entra coincide con la máscara de colisión
            if (((1 << other.gameObject.layer) & validTriggerMask) != 0)
            {
                hasLogged = true;

                string eventNameToLog = "zone_decision_point";
                
                // Mapear el success y fail a los eventos por defecto de Python (metrics.py / log_parser)
                switch (zoneType)
                {
                    case SemanticZoneType.Success:
                        eventNameToLog = "action_success";
                        break;
                    case SemanticZoneType.Fail:
                    case SemanticZoneType.Backtrack:
                        eventNameToLog = "action_fail";
                        break;
                    case SemanticZoneType.Decision:
                        eventNameToLog = "zone_decision_point";
                        break;
                }

                LoggerService.LogEvent(
                    eventType: "metrics",
                    eventName: eventNameToLog,
                    eventValue: new { 
                        zoneId = this.zoneId, 
                        zoneType = zoneType.ToString(),
                        triggerEntity = other.name 
                    },
                    eventContext: null
                );
            }
        }
    }
}
