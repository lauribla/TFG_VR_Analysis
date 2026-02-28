using UnityEngine;

namespace VRLogger.Trackers
{
    /// <summary>
    /// Componente Plug & Play para gestionar el flujo de tareas.
    /// ALIMENTA LAS MÉTRICAS EN PYTHON: AvgTaskDurationMs, SuccessRate, SuccessAfterRestart.
    /// USO: Arrástralo a botones de UI o úsalo mediante UnityEvents en momentos clave (inicio de nivel, final de nivel).
    /// </summary>
    [AddComponentMenu("VR Logger/Metrics/Task Flow Logger")]
    public class TaskFlowLogger : MonoBehaviour
    {
        [Tooltip("Si activas esto, se registrará el inicio de la tarea nada más cargar la escena/spawnear el objeto.")]
        public bool logStartOnAwake = false;

        [Tooltip("ID de la Tarea (ej. Nivel_1, Montar_Arma). Si se deja en blanco, usará el nombre del GameObject.")]
        public string defaultTaskId = "";

        private string GetTaskId()
        {
            return string.IsNullOrEmpty(defaultTaskId) ? gameObject.name : defaultTaskId;
        }

        private void Start()
        {
            if (logStartOnAwake)
            {
                StartTask();
            }
        }

        /// <summary>
        /// Registra el inicio de una tarea.
        /// </summary>
        public void StartTask()
        {
            LogAPI.LogTaskStart(GetTaskId());
            Debug.Log($"[TaskFlowLogger] 🚦 Task Started: {GetTaskId()}");
        }

        /// <summary>
        /// Registra que la tarea ha terminado con ÉXITO.
        /// </summary>
        public void EndTaskSuccess()
        {
            // Nota: Podríamos calcular la duración aquí, pero python MetricsCalculator _derive_task_stats ya lo sabe calcular por tiempos.
            // Para mantener compatibilidad con LogTaskEnd, enviamos duración 0 y que Python lo asigne por Timestamp.
            LogAPI.LogTaskEnd(GetTaskId(), "success", 0f, 0);
            Debug.Log($"[TaskFlowLogger] 🏆 Task Success: {GetTaskId()}");
        }

        /// <summary>
        /// Registra que la tarea ha terminado pero FALLÓ.
        /// </summary>
        public void EndTaskFail()
        {
            LogAPI.LogTaskEnd(GetTaskId(), "failed", 0f, 0);
            Debug.Log($"[TaskFlowLogger] ❌ Task Failed: {GetTaskId()}");
        }

        /// <summary>
        /// Registra que la tarea se reinició.
        /// (Crucial para la métrica SuccessAfterRestart).
        /// </summary>
        public void RestartTask()
        {
            LogAPI.LogTaskRestart(GetTaskId());
            Debug.Log($"[TaskFlowLogger] 🔄 Task Restarted: {GetTaskId()}");
        }
        
        /// <summary>
        /// Registra que la tarea fue abandonada.
        /// </summary>
        public void AbandonTask()
        {
            LogAPI.LogTaskAbandoned(GetTaskId());
            Debug.Log($"[TaskFlowLogger] 🏳️ Task Abandoned: {GetTaskId()}");
        }
    }
}
