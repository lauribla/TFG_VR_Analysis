using UnityEngine;
using UnityEngine.EventSystems;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Escucha interacciones pasivas como Punteros (PointerEnter/PointerExit) frecuentemente 
    /// utilizados para gaze (mirada) en VR o raycasts del mando apuntando sobre carteles de "Ayuda".
    /// Si el usuario mantiene el puntero sobre el objeto por un tiempo > tolerance (ej. 1s),
    /// cuando aparte la mirada se registrará que ha "usado/consumido" esa ayuda y cuánto tiempo la estuvo leyendo.
    /// </summary>
    public class AidInteractionLogger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Configuración de Ayuda/Pista")]
        [Tooltip("Identificador de la pista o texto de ayuda que el usuario est\u00e1 observando.")]
        public string aidId = "Cartel_Pista_01";

        [Tooltip("Segundos ininterrumpidos que el Gaze / Puntero debe estar encima del objeto para descontar 'roces autom\u00e1ticos' de la m\u00e9trica.")]
        public float recognitionTimeThreshold = 1.0f;

        private float hoverStartTime = 0f;
        private bool isHovering = false;

        private void Awake()
        {
            if (string.IsNullOrEmpty(aidId))
                aidId = gameObject.name;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            hoverStartTime = Time.time;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHovering)
            {
                float totalHoverTimeMs = (Time.time - hoverStartTime) * 1000f;
                
                // Si lo mir\u00f3 el tiempo suficiente como para considerarse que lo ley\u00f3 u observ\u00f3 adrede
                if (totalHoverTimeMs > (recognitionTimeThreshold * 1000f))
                {
                    LoggerService.LogEvent(
                        eventType: "metrics",
                        eventName: "AID_CONSUMED",
                        eventValue: new { 
                            aidId = this.aidId, 
                            timeObserved_ms = totalHoverTimeMs 
                        },
                        eventContext: null
                    );
                }
                
                isHovering = false;
            }
        }
        
        // Tambi\u00e9n agregamos triggers 3D f\u00edsicos para contextos sin UI Canvas (Gaze de Mando mediante Raycasts nativos)
        // Esto asume que el objeto que mira env\u00eda mensajes "OnTriggerEnter" o bien el Dev a\u00f1ade eventos manuales si fuese necesario
    }
}
