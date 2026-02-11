using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        [Header("Session Configuration")]
        public string SessionName = "Dia_1";
        public string GroupName = "Grupo_A";
        public string IndependentVariable = "";
        public float TurnDurationSeconds = 60f;

        [Header("Participants")]
        public int ParticipantCount = 4;
        public List<string> ParticipantOrder = new List<string> { "U001", "U002", "U003", "U004" };

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
            // Reconstruct the JSON structure expected by the rest of the system
            jsonConfig = new JObject();

            // Session
            jsonConfig["session"] = new JObject
            {
                { "session_name", SessionName },
                { "group_name", GroupName },
                { "independent_variable", IndependentVariable },
                { "turn_duration_seconds", TurnDurationSeconds }
            };

            // Participants
            jsonConfig["participants"] = new JObject
            {
                { "count", ParticipantCount },
                { "order", new JArray(ParticipantOrder) }
            };

            // Experiment Selection
            jsonConfig["experiment_selection"] = new JObject
            {
                { "experiment_id", ExperimentId },
                { "formula_profile", FormulaProfile },
                { "description", Description }
            };

            // Modules
            jsonConfig["modules"] = new JObject
            {
                { "useGazeTracker", UseGazeTracker },
                { "useMovementTracker", UseMovementTracker },
                { "useHandTracker", UseHandTracker },
                { "useFootTracker", UseFootTracker },
                { "useRaycastLogger", UseRaycastLogger },
                { "useCollisionLogger", UseCollisionLogger }
            };

            // Flow
            jsonConfig["participant_flow"] = new JObject
            {
                { "mode", FlowMode.ToString().ToLower() },
                { "end_condition", EndCondition.ToString().ToLower() }
            };

            // Metrics
            jsonConfig["metrics"] = new JObject
            {
                { "efectividad", new JObject 
                    {
                        { "hit_ratio", MetricToJson(Metrics.HitRatio) },
                        { "success_rate", MetricToJson(Metrics.SuccessRate) },
                        { "learning_curve_mean", MetricToJson(Metrics.LearningCurveMean) },
                        { "progression", MetricToJson(Metrics.Progression) },
                        { "success_after_restart", MetricToJson(Metrics.SuccessAfterRestart) }
                    } 
                },
                { "eficiencia", new JObject 
                    {
                        { "avg_reaction_time_ms", MetricToJson(Metrics.AvgReactionTimeMs) },
                        { "avg_task_duration_ms", MetricToJson(Metrics.AvgTaskDurationMs) },
                        { "time_per_success_s", MetricToJson(Metrics.TimePerSuccessS) },
                        { "navigation_errors", MetricToJson(Metrics.NavigationErrors) }
                    } 
                },
                { "satisfaccion", new JObject 
                    {
                        { "learning_stability", MetricToJson(Metrics.LearningStability) },
                        { "error_reduction_rate", MetricToJson(Metrics.ErrorReductionRate) },
                        { "voluntary_play_time_s", MetricToJson(Metrics.VoluntaryPlayTimeS) },
                        { "aid_usage", MetricToJson(Metrics.AidUsage) },
                        { "interface_errors", MetricToJson(Metrics.InterfaceErrors) }
                    } 
                },
                { "presencia", new JObject 
                    {
                        { "activity_level_per_min", MetricToJson(Metrics.ActivityLevelPerMin) },
                        { "first_success_time_s", MetricToJson(Metrics.FirstSuccessTimeS) },
                        { "inactivity_time_s", MetricToJson(Metrics.InactivityTimeS) },
                        { "sound_localization_time_s", MetricToJson(Metrics.SoundLocalizationTimeS) },
                        { "audio_performance_gain", MetricToJson(Metrics.AudioPerformanceGain) }
                    } 
                }
            };
            
            // Event Roles (Critical for Python Analysis)
            jsonConfig["event_roles"] = new JObject
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

            // Custom Events
            jsonConfig["custom_events"] = new JObject {
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
    }
}
