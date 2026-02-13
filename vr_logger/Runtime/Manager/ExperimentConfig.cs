using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        [Header("Profile System")]
        public ExperimentProfile activeProfile;

        [Header("Session Configuration")]
        public string SessionName = "Dia_1";
        public string GroupName = "Grupo_A";
        public string IndependentVariable = "";
        public float TurnDurationSeconds = 60f;

        [Header("Participants")]
        public int ParticipantCount = 4;
        public List<string> ParticipantOrder = new List<string> { "U001", "U002", "U003", "U004" };
        
        [Tooltip("If set, overrides the Participant list for a single-user run.")]
        public string ManualParticipantName = "";

        [Header("Experiment Info")]
        public string ExperimentId = "shooting_basic";
        public string FormulaProfile = "default";
        [TextArea] public string Description = "Basic shooting experiment";

        [Header("Modules")]
        public bool UseGazeTracker = true;
        public bool UseMovementTracker = true;
        public bool UseHandTracker = false;
        public bool UseFootTracker = false;
        public bool UseRaycastLogger = true;
        public bool UseCollisionLogger = true;

        [Header("GM Controls")]
        public bool EnableGMControls = true;

        [System.Serializable]
        public enum FlowModeType { Turns, Manual }
        
        [System.Serializable]
        public enum EndConditionType { Timer, GM }

        [Header("Flow")]
        public FlowModeType FlowMode = FlowModeType.Turns;
        public EndConditionType EndCondition = EndConditionType.Timer;

        [System.Serializable]
        public struct MetricConfig
        {
            public bool Enabled;
            [Range(0, 1)] public float Weight;
            public float Min;
            public float Max;
            public bool Invert;
        }


        [System.Serializable]
        public enum EventRoleType
        {
            Action_Success,
            Action_Fail,
            Task_Start,
            Task_End,
            Task_Restart,
            Task_Abandoned,
            Session_Start,
            Session_End,
            Navigation_Error,
            Interface_Error,
            Interface_Action,
            Help_Event,
            Movement_Update,
            Movement_Action,
            Exploration_Event,
            Gaze_Event,
            Gaze_Action,
            Gaze_Sample,
            Interaction_Event,
            Audio_Event,
            Audio_Reaction,
            Inactivity_Event,
            System_Event,
            Performance_Measure,
            Custom_Event
        }

        [System.Serializable]
        public struct EventRoleMapping
        {
            public string EventName;
            public EventRoleType Role;
        }

        [Header("Event Mapping")]
        public List<EventRoleMapping> CustomEventRoles = new List<EventRoleMapping>();



        [System.Serializable]
        public struct MetricsCategory
        {
            public MetricConfig HitRatio;
            public MetricConfig SuccessRate;
            public MetricConfig LearningCurveMean;
            public MetricConfig Progression;
            public MetricConfig SuccessAfterRestart;

            public MetricConfig AvgReactionTimeMs;
            public MetricConfig AvgTaskDurationMs;
            public MetricConfig TimePerSuccessS;
            public MetricConfig NavigationErrors;

            public MetricConfig LearningStability;
            public MetricConfig ErrorReductionRate;
            public MetricConfig VoluntaryPlayTimeS;
            public MetricConfig AidUsage;
            public MetricConfig InterfaceErrors;

            public MetricConfig ActivityLevelPerMin;
            public MetricConfig FirstSuccessTimeS;
            public MetricConfig InactivityTimeS;
            public MetricConfig SoundLocalizationTimeS;
            public MetricConfig AudioPerformanceGain;
        }
        
        [Header("Metrics")]
        public MetricsCategory Metrics = new MetricsCategory
        {
            // Default Values matching original JSON
            HitRatio = new MetricConfig { Enabled = true, Weight = 0.35f, Min = 0, Max = 1, Invert = false },
            SuccessRate = new MetricConfig { Enabled = true, Weight = 0.30f, Min = 0, Max = 1, Invert = false },
            LearningCurveMean = new MetricConfig { Enabled = true, Weight = 0.15f, Min = 0, Max = 1, Invert = false },
            Progression = new MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 10, Invert = false },
            SuccessAfterRestart = new MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 1, Invert = false },

            AvgReactionTimeMs = new MetricConfig { Enabled = true, Weight = 0.40f, Min = 100, Max = 2000, Invert = true },
            AvgTaskDurationMs = new MetricConfig { Enabled = true, Weight = 0.30f, Min = 1000, Max = 30000, Invert = true },
            TimePerSuccessS = new MetricConfig { Enabled = true, Weight = 0.20f, Min = 0, Max = 60, Invert = true },
            NavigationErrors = new MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 10, Invert = true },
            
            LearningStability = new MetricConfig { Enabled = true, Weight = 0.30f, Min = 0, Max = 1, Invert = false },
            ErrorReductionRate = new MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 1, Invert = false },
            VoluntaryPlayTimeS = new MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 60, Invert = false },
            AidUsage = new MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 5, Invert = true },
            InterfaceErrors = new MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 5, Invert = true },

            ActivityLevelPerMin = new MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 100, Invert = false },
            FirstSuccessTimeS = new MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 30, Invert = true },
            InactivityTimeS = new MetricConfig { Enabled = true, Weight = 0.20f, Min = 0, Max = 60, Invert = true },
            SoundLocalizationTimeS = new MetricConfig { Enabled = true, Weight = 0.15f, Min = 0, Max = 10, Invert = true },
            AudioPerformanceGain = new MetricConfig { Enabled = true, Weight = 0.15f, Min = -1, Max = 1, Invert = false },
        };

        private JObject jsonConfig;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            BuildJsonFromInspector();
        }

        void BuildJsonFromInspector()
        {
            // Determine source: Profile OR Local Inspector
            ExperimentProfile p = activeProfile;

            // Helper to get value
            string sName = p ? p.SessionName : SessionName;
            string gName = p ? p.GroupName : GroupName;
            string iv = p ? p.IndependentVariable : IndependentVariable;
            float tDuration = p ? p.TurnDurationSeconds : TurnDurationSeconds;
            
            // MANUAL USER ID LOGIC
            string manualUser = p ? p.ManualParticipantName : ManualParticipantName;
            
            int pCount = p ? p.ParticipantCount : ParticipantCount;
            List<string> pOrder = p ? p.ParticipantOrder : ParticipantOrder;
            
            if (!string.IsNullOrEmpty(manualUser))
            {
                pCount = 1;
                pOrder = new List<string> { manualUser };
                Debug.Log($"[ExperimentConfig] ⚠️ Using Manual Participant Override: {manualUser}");
            }
            
            string exId = p ? p.ExperimentId : ExperimentId;
            string fProf = p ? p.FormulaProfile : FormulaProfile;
            string desc = p ? p.Description : Description;
            
            bool useGaze = p ? p.UseGazeTracker : UseGazeTracker;
            bool useMove = p ? p.UseMovementTracker : UseMovementTracker;
            bool useHand = p ? p.UseHandTracker : UseHandTracker;
            bool useFoot = p ? p.UseFootTracker : UseFootTracker;
            bool useRay = p ? p.UseRaycastLogger : UseRaycastLogger;
            bool useCol = p ? p.UseCollisionLogger : UseCollisionLogger;
            
            FlowModeType fMode = p ? p.FlowMode : FlowMode;
            EndConditionType eCond = p ? p.EndCondition : EndCondition;
            bool gmEnable = p ? p.EnableGMControls : EnableGMControls;

            MetricsCategory met = p ? p.Metrics : Metrics;
            
            // Custom Roles Source
            List<EventRoleMapping> customRoles = p ? p.CustomEventRoles : CustomEventRoles;

            Debug.Log($"[ExperimentConfig] Building JSON. ActiveProfile: {(p ? p.name : "None")}");
            Debug.Log($"[ExperimentConfig] Sample Metric (HitRatio): Enabled={met.HitRatio.Enabled}, Weight={met.HitRatio.Weight}");


            // Reconstruct the JSON structure expected by the rest of the system
            jsonConfig = new JObject();

            // Session
            jsonConfig["session"] = new JObject
            {
                { "session_name", sName },
                { "group_name", gName },
                { "independent_variable", iv },
                { "turn_duration_seconds", tDuration }
            };

            // Participants
            jsonConfig["participants"] = new JObject
            {
                { "count", pCount },
                { "order", new JArray(pOrder) }
            };

            // Experiment Selection
            jsonConfig["experiment_selection"] = new JObject
            {
                { "experiment_id", exId },
                { "formula_profile", fProf },
                { "description", desc }
            };

            // Modules
            jsonConfig["modules"] = new JObject
            {
                { "useGazeTracker", useGaze },
                { "useMovementTracker", useMove },
                { "useHandTracker", useHand },
                { "useFootTracker", useFoot },
                { "useRaycastLogger", useRay },
                { "useCollisionLogger", useCol }
            };

            // Flow
            JObject gmControls = new JObject
            {
                { "enabled", gmEnable }
            };

            jsonConfig["participant_flow"] = new JObject
            {
                { "mode", fMode.ToString().ToLower() },
                { "end_condition", eCond.ToString().ToLower() },
                { "gm_controls", gmControls }
            };

            // Metrics
            jsonConfig["metrics"] = new JObject
            {
                { "efectividad", new JObject 
                    {
                        { "hit_ratio", MetricToJson(met.HitRatio) },
                        { "success_rate", MetricToJson(met.SuccessRate) },
                        { "learning_curve_mean", MetricToJson(met.LearningCurveMean) },
                        { "progression", MetricToJson(met.Progression) },
                        { "success_after_restart", MetricToJson(met.SuccessAfterRestart) }
                    } 
                },
                { "eficiencia", new JObject 
                    {
                        { "avg_reaction_time_ms", MetricToJson(met.AvgReactionTimeMs) },
                        { "avg_task_duration_ms", MetricToJson(met.AvgTaskDurationMs) },
                        { "time_per_success_s", MetricToJson(met.TimePerSuccessS) },
                        { "navigation_errors", MetricToJson(met.NavigationErrors) }
                    } 
                },
                { "satisfaccion", new JObject 
                    {
                        { "learning_stability", MetricToJson(met.LearningStability) },
                        { "error_reduction_rate", MetricToJson(met.ErrorReductionRate) },
                        { "voluntary_play_time_s", MetricToJson(met.VoluntaryPlayTimeS) },
                        { "aid_usage", MetricToJson(met.AidUsage) },
                        { "interface_errors", MetricToJson(met.InterfaceErrors) }
                    } 
                },
                { "presencia", new JObject 
                    {
                        { "activity_level_per_min", MetricToJson(met.ActivityLevelPerMin) },
                        { "first_success_time_s", MetricToJson(met.FirstSuccessTimeS) },
                        { "inactivity_time_s", MetricToJson(met.InactivityTimeS) },
                        { "sound_localization_time_s", MetricToJson(met.SoundLocalizationTimeS) },
                        { "audio_performance_gain", MetricToJson(met.AudioPerformanceGain) }
                    } 
                }
            };

            // Event Roles (Base Hardcoded + Custom Overrides)
            // Strategy: Create a Dictionary for fast lookup/override, then serialize
            Dictionary<string, string> roleMap = new Dictionary<string, string>
            {
                { "target_hit", "action_success" },
                { "target_miss", "action_fail" },
                { "goal_reached", "action_success" },
                { "object_placed_correctly", "action_success" },
                { "object_dropped", "action_fail" },
                { "fall_detected", "action_fail" },
                { "task_start", "task_start" },
                { "task_end", "task_end" },
                { "task_restart", "task_restart" },
                { "task_timeout", "task_abandoned" },
                { "task_abandoned", "task_abandoned" },
                { "session_start", "session_start" },
                { "session_end", "session_end" },
                { "early_leave", "session_end" },
                { "collision", "navigation_error" },
                { "navigation_error", "navigation_error" },
                { "controller_error", "interface_error" },
                { "wrong_button", "interface_error" },
                { "ui_interaction", "interface_action" },
                { "menu_opened", "interface_action" },
                { "menu_closed", "interface_action" },
                { "help_requested", "help_event" },
                { "guide_used", "help_event" },
                { "hint_used", "help_event" },
                { "tutorial_step", "help_event" },
                { "movement_frame", "movement_update" },
                { "teleport_used", "movement_action" },
                { "walk_step", "movement_action" },
                { "sharp_turn", "movement_action" },
                { "turn_event", "movement_action" },
                { "inspect_object", "exploration_event" },
                { "gaze_sustained", "gaze_event" },
                { "gaze_frequency_change", "gaze_event" },
                { "gaze_exit", "gaze_event" },
                { "action_with_gaze_check", "gaze_action" },
                { "eye_tracking_sample", "gaze_sample" },
                { "controller_action", "interaction_event" },
                { "object_grabbed", "interaction_event" },
                { "object_released", "interaction_event" },
                { "trigger_pull", "interaction_event" },
                { "audio_triggered", "audio_event" },
                { "sound_source_active", "audio_event" },
                { "head_turn", "audio_reaction" },
                { "inactivity_frame", "inactivity_event" },
                { "timeout_detected", "inactivity_event" },
                { "system_warning", "system_event" },
                { "performance_drop", "system_event" },
                { "latency_spike", "system_event" },
                { "spawn_object", "task_start" },
                { "bullet_hit", "action_success" },
                { "reaction_time", "performance_measure" },
                { "manual_despawn", "action_fail" },
                { "custom_event", "custom_event" }
            };

            // Apply Custom Overrides
            if (customRoles != null)
            {
                foreach (var mapping in customRoles)
                {
                    if (!string.IsNullOrEmpty(mapping.EventName))
                    {
                        string defaultRole = mapping.Role.ToString().ToLower();
                        if (roleMap.ContainsKey(mapping.EventName))
                            roleMap[mapping.EventName] = defaultRole; // Overwrite
                        else
                            roleMap.Add(mapping.EventName, defaultRole); // Add new
                    }
                }
            }

            // Convert Dictionary to JObject
            JObject rolesJson = new JObject();
            foreach (var kvp in roleMap)
            {
                rolesJson.Add(kvp.Key, kvp.Value);
            }

            jsonConfig["event_roles"] = rolesJson;

            // Custom Events (Hardcoded logic mapping for Python Parser)
            // Matched from experiment_config.json
            jsonConfig["custom_events"] = new JObject
            {
                { "hand_movement", "motion_capture" },
                { "body_rotation", "motion_capture" },
                { "controller_vibration", "feedback_event" },
                { "npc_interaction", "social_event" },
                { "menu_navigation", "interface_action" },
                { "object_throw", "interaction_event" },
                { "object_collision", "physics_event" },
                { "gaze_on_npc", "attention_event" },
                { "camera_shake", "system_event" },
                { "microphone_input", "voice_event" }
            };

            if (p != null)
                Debug.Log($"[ExperimentConfig] ✅ Config built from PROFILE '{p.name}'.");
            else
                Debug.Log("[ExperimentConfig] ✅ Config built from Inspector values.");
        }

        JObject MetricToJson(MetricConfig m)
        {
            // Heuristic: If min/max are effectively integers, serialize as such to match original JSON
            // But JSONL parser in Python might not care about 0 vs 0.0. 
            // However, sticking to float is safer for generic MetricConfig.
            return new JObject
            {
                { "enabled", m.Enabled },
                { "weight", m.Weight },
                { "min", m.Min },
                { "max", m.Max },
                { "invert", m.Invert }
            };
        }

        public async void SendConfigAsLog()
        {
            if (jsonConfig == null)
            {
                Debug.LogError("[ExperimentConfig] ❌ No hay config cargado para enviar.");
                return;
            }

            Debug.Log("[ExperimentConfig] 📤 Enviando config REAL tal cual a Mongo...");

            await LoggerService.LogEvent(
                "config",
                "experiment_config",
                null,
                jsonConfig
            );

            Debug.Log("[ExperimentConfig] ✅ Config enviado a Mongo.");
        }

        public JObject GetConfig()
        {
            return jsonConfig;
        }

