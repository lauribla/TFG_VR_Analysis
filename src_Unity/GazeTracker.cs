using UnityEngine;
using System;

public class GazeTracker : MonoBehaviour
{
//se dedica a coger la trayectoria periódicamente de las gafas y ahcer logs con ellas 
//no tiene que estar dentro de solution "src_bd_unity" porque esto lo llamará unity y las solutions es para hacer run manual nosotros 
    [Header("Tracking settings")]
    public float checkInterval = 0.25f;           // Intervalo entre raycasts
    public float fixationThreshold = 2.0f;        // Tiempo necesario para considerar fijación
    public LayerMask gazeLayerMask;              // Para ignorar ciertos objetos

    private float nextCheckTime = 0f;
    private GameObject lastHit = null;
    private float gazeStartTime = 0f;

    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            RaycastHit hit;
            Vector3 origin = Camera.main.transform.position;
            Vector3 direction = GetGazeDirection();

            if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity, gazeLayerMask))
            {
                GameObject currentHit = hit.collider.gameObject;

                if (currentHit == lastHit)
                {
                    float duration = Time.time - gazeStartTime;

                    if (duration >= fixationThreshold)
                    {
                        Logger.LogEvent("gaze", "gaze_sustained", duration, new
                        {
                            object_name = currentHit.name,
                            duration_ms = (int)(duration * 1000),
                            position = currentHit.transform.position
                        });

                        gazeStartTime = Time.time + 999f; // evitar duplicados
                    }
                }
                else
                {
                    Logger.LogEvent("gaze", "gaze_enter", null, new
                    {
                        object_name = currentHit.name,
                        position = currentHit.transform.position
                    });

                    lastHit = currentHit;
                    gazeStartTime = Time.time;
                }
            }
            else if (lastHit != null)
            {
                Logger.LogEvent("gaze", "gaze_exit", null, new
                {
                    object_name = lastHit.name
                });

                lastHit = null;
                gazeStartTime = 0f;
            }

            nextCheckTime = Time.time + checkInterval;
        }
    }

    Vector3 GetGazeDirection()
    {
        // Reemplazar esta parte si tienes SDK con eye tracking real
        return Camera.main.transform.forward;
    }
}
