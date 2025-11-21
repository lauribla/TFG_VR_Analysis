using UnityEngine;

namespace VRLogger
{
    public class ParticipantFlowController : MonoBehaviour
    {
        public static ParticipantFlowController Instance;

        // Index of current participant in the order list
        private int currentIndex = 0;

        // Order of participant ids (from config)
        private string[] participantOrder;
        private string groupId;

        // Duration of each turn in seconds (from config)
        private float turnDurationSeconds = 60f; // default fallback

        // Time control
        private float turnStartTime = 0f;
        private bool turnActive = false;

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
            // Read experiment config
            var cfg = ExperimentConfig.Instance.config;

            // 1️⃣ ENVIAR CONFIG A MONGODB ANTES DE EMPEZAR
            ExperimentConfig.Instance.SendConfigAsLog();

            participantOrder = cfg.participants.order;
            groupId = cfg.session.group_name;

            // IMPORTANT:
            // Make sure your config class has:
            // public float turn_duration_seconds;
            // inside cfg.session, and set it in the JSON.
            if (cfg.session.turn_duration_seconds > 0f)
            {
                turnDurationSeconds = cfg.session.turn_duration_seconds;
            }

            Debug.Log($"[ParticipantFlow] Loaded {participantOrder.Length} participants.");
            Debug.Log($"[ParticipantFlow] Turn duration: {turnDurationSeconds} seconds.");

            ExperimentConfig.Instance.SendConfigAsLog();
            BeginParticipant();
        }

        void Update()
        {
            // Automatic turn change based on time
            if (turnActive)
            {
                if (Time.time >= turnStartTime + turnDurationSeconds)
                {
                    Debug.Log("[ParticipantFlow] Turn time finished, moving to next participant.");
                    NextParticipant();
                }
            }
        }

        // -------------------------------------------------------------
        // Start current participant
        // -------------------------------------------------------------
        private void BeginParticipant()
        {
            if (currentIndex >= participantOrder.Length)
            {
                Debug.Log("[ParticipantFlow] Experiment finished. No more participants.");
                turnActive = false;
                return;
            }

            string userId = participantOrder[currentIndex];

            Debug.Log($"[ParticipantFlow] Starting participant {currentIndex + 1}/{participantOrder.Length}: {userId}");

            // Start session and tracking
            UserSessionManager.Instance.StartSessionForUser(userId, groupId);
            VRTrackingManager.Instance.BeginTrackingForUser();

            // Start turn timer
            turnStartTime = Time.time;
            turnActive = true;
        }

        // -------------------------------------------------------------
        // End current participant and go to next
        // -------------------------------------------------------------
        private void NextParticipant()
        {
            if (!turnActive)
            {
                // Already finished or not started
                return;
            }

            Debug.Log("[ParticipantFlow] Ending current participant.");

            // End tracking and session
            VRTrackingManager.Instance.EndTracking();
            UserSessionManager.Instance.EndSession();

            turnActive = false;

            // Move index and start next participant (if any)
            currentIndex++;

            if (currentIndex < participantOrder.Length)
            {
                BeginParticipant();
            }
            else
            {
                Debug.Log("[ParticipantFlow] All participants completed. Experiment fully finished.");
            }
        }

        // -------------------------------------------------------------
        // Optional: public method to force skip to next participant
        // (for teacher override, debugging, etc.)
        // -------------------------------------------------------------
        public void ForceNextParticipant()
        {
            Debug.Log("[ParticipantFlow] ForceNextParticipant called.");
            NextParticipant();
        }
    }
}
