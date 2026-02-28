using UnityEngine;

namespace VRLogger.Trackers
{
    /// <summary>
    /// Componente Plug & Play para registrar choques físicos involuntarios.
    /// ALIMENTA LAS MÉTRICAS EN PYTHON: NavigationErrors
    /// USO: Arrástralo a las paredes o trampas "malas" que el jugador NO debe tocar. Asegúrate de tener un Collider (Trigger o Collision).
    /// </summary>
    [AddComponentMenu("VR Logger/Metrics/Obstacle Logger")]
    public class ObstacleLogger : MonoBehaviour
    {
        [Tooltip("Etiqueta o descripción de este obstáculo (ej. Pared_Laberinto). Si está vacío usa el nombre del objeto.")]
        public string obstacleId = "";

        [Tooltip("Verifica colisiones físicas normales (OnCollisionEnter)")]
        public bool logOnCollisionEnter = true;

        [Tooltip("Verifica entradas a zonas prohibidas (OnTriggerEnter)")]
        public bool logOnTriggerEnter = false;

        [Tooltip("Etiqueta (Tag) permitida para causar la colisión (ej. 'Player' u 'Hand'). Déjalo vacío para loggear TODO lo que choque.")]
        public string onlyCollideWithTag = "Player";

        private string GetObstacleId()
        {
            return string.IsNullOrEmpty(obstacleId) ? gameObject.name : obstacleId;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!logOnCollisionEnter) return;
            EvaluateHit(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!logOnTriggerEnter) return;
            EvaluateHit(other.gameObject);
        }

        private void EvaluateHit(GameObject hitTarget)
        {
            if (!string.IsNullOrEmpty(onlyCollideWithTag))
            {
                if (!hitTarget.CompareTag(onlyCollideWithTag))
                    return; // Ignorar colisión si no es el tag esperado
            }

            // Reportamos un "navigation_error" por intensidad 1
            LogAPI.LogCollision(GetObstacleId(), 1f);
            Debug.Log($"[ObstacleLogger] 🛑 Obstacle Hit ({GetObstacleId()}) by {hitTarget.name}");
        }
    }
}
