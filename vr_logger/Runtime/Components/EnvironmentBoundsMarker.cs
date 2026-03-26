using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Marca un punto clave del entorno físico (ej: esquinas de la habitación).
    /// El script de Python usará todas las posiciones para dibujar el contorno real del play area en los gráficos espaciales,
    /// ideal para sustituir el cuadrado genérico generado por ancho y profundidad.
    /// Simplemente arrastra este script a GameObjects vacíos y colócalos en los límites de tu mundo.
    /// </summary>
    public class EnvironmentBoundsMarker : MonoBehaviour
    {
        private void Start()
        {
            // Pequeño retraso para asegurar que UserSessionManager y LoggerService están completamente conectados a Mongo
            Invoke(nameof(LogMarkerData), 2.5f);
        }

        private void LogMarkerData()
        {
            LoggerService.LogEvent(
                eventType: "system",
                eventName: "ENVIRONMENT_BOUNDARY_MARKER",
                eventValue: new { 
                    marker_x = transform.position.x,
                    marker_z = transform.position.z 
                },
                eventContext: null
            );
        }
    }
}
