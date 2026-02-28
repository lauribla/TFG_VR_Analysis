using UnityEngine;

namespace VRLogger.Trackers
{
    /// <summary>
    /// Componente Plug & Play para interactuables y objetivos.
    /// ALIMENTA LAS MÉTRICAS EN PYTHON: HitRatio, FirstSuccessTimeS, AvgReactionTimeMs, AimErrors.
    /// USO: Arrástralo a enemigos, dianas o elementos que el usuario debe interactuar correctamente.
    /// </summary>
    [AddComponentMenu("VR Logger/Metrics/Action Outcome Logger")]
    public class ActionOutcomeLogger : MonoBehaviour
    {
        [Tooltip("ID del Objetivo (ej. Enemy_Red, Diana_Principal). Si está vacío usa el nombre del objeto.")]
        public string targetId = "";

        [Tooltip("Informa automáticamente que el objetivo apareció al instante (útil cuando el objeto hace spawn).")]
        public bool notifyAppearedOnStart = true;

        private void Start()
        {
            if (notifyAppearedOnStart)
            {
                ReportTargetAppeared();
            }
        }

        private string GetTargetId()
        {
            return string.IsNullOrEmpty(targetId) ? gameObject.name : targetId;
        }

        /// <summary>
        /// Informa a Python que este objetivo apareció en escena ("target_appeared").
        /// (Python usa esto para calcular el AvgReactionTimeMs desde que aparece hasta que hay Success/Fail).
        /// </summary>
        public void ReportTargetAppeared()
        {
            LogAPI.LogTargetAppeared(GetTargetId());
            Debug.Log($"[ActionOutcomeLogger] 👁️ Target Appeared: {GetTargetId()}");
        }

        /// <summary>
        /// Registra un Acierto o Éxito contra este objetivo.
        /// Conéctalo al UnityEvent de recibir daño, colisionar con la mano, etc.
        /// </summary>
        public void ReportSuccess()
        {
            // Python asocia success y fail al ID, calcularemos reaction time allí por timestamp
            LogAPI.LogTargetHit(GetTargetId(), 1, 0f); 
            Debug.Log($"[ActionOutcomeLogger] 💥 Target SUCCESS (Hit): {GetTargetId()}");
        }

        /// <summary>
        /// Registra un Fallo.
        /// Conéctalo al UnityEvent de disparo fallido, despawn por tiempo agotado, etc.
        /// </summary>
        public void ReportFail()
        {
            LogAPI.LogTargetMiss(GetTargetId(), 0f);
            Debug.Log($"[ActionOutcomeLogger] 💨 Target FAIL (Miss): {GetTargetId()}");
        }
    }
}
