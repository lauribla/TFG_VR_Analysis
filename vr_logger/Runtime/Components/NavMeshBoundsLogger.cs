using UnityEngine;
using UnityEngine.AI;
using VRLogger;
using System.Collections.Generic;

namespace VRLogger.Components
{
    /// <summary>
    /// Escanea automáticamente el NavMesh (área caminable) de Unity y extrae el contorno exterior.
    /// Envía un único log a MongoDB con estos límites para que Python pueda dibujar
    /// un marco azul espacial realista en los mapas de calor y trayectorias, en lugar
    /// de usar manualmente los EnvironmentBoundsMarkers.
    /// </summary>
    public class NavMeshBoundsLogger : MonoBehaviour
    {
        [Header("Configuración de NavMesh")]
        [Tooltip("Si hay cientos de vértices, esto agrupará aquellos que estén a menos de 10cm entre ellos para evitar subir JSONs enormes a Mongo, respetando la cuota.")]
        public bool reduccionInteligenteDePeso = true;

        private void Start()
        {
            // Pequeño retraso para asegurar que UserSessionManager y LoggerService están completamente listos y conectados
            Invoke(nameof(ExtractAndLogNavMesh), 3.0f);
        }

        private void ExtractAndLogNavMesh()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            
            if (triangulation.vertices.Length == 0)
            {
                Debug.LogWarning("[NavMeshBoundsLogger] ⚠️ No se encontró ningún NavMesh horneado en la escena. Ve a Window > AI > Navigation y haz Bake del suelo.");
                return;
            }

            List<float> xCoords = new List<float>();
            List<float> zCoords = new List<float>();

            // Usar un HashSet permite descartar rápidamente vértices repetidos o superpuestos.
            HashSet<string> uniquePoints = new HashSet<string>();

            for (int i = 0; i < triangulation.vertices.Length; i++)
            {
                Vector3 v = triangulation.vertices[i];
                
                // Redondeamos a 1 decimal (10 cm de precisión) para agrupar vértices cercanos y rebajar el coste de subida y guardado.
                float px = BaseModel(v.x);
                float pz = BaseModel(v.z);

                string id = px.ToString("F1") + "_" + pz.ToString("F1");
                if (!uniquePoints.Contains(id))
                {
                    uniquePoints.Add(id);
                    xCoords.Add(px);
                    zCoords.Add(pz);
                }
            }

            LoggerService.LogEvent(
                eventType: "system",
                eventName: "NAVMESH_BOUNDARY",
                eventValue: new { 
                    vertices_x = xCoords.ToArray(),
                    vertices_z = zCoords.ToArray(),
                    vertex_count = xCoords.Count,
                    is_decimated = reduccionInteligenteDePeso
                },
                eventContext: null
            );

            Debug.Log($"[NavMeshBoundsLogger] 🗺️ Extraídos {xCoords.Count} puntos únicos del NavMesh y enviados a Mongo.");
        }

        private float BaseModel(float f)
        {
            if (reduccionInteligenteDePeso)
                return Mathf.Round(f * 10f) / 10f;
            return f;
        }
    }
}
