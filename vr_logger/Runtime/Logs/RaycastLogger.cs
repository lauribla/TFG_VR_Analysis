using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Captura eventos de raycast (mirada o puntero) y los registra en MongoDB.
/// </summary>
namespace VRLogger
{
    public class RaycastLogger : MonoBehaviour
    {
        [Header("Raycast Settings")]
        public float maxDistance = 50f;
        public LayerMask targetLayers;

        void Update()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxDistance, targetLayers))
            {
                _ = LogHit(hit);
            }
        }

        /// <summary>
        /// Envía un log de impacto de raycast al LoggerService (MongoDB).
        /// </summary>
        private async Task LogHit(RaycastHit hit)
        {
            var context = new
            {
                object_name = hit.collider.name,
                distance = hit.distance,
                hit_point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                ray_origin = new { x = transform.position.x, y = transform.position.y, z = transform.position.z },
                ray_direction = new { x = transform.forward.x, y = transform.forward.y, z = transform.forward.z },
                timestamp = System.DateTime.UtcNow.ToString("o")
            };

            var log = new
            {
                event_type = "raycast",
                event_name = "raycast_hit",
                event_value = 1,
                event_context = context
            };

            await LoggerService.SendLog(log);
        }
    }
}