#if UNITY_EDITOR
        [ContextMenu("Load From Profile (Overwrite Inspector)")]
        public void LoadFromProfile()
        {
            if (activeProfile == null)
            {
                Debug.LogError("No profile assigned!");
                return;
            }

            // Copy Simple Fields
            SessionName = activeProfile.SessionName;
            GroupName = activeProfile.GroupName;
            IndependentVariable = activeProfile.IndependentVariable;
            TurnDurationSeconds = activeProfile.TurnDurationSeconds;
            
            ParticipantCount = activeProfile.ParticipantCount;
            ParticipantOrder = new List<string>(activeProfile.ParticipantOrder);
            ManualParticipantName = activeProfile.ManualParticipantName;
            
            ExperimentId = activeProfile.ExperimentId;
            FormulaProfile = activeProfile.FormulaProfile;
            Description = activeProfile.Description;
            
            UseGazeTracker = activeProfile.UseGazeTracker;
            UseMovementTracker = activeProfile.UseMovementTracker;
            UseHandTracker = activeProfile.UseHandTracker;
            UseFootTracker = activeProfile.UseFootTracker;
            UseRaycastLogger = activeProfile.UseRaycastLogger;
            UseCollisionLogger = activeProfile.UseCollisionLogger;
            
            FlowMode = activeProfile.FlowMode;
            EndCondition = activeProfile.EndCondition;
            EnableGMControls = activeProfile.EnableGMControls;
            
            Metrics = activeProfile.Metrics;
            CustomEventRoles = new List<EventRoleMapping>(activeProfile.CustomEventRoles);

            Debug.Log($"Loaded values from profile: {activeProfile.name}");
        }

        [ContextMenu("Save To Profile (Overwrite Asset)")]
        public void SaveToProfile()
        {
            if (activeProfile == null)
            {
                Debug.LogError("No profile assigned to save to!");
                return;
            }

            // Copy Fields Back
            activeProfile.SessionName = SessionName;
            activeProfile.GroupName = GroupName;
            activeProfile.IndependentVariable = IndependentVariable;
            activeProfile.TurnDurationSeconds = TurnDurationSeconds;
            
            activeProfile.ParticipantCount = ParticipantCount;
            activeProfile.ParticipantOrder = new List<string>(ParticipantOrder);
            activeProfile.ManualParticipantName = ManualParticipantName;
            
            activeProfile.ExperimentId = ExperimentId;
            activeProfile.FormulaProfile = FormulaProfile;
            activeProfile.Description = Description;
            
            activeProfile.UseGazeTracker = UseGazeTracker;
            activeProfile.UseMovementTracker = UseMovementTracker;
            activeProfile.UseHandTracker = UseHandTracker;
            activeProfile.UseFootTracker = UseFootTracker;
            activeProfile.UseRaycastLogger = UseRaycastLogger;
            activeProfile.UseCollisionLogger = UseCollisionLogger;
            
            activeProfile.FlowMode = FlowMode;
            activeProfile.EndCondition = EndCondition;
            activeProfile.EnableGMControls = EnableGMControls;
            
            
            activeProfile.Metrics = Metrics;
            activeProfile.CustomEventRoles = new List<EventRoleMapping>(CustomEventRoles);

            UnityEditor.EditorUtility.SetDirty(activeProfile);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Saved values to profile: {activeProfile.name}");
        }
#endif
    }
}
