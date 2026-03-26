using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Componente para intersecciones en laberintos. 
    /// Al entrar registra la decisión, y al SALIR calcula matemáticamente por qué cara local del cubo ha salido el jugador,
    /// logueando automáticamente un Success o Fail en base a la configuración de sus caras.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DirectionalSemanticZoneLogger : MonoBehaviour
    {
        [Header("Configuración de Intersección")]
        [Tooltip("ID de la intersección. Ej: Cruce_Principal")]
        public string zoneId = "Cruce_01";
        
        [Tooltip("Las capas que activan el trigger (ej: Player)")]
        public LayerMask validTriggerMask;

        [Header("Configuración de Caras (Local)")]
        [Tooltip("Resultado si el jugador sale atravesando la cara DERECHA (+X) de este cubo.")]
        public SemanticZoneType exitRightX = SemanticZoneType.Fail;
        
        [Tooltip("Resultado si el jugador sale atravesando la cara IZQUIERDA (-X) de este cubo.")]
        public SemanticZoneType exitLeftX = SemanticZoneType.Fail;
        
        [Tooltip("Resultado si el jugador sale atravesando la cara FRONTAL (+Z) de este cubo.")]
        public SemanticZoneType exitForwardZ = SemanticZoneType.Success;
        
        [Tooltip("Resultado si el jugador sale atravesando la cara TRASERA (-Z) de este cubo.")]
        public SemanticZoneType exitBackwardZ = SemanticZoneType.Backtrack;

        private void Awake()
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol == null || !myCol.isTrigger)
            {
                Debug.LogWarning($"[DirectionalSemanticZoneLogger] ⚠️ El objeto {gameObject.name} necesita un BoxCollider con isTrigger activado.");
                if (myCol != null) myCol.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & validTriggerMask) != 0)
            {
                // Registramos que el jugador ha llegado al punto de cruce (punto neutro)
                LoggerService.LogEvent(
                    eventType: "metrics",
                    eventName: "zone_decision_point",
                    eventValue: new { zoneId = this.zoneId, status = "entered_intersection" },
                    eventContext: null
                );
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & validTriggerMask) != 0)
            {
                // Calcular la posición local del jugador respecto al centro del cubo
                Vector3 localPos = transform.InverseTransformPoint(other.transform.position);
                
                // Determinar qué eje es el dominante basándonos en si ha salido más lejos por X o por Z local
                SemanticZoneType resultType = SemanticZoneType.Decision;
                string exitFace = "";

                if (Mathf.Abs(localPos.z) > Mathf.Abs(localPos.x))
                {
                    // Salió predominantemente por el eje Z (Frontal o Trasero)
                    if (localPos.z > 0) 
                    { 
                        resultType = exitForwardZ; 
                        exitFace = "Forward (+Z)"; 
                    }
                    else 
                    { 
                        resultType = exitBackwardZ; 
                        exitFace = "Backward (-Z)"; 
                    }
                }
                else
                {
                    // Salió predominantemente por el eje X (Derecha o Izquierda)
                    if (localPos.x > 0) 
                    { 
                        resultType = exitRightX; 
                        exitFace = "Right (+X)"; 
                    }
                    else 
                    { 
                        resultType = exitLeftX; 
                        exitFace = "Left (-X)"; 
                    }
                }

                // Mapeo automático de resultados
                string eventNameToLog = "zone_decision_point";
                switch (resultType)
                {
                    case SemanticZoneType.Success: 
                        eventNameToLog = "action_success"; 
                        break;
                    case SemanticZoneType.Fail:
                    case SemanticZoneType.Backtrack: 
                        eventNameToLog = "action_fail"; 
                        break;
                }

                // Enviar Log del resultado final de la decisión
                LoggerService.LogEvent(
                    eventType: "metrics",
                    eventName: eventNameToLog,
                    eventValue: new { 
                        zoneId = this.zoneId, 
                        zoneType = resultType.ToString(),
                        exitFace = exitFace
                    },
                    eventContext: null
                );
            }
        }
    }
}
