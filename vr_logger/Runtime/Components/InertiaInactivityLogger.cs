using UnityEngine;
using System.Collections;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Detecta periodos prolongados de quietud en la experiencia.
    /// Útil para capturar analíticas de "Voluntary Play Time", ausencias del jugador frente al visor,
    /// o momentos excesivos de confusión (mirando a un mismo sitio sin avanzar).
    /// Altamente automático: se arrastra al manager de sesión o jugador,
    /// y funciona con la cámara (target tracker) monitorizando su varianza de posición en el tiempo.
    /// </summary>
    public class InertiaInactivityLogger : MonoBehaviour
    {
        [Header("Configuración de Inactividad")]
        [Tooltip("Transform a observar. Usualmente será el Main Camera que la cabeza del jugador (Head/HMD).")]
        public Transform targetTracker;

        [Tooltip("Segundos consecutivos sin moverse para considerar al usuario inactivo.")]
        public float inactivityThreshold_s = 5.0f;

        [Tooltip("Tolerancia de movimiento residual humano (distancia max en metros a la que cuenta como inactivo). " +
                 "Un humano quieto siempre tiembla un poco, ajusta a 0.05 (5cm) o 0.03 (3cm) aprox.")]
        public float positionalVariance_m = 0.03f;

        [Tooltip("Frecuencia de chequeo en segundos, para no saturar procesos cada Update. Recomendado: 0.5s")]
        public float checkInterval_s = 0.5f;

        // Estado interno
        private bool isCurrentlyInactive = false;
        private float inactiveTimer = 0f;
        private float lastActiveTime = 0f;
        private Vector3 originPosition;
        private Coroutine trackingCoroutine;

        private void Start()
        {
            if (targetTracker == null)
            {
                if (Camera.main != null)
                {
                    targetTracker = Camera.main.transform;
                    Debug.Log($"[InertiaInactivityLogger] Tracker asignado automáticamente al {targetTracker.name}.");
                }
                else
                {
                    Debug.LogWarning("[InertiaInactivityLogger] Falta Target Tracker (Head) y no se encontró MainCamera.");
                    this.enabled = false;
                    return;
                }
            }

            originPosition = targetTracker.position;
            lastActiveTime = Time.time;
            trackingCoroutine = StartCoroutine(InactivityCheckCycle());
        }

        private void OnDisable()
        {
            if (trackingCoroutine != null) StopCoroutine(trackingCoroutine);

            // Cerrar el log de forma segura si cerramos el juego a la mitad de uno
            if (isCurrentlyInactive)
            {
                EndInactivePeriod();
            }
        }

        private IEnumerator InactivityCheckCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval_s);

                float currentDistance = Vector3.Distance(targetTracker.position, originPosition);

                // El usuario ha roto la distancia del threshold -> SE HA MOVIDO
                if (currentDistance > positionalVariance_m)
                {
                    if (isCurrentlyInactive)
                    {
                        // Estaba inactivo, y se acabó la inactividad.
                        EndInactivePeriod();
                    }
                    else
                    {
                        // Estaba activo, y sigue activo -> Actualiza su pivote para el siguiente microchequeo
                        inactiveTimer = 0f;
                    }
                    
                    originPosition = targetTracker.position; // Actualiza el nuevo eje donde paró
                    lastActiveTime = Time.time;
                }
                // El usuario NO HA ROTO el umbral de distancia. -> ESTÁ QUIETO
                else
                {
                    inactiveTimer += checkInterval_s;

                    if (inactiveTimer >= inactivityThreshold_s && !isCurrentlyInactive)
                    {
                        // Empezó periodo inactivo fuerte
                        StartInactivePeriod();
                    }
                }
            }
        }

        private void StartInactivePeriod()
        {
            isCurrentlyInactive = true;
            Debug.Log("[InertiaInactivityLogger] El usuario está inactivo.");

            LoggerService.LogEvent(
                eventType: "metrics_activity",
                eventName: "INACTIVE_PERIOD_START",
                eventValue: new { 
                    trackerName = targetTracker.name,
                    threshold_s = inactivityThreshold_s
                },
                eventContext: null
            );
        }

        private void EndInactivePeriod()
        {
            isCurrentlyInactive = false;
            float totalInactiveTimeMs = (Time.time - lastActiveTime) * 1000f;

            Debug.Log($"[InertiaInactivityLogger] El usuario se movió tras {totalInactiveTimeMs / 1000f:F1}s inactivo.");

            LoggerService.LogEvent(
                eventType: "metrics_activity",
                eventName: "INACTIVE_PERIOD_END",
                eventValue: new { 
                    duration_ms = totalInactiveTimeMs
                },
                eventContext: null
            );
            
            inactiveTimer = 0f;
        }
    }
}
