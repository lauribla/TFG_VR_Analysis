using UnityEngine;

namespace VRLogger
{
    public class ParticipantFlowController : MonoBehaviour
    {
        public static ParticipantFlowController Instance;

        private int currentIndex = 0;
        private string[] participantOrder;
        private string groupId;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            var cfg = ExperimentConfig.Instance.config;

            participantOrder = cfg.participants.order;
            groupId = cfg.session.group_name;

            Debug.Log($"[ParticipantFlow] 🔵 Participantes cargados: {participantOrder.Length}");
            BeginParticipant();
        }

        // -------------------------------------------------------------------
        // INICIAR PARTICIPANTE ACTUAL
        // -------------------------------------------------------------------
        public void BeginParticipant()
        {
            if (currentIndex >= participantOrder.Length)
            {
                Debug.Log("[ParticipantFlow] 🟢 FIN DEL EXPERIMENTO: No quedan participantes.");
                return;
            }

            string userId = participantOrder[currentIndex];
            Debug.Log($"[ParticipantFlow] ▶ Iniciando participante {currentIndex+1}/{participantOrder.Length}: {userId}");

            // Iniciar sesión del usuario actual
            UserSessionManager.Instance.StartSessionForUser(userId, groupId);

            // Activar trackers
            VRTrackingManager.Instance.BeginTrackingForUser();
        }

        // -------------------------------------------------------------------
        // PASAR AL SIGUIENTE
        // -------------------------------------------------------------------
        public void NextParticipant()
        {
            Debug.Log("[ParticipantFlow] ⏹ Terminando participante actual...");
            UserSessionManager.Instance.EndSession();
            VRTrackingManager.Instance.EndTracking();

            currentIndex++;
            BeginParticipant();
        }
    }
}
