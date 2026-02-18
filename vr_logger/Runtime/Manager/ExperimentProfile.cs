using UnityEngine;
using System.Collections.Generic;

namespace VRLogger
{
    [CreateAssetMenu(fileName = "NewExperimentProfile", menuName = "VRLogger/Experiment Profile")]
    public class ExperimentProfile : ScriptableObject
    {
        [Header("Session Configuration")]
        public string SessionName = "Dia_1";
        public string GroupName = "Grupo_A";
        public string IndependentVariable = "";
        public float TurnDurationSeconds = 60f;

        [Header("Participants")]
        public int ParticipantCount = 4;
        public List<string> ParticipantOrder = new List<string> { "U001", "U002", "U003", "U004" };
        public string ManualParticipantName = "";

        [Header("Experiment Info")]
        public string ExperimentId = "shooting_basic";
        public string FormulaProfile = "default";
        [TextArea] public string Description = "Basic shooting experiment";

        [Header("Modules")]
        public bool UseGazeTracker = true;
        public bool UseEyeTracker = false; // New
        public bool UseMovementTracker = true;
        public bool UseHandTracker = false;
        public bool UseFootTracker = false;
        public bool UseRaycastLogger = true;
        public bool UseCollisionLogger = true;

        [Header("GM Controls")]
        public bool EnableGMControls = true;

        [Header("Flow")]
        public ExperimentConfig.FlowModeType FlowMode = ExperimentConfig.FlowModeType.Turns;
        public ExperimentConfig.EndConditionType EndCondition = ExperimentConfig.EndConditionType.Timer;

        [Header("Metrics")]
        public ExperimentConfig.MetricsCategory Metrics = new ExperimentConfig.MetricsCategory
        {
            // Default Values matching original JSON
            HitRatio = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.35f, Min = 0, Max = 1, Invert = false },
            SuccessRate = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.30f, Min = 0, Max = 1, Invert = false },
            LearningCurveMean = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.15f, Min = 0, Max = 1, Invert = false },
            Progression = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 10, Invert = false },
            SuccessAfterRestart = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 1, Invert = false },

            AvgReactionTimeMs = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.40f, Min = 100, Max = 5000, Invert = true },
            AvgTaskDurationMs = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.30f, Min = 1000, Max = 30000, Invert = true },
            TimePerSuccessS = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.20f, Min = 0, Max = 60, Invert = true },
            NavigationErrors = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 10, Invert = true },
            
            LearningStability = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.30f, Min = 0, Max = 1, Invert = false },
            ErrorReductionRate = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 1, Invert = false },
            VoluntaryPlayTimeS = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 60, Invert = false },
            AidUsage = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 5, Invert = true },
            InterfaceErrors = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.10f, Min = 0, Max = 5, Invert = true },

            ActivityLevelPerMin = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 100, Invert = false },
            FirstSuccessTimeS = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.25f, Min = 0, Max = 30, Invert = true },
            InactivityTimeS = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.20f, Min = 0, Max = 60, Invert = true },
            SoundLocalizationTimeS = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.15f, Min = 0, Max = 10, Invert = true },
            AudioPerformanceGain = new ExperimentConfig.MetricConfig { Enabled = true, Weight = 0.15f, Min = -1, Max = 1, Invert = false },
        };

        [Header("Event Roles")]
        public List<ExperimentConfig.EventRoleMapping> CustomEventRoles = new List<ExperimentConfig.EventRoleMapping>();
    }
}
