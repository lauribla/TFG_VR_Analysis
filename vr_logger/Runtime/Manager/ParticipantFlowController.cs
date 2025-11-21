using UnityEngine;
using Newtonsoft.Json.Linq;

namespace VRLogger
{
    public class ParticipantFlowController : MonoBehaviour
    {
        public static ParticipantFlowController Instance;

        private JArray participantOrder;
        private string groupId;
        private float turnDurationSeconds = 30f;
        private int currentIndex = 0;
        private bool experimentRunning = false;

        private float timer = 0f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            JObject cfg = ExperimentConfig.Instance.GetConfig();

            if (cfg == null)
            {
                Debug.LogError("[ParticipantFlow] ❌ Config no cargado");
                return;
            }

            // PARTICIPANTES
            participantOrder = (JArray)cfg["participants"]["order"];

            // GRUPO
            groupId = (string)cfg["session"]["group_name"];

            // DURACIÓN DEL TURNO
            turnDurationSeconds = (float)cfg["session"]["turn_duration_seconds"];

            StartNextParticipant();
        }

        void StartNextParticipant()
        {
            if (participantOrder == null || currentIndex >= participantOrder.Count)
            {
                Debug.Log("[ParticipantFlow] Experimento terminado. No más participantes.");
                experimentRunning = false;
                return;
            }

            string userId = (string)participantOrder[currentIndex];

            UserSessionManager.Instance.StartSessionForUser(userId, groupId);
            VRTrackingManager.Instance.BeginTrackingForUser();

            Debug.Log($"[ParticipantFlow] ▶ Comienza participante: {userId}");

            timer = turnDurationSeconds;
            experimentRunning = true;
        }

        void Update()
        {
            if (!experimentRunning)
                return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                EndCurrentParticipant();
            }
        }

        void EndCurrentParticipant()
        {
            VRTrackingManager.Instance.EndTracking();
            UserSessionManager.Instance.EndSession();

            Debug.Log("[ParticipantFlow] ⏹ Turno finalizado.");

            currentIndex++;
            StartNextParticipant();
        }
    }
}
