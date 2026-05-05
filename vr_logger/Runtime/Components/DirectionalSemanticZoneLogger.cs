using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Componente para intersecciones en laberintos. 
    /// Al entrar registra la decisión, y al SALIR calcula matemáticamente por qué cara local ha salido el jugador,
    /// logueando automáticamente un Success o Fail. Soporta cualquier forma si se configuran Balizas.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DirectionalSemanticZoneLogger : MonoBehaviour
    {
        [Header("Configuración de Intersección")]
        [Tooltip("ID de la intersección. Ej: Cruce_Principal")]
        public string zoneId = "Cruce_01";

        [Tooltip("Las capas que activan el trigger (ej: Player)")]
        public LayerMask validTriggerMask;

        [Tooltip("Si est activado, este cruce solo registrar la PRIMERA vez que el jugador pase por l. Muy til para no duplicar datos si el jugador se pierde y da vueltas.")]
        public bool logOnlyOnce = true;
        private bool _hasLogged = false;

        [System.Serializable]
        public struct CustomDirectionalExit
        {
            [Tooltip("Nombre de la cara o pasillo. Ej: Pasaje Secreto")]
            public string faceName;
            [Tooltip("Un GameObject vacío situado justo en el MODO DE SALIDA de este pasillo (baliza).")]
            public Transform beaconTransform;
            [Tooltip("El resultado que se registrará si el jugador sale por esta baliza.")]
            public SemanticZoneType resultType;
        }

        [Header("Configuración para Polígonos Complejos (Balizas)")]
        [Tooltip("OPCIONAL: Si usas un pentágono, añade aquí Balizas. Si lo dejas vacío, usará las 4 Caras del cubo por defecto.")]
        public System.Collections.Generic.List<CustomDirectionalExit> customExits = new System.Collections.Generic.List<CustomDirectionalExit>();

        [Header("Configuración de Caras (Solo para Cubos)")]
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
                Debug.LogWarning($"[DirectionalSemanticZoneLogger] ⚠️ El objeto {gameObject.name} necesita un Collider con isTrigger activado.");
                if (myCol != null) myCol.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (logOnlyOnce && _hasLogged) return;

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
            if (logOnlyOnce && _hasLogged) return;

            if (((1 << other.gameObject.layer) & validTriggerMask) != 0)
            {
                SemanticZoneType resultType = SemanticZoneType.Decision;
                string exitFace = "";

                // Si hemos configurado balizas, usamos distancia geométrica por baliza
                if (customExits != null && customExits.Count > 0)
                {
                    float minDistance = float.MaxValue;
                    Vector3 exitWorldPos = other.transform.position;

                    foreach (var exit in customExits)
                    {
                        if (exit.beaconTransform == null) continue;

                        float dist = Vector3.Distance(exitWorldPos, exit.beaconTransform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            resultType = exit.resultType;
                            exitFace = string.IsNullOrEmpty(exit.faceName) ? exit.beaconTransform.name : exit.faceName;
                        }
                    }

                    if (string.IsNullOrEmpty(exitFace)) exitFace = "Unknown_Custom";
                }
                else
                {
                    // Lógica por defecto para retrocompatibilidad con Cubos Ortogonales
                    Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

                    if (Mathf.Abs(localPos.z) > Mathf.Abs(localPos.x))
                    {
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

                _hasLogged = true;
            }
        }
    }
}
