using UnityEngine;

/// <summary>
/// Añade colisión física a la línea de camino (LineRenderer) del modo PathLine.
/// Genera un CapsuleCollider por cada segmento del LineRenderer para que los
/// raycasts de GazeLogger y EyeTrackingLogger detecten la línea como "PathLine".
///
/// USO:
///   1. Añade este componente al mismo GameObject que tiene el LineRenderer
///      (el mismo objeto que tiene FloorPathRenderer, o el que lleva el LineRenderer).
///   2. Asegúrate de que el tag "PathLine" existe en Edit > Project Settings > Tags.
///   3. El script espera un frame antes de leer las posiciones del LineRenderer,
///      para garantizar que FloorPathRenderer ya las ha escrito.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathLineCollider : MonoBehaviour
{
    [Tooltip("Radio de los CapsuleColliders (debe coincidir visualmente con el ancho de la línea).")]
    public float colliderRadius = 0.05f;

    [Tooltip("Tag que se asignará a cada segmento colisionador para que los scripts de gaze lo reconozcan.")]
    public string segmentTag = "PathLine";

    [Tooltip("Layer de los segmentos (opcional; -1 = sin cambio, hereda el del GameObject padre).")]
    public int segmentLayer = -1;

    private GameObject _collidersRoot;

    void Start()
    {
        // Esperamos un frame para que FloorPathRenderer (u otro script) haya
        // colocado ya todas las posiciones en el LineRenderer.
        StartCoroutine(BuildCollidersNextFrame());
    }

    System.Collections.IEnumerator BuildCollidersNextFrame()
    {
        yield return null; // espera 1 frame

        var lr = GetComponent<LineRenderer>();
        int count = lr.positionCount;

        if (count < 2)
        {
            Debug.LogWarning("PathLineCollider: LineRenderer tiene menos de 2 puntos — no se generaron colliders.");
            yield break;
        }

        Vector3[] positions = new Vector3[count];
        lr.GetPositions(positions);

        // Contenedor hijo para mantener la jerarquía limpia
        _collidersRoot = new GameObject("PathLine_Colliders");
        _collidersRoot.transform.SetParent(transform, false);

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 a = positions[i];
            Vector3 b = positions[i + 1];

            float length = Vector3.Distance(a, b);
            if (length < 0.001f) continue;

            GameObject seg = new GameObject(!string.IsNullOrEmpty(segmentTag) ? segmentTag : "PathLine");
            seg.transform.SetParent(_collidersRoot.transform, true);

            // Asignar tag (el tag debe existir en el proyecto)
            if (!string.IsNullOrEmpty(segmentTag))
            {
                try { seg.tag = segmentTag; }
                catch { Debug.LogWarning($"PathLineCollider: El tag '{segmentTag}' no existe. Créalo en Edit > Project Settings > Tags."); }
            }

            // Asignar layer
            if (segmentLayer >= 0)
                seg.layer = segmentLayer;

            // Posicionar y orientar el segmento
            seg.transform.position = (a + b) * 0.5f;
            seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);

            // CapsuleCollider a lo largo del eje Y local
            var cap = seg.AddComponent<CapsuleCollider>();
            cap.radius = colliderRadius;
            cap.height = length + colliderRadius * 2f;
            cap.direction = 1; // Y axis
            cap.isTrigger = false; // sólido para que el raycast lo detecte
        }

        Debug.Log($"PathLineCollider: {count - 1} segmentos colisionadores creados.");
    }

    void OnDestroy()
    {
        if (_collidersRoot != null)
            Destroy(_collidersRoot);
    }
}
