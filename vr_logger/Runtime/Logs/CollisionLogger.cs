using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Captura colisiones físicas de objetos del usuario (manos, controladores, etc.) y registra eventos en MongoDB.
/// </summary>
namespace VRLogger
{
    [RequireComponent(typeof(Collider))]
    public class CollisionLogger : MonoBehaviour
    {
        private async void OnCollisionEnter(Collision collision)
        {
            await LogCollision("collision_enter", collision);
        }

        private async void OnCollisionExit(Collision collision)
        {
            await LogCollision("collision_exit", collision);
        }

        /// <summary>
        /// Envía un log de colisión (inicio o fin) al LoggerService (MongoDB).
        /// </summary>
        private async Task LogCollision(string eventName, Collision collision)
        {
            var context = new
            {
                this_object = gameObject.name,
                other_object = collision.gameObject.name,
                relative_velocity = new
                {
                    x = collision.relativeVelocity.x,
                    y = collision.relativeVelocity.y,
                    z = collision.relativeVelocity.z
                },
                contact_points = collision.contacts.Length,
                first_contact = collision.contacts.Length > 0 ? new
                {
                    x = collision.contacts[0].point.x,
                    y = collision.contacts[0].point.y,
                    z = collision.contacts[0].point.z
                } : null,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };

            var log = new
            {
                event_type = "collision",
                event_name = eventName,
                event_value = 1,
                event_context = context
            };

            await LoggerService.SendLog(log);
        }
    }
}
