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
        private bool paused = false;

        private float timer = 0f;

        // NEW: flow settings
        private string flowMode = "turns";          // "turns" | "manual"
        private string endCondition = "timer";      // "timer" | "gm"

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
                Debug.LogError("[ParticipantFlow] Config not loaded");
                return;
            }

            // PARTICIPANTS
            participantOrder = (JArray)cfg["participants"]?["order"];
            if (participantOrder == null)
            {
                Debug.LogError("[ParticipantFlow] participants.order missing");
                return;
            }

            // GROUP
            groupId = (string)cfg["session"]?["group_name"];
            if (string.IsNullOrEmpty(groupId)) groupId = "GROUP";

            // TURN DURATION
            JToken tds = cfg["session"]?["turn_duration_seconds"];
            if (tds != null)
            {
                turnDurationSeconds = (float)tds;
            }

            // FLOW (NEW, with defaults)
            JObject pf = (JObject)cfg["participant_flow"];
            if (pf != null)
            {
                flowMode = (string)pf["mode"] ?? "turns";
                endCondition = (string)pf["end_condition"] ?? "timer";
            }

            // Default behavior if manual: end by gm
            if (flowMode == "manual" && endCondition == "timer")
            {
                endCondition = "gm";
            }

            StartNextParticipant();
        }

        void Update()
        {
            if (!experimentRunning) return;
            if (paused) return;

            if (endCondition == "timer")
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    EndCurrentParticipant("timer");
                }
            }
            // if endCondition == "gm", do nothing here (GM input ends)
        }

        private void StartNextParticipant()
        {
            if (participantOrder == null || currentIndex >= participantOrder.Count)
            {
                Debug.Log("[ParticipantFlow] Experiment finished. No more participants.");
                experimentRunning = false;
                return;
            }


            string userId = (string)participantOrder[currentIndex];

            UserSessionManager.Instance.StartSessionForUser(userId, groupId);
            VRTrackingManager.Instance.BeginTrackingForUser();

            Debug.Log("[ParticipantFlow] Start participant: " + userId);

            timer = turnDurationSeconds;
            experimentRunning = true;
            paused = false;
        }

        private void EndCurrentParticipant(string reason)
        {
            VRTrackingManager.Instance.EndTracking();
            UserSessionManager.Instance.EndSession();

            Debug.Log("[ParticipantFlow] Turn ended. reason=" + reason);

            currentIndex++;
            StartNextParticipant();
        }

        // -------------------------
        // PUBLIC API for GM console
        // -------------------------
        public bool IsRunning()
        {
            return experimentRunning;
        }

        public void TogglePause()
        {
            paused = !paused;
            Debug.Log("[ParticipantFlow] Pause=" + paused);
        }

        public void GM_EndTurn()
        {
            if (!experimentRunning) return;
            if (paused) return;

            EndCurrentParticipant("gm");
        }

        public void GM_NextParticipant()
        {
            // For prototype: next == end current + start next
            GM_EndTurn();
        }
    }
}
