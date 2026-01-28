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

        // COOLDOWN LOGIC
        private bool isCooldown = false;
        private float cooldownDuration = 5f;

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
            RefreshSettings();
            // StartNextParticipant(); // REMOVED: Waits for ConfigUI to call RestartExperiment()
        }

        // ------------------------------------------------------------------
        // REFRESH SETTINGS (Called by ConfigUI after save)
        // ------------------------------------------------------------------
        public void RefreshSettings()
        {
            JObject cfg = ExperimentConfig.Instance.GetConfig();
            
            if (cfg == null)
            {
                Debug.LogError("[ParticipantFlow] Config not loaded");
                return;
            }

            // PARTICIPANTS
            participantOrder = (JArray)cfg["participants"]?["order"];
            int desiredCount = (int?)cfg["participants"]?["count"] ?? participantOrder.Count;

            // Auto-Generate if count > defined list
            if (participantOrder != null)
            {
               int currentLen = participantOrder.Count;
               
               if (desiredCount > currentLen)
               {
                   // EXPAND
                   int needed = desiredCount - currentLen;
                   for(int i=0; i<needed; i++)
                   {
                       string lastId = participantOrder[currentLen + i - 1].ToString(); // basic assumption
                       // Generating U005, U006... based on index
                       string newId = "U" + (currentLen + i + 1).ToString("D3");
                       participantOrder.Add(newId);
                   }
                   Debug.Log($"[ParticipantFlow] Expanded to {participantOrder.Count} participants.");
               }
               else if (desiredCount < currentLen)
               {
                   // TRUNCATE
                   // Remove from the end
                   while (participantOrder.Count > desiredCount)
                   {
                       participantOrder.RemoveAt(participantOrder.Count - 1);
                   }
                   Debug.Log($"[ParticipantFlow] Truncated to {participantOrder.Count} participants.");
               }
            }
            
            // GROUP
            groupId = (string)cfg["session"]?["group_name"];
            if (string.IsNullOrEmpty(groupId)) groupId = "GROUP";

            // TURN DURATION
            JToken tds = cfg["session"]?["turn_duration_seconds"];
            if (tds != null)
            {
                turnDurationSeconds = (float)tds;
                
                // FORCE UPDATE CURRENT TIMER for the active turn (First Participant)
                // Since game is paused while ConfigUI is open, this is safe.
                timer = turnDurationSeconds; 
                
                Debug.Log($"[ParticipantFlow] Turn Duration Updated: {turnDurationSeconds}s (Applied to current timer)");
            }

            // FLOW
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
            
            // If experiment hasn't really started (no sessions logs yet), 
            // updating timer here is safe for the *first* participant.
            if (experimentRunning && !isCooldown) 
            {
                // Optional: Force update current timer if within reasonable bounds?
                // For now, let's just update the variable so NEXT turn uses it. 
                // However, user expects immediate effect if they haven't started playing.
                // Since this is called BEFORE StartNextParticipant initially, it's fine.
                // If called DURING a turn, we might want to clamp timer? Let's leave as is for now: affects NEXT turn.
            }
        }

        // Called by ConfigUI after user presses Start to properly (re)start the experiment
        public void RestartExperiment()
        {
            currentIndex = 0;
            experimentRunning = false;
            isCooldown = false;
            paused = false;
            RefreshSettings();
            StartNextParticipant();
            Debug.Log($"[ParticipantFlow] Experiment Restarted. Mode={flowMode}, EndCondition={endCondition}, Timer={turnDurationSeconds}s");
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
                    if (isCooldown)
                    {
                        // Cooldown Finished -> Start NEXT
                        StartNextParticipant();
                    }
                    else
                    {
                        // Turn Finished -> Enter COOLDOWN
                        EndCurrentParticipant("timer");
                    }
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

            isCooldown = false; // Reset cooldown

            string rawId = (string)participantOrder[currentIndex];
            // Fix: Prefix with GroupID to ensure uniqueness across sessions/groups in analysis
            string userId = $"{groupId}_{rawId}";

            UserSessionManager.Instance.StartSessionForUser(userId, groupId);
            VRTrackingManager.Instance.BeginTrackingForUser();

            Debug.Log("[ParticipantFlow] Start participant: " + userId);
            
            // IMPORTANT: Use the potentially updated turnDurationSeconds
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

            // If no more participants, just try start next (it will handle finish)
            if (currentIndex >= participantOrder.Count)
            {
                StartNextParticipant();
                return;
            }

            // COOLDOWN LOGIC
            // Only use cooldown if we are in TIMER mode. 
            // In GM mode, 'Next' means 'Next Instantly'.
            if (endCondition == "timer")
            {
                isCooldown = true;
                timer = cooldownDuration;
                Debug.Log($"[ParticipantFlow] Starting Cooldown ({cooldownDuration}s)...");
            }
            else
            {
                // GM Mode: immediate switch
                StartNextParticipant();
            }
        }

        // -------------------------
        // PUBLIC API for GM console
        // -------------------------
        public bool IsRunning()
        {
            return experimentRunning;
        }

        public bool IsPaused => paused;

        public void TogglePause()
        {
            paused = !paused;
            Debug.Log("[ParticipantFlow] Pause=" + paused);
        }

        public void GM_EndTurn()
        {
            if (!experimentRunning) return;
            if (paused) return;
            // if (isCooldown) return; // Allow forcing next even if cooldown (e.g. if stuck)

            EndCurrentParticipant("gm");
        }

        public void GM_NextParticipant()
        {
            // For prototype: next == end current + start next
            GM_EndTurn();
        }

        // -------------------------
        // UI HELPERS
        // -------------------------
        public float GetTimeRemaining() { return timer; }
        public string GetEndCondition() { return endCondition; }
        public bool IsCooldown() { return isCooldown; }
        
        public string GetCurrentParticipant()
        {
             if (participantOrder == null || currentIndex >= participantOrder.Count) return "-";
             return participantOrder[currentIndex].ToString();
        }

        public string GetNextParticipant()
        {
             if (participantOrder == null || currentIndex + 1 >= participantOrder.Count) return "END";
             return participantOrder[currentIndex + 1].ToString();
        }
    }
}
