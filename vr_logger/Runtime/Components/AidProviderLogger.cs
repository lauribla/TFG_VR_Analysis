using UnityEngine;

namespace VRLogger.Trackers
{
    /// <summary>
    /// Componente Plug & Play para registrar ayudas usadas por el jugador.
    /// ALIMENTA LAS MÉTRICAS EN PYTHON: AidUsage
    /// USO: Arrástralo a Botones UI de "Pista", Guías 3D que aparecen, o NPCs consejeros.
    /// </summary>
    [AddComponentMenu("VR Logger/Metrics/Aid Provider Logger")]
    public class AidProviderLogger : MonoBehaviour
    {
        [Tooltip("Nombre de esta ayuda para los logs (ej. Pista_Secreta, Guia_Mapa). Si está vacío usa el nombre.")]
        public string aidId = "";

        public enum AidType
        {
            Hint,
            Guide,
            HelpRequest
        }

        public AidType typeOfAid = AidType.Hint;

        private string GetAidId()
        {
            return string.IsNullOrEmpty(aidId) ? gameObject.name : aidId;
        }

        /// <summary>
        /// Conéctalo al UnityEvent de botón "Mostrar Pista" o a la función que muestra el canvas de ayuda.
        /// </summary>
        public void RecordAidUsed()
        {
            switch (typeOfAid)
            {
                case AidType.Hint:
                    LogAPI.LogHintUsed(GetAidId());
                    Debug.Log($"[AidLogger] 💡 Hint Used: {GetAidId()}");
                    break;
                case AidType.Guide:
                    LogAPI.LogGuideUsed(GetAidId());
                    Debug.Log($"[AidLogger] 🗺️ Guide Used: {GetAidId()}");
                    break;
                case AidType.HelpRequest:
                    // Usualmente requiere el ID actual de la Tarea, aquí evitamos depender de otros y enviamos vacío "auto"
                    LogAPI.LogHelpRequested(GetAidId(), "auto_aid_logger");
                    Debug.Log($"[AidLogger] 🙋‍♂️ Help Requested: {GetAidId()}");
                    break;
            }
        }
    }
}
