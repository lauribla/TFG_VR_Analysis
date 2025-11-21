using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace VRLogger
{
    [System.Serializable]
    public class ExperimentConfigData
    {
        public string version;
        public string description;
        public string author;
        public string last_updated;

        public SessionInfo session;
        public ParticipantInfo participants;
        public ExperimentSelection experiment_selection;
        public Modules modules;

        public EventRoles event_roles;
        public Metrics metrics;
        public CustomEvents custom_events;

        // ---- Estructuras antiguas (por compatibilidad) ----
        public ExperimentSettings experiment_settings;
        public TrackingSettings tracking_settings;

        // ============================================================
        //       SUBCLASES ORGANIZADAS
        // ============================================================

        [System.Serializable]
        public class SessionInfo
        {
            public string session_name;
            public string group_name;
        }

        [System.Serializable]
        public class ParticipantInfo
        {
            public int count;
            public string[] order;
        }

        [System.Serializable]
        public class ExperimentSelection
        {
            public string experiment_id;
            public string formula_profile;
            public string description;
        }

        [System.Serializable]
        public class Modules
        {
            public bool useGazeTracker;
            public bool useMovementTracker;
            public bool useHandTracker;
            public bool useFootTracker;
            public bool useRaycastLogger;
            public bool useCollisionLogger;
        }

        [System.Serializable]
        public class EventRoles
        {
            public SerializableDictionary<string, string> roles;
        }

        [System.Serializable]
        public class Metrics
        {
            public SerializableDictionary<string, SerializableDictionary<string, string>> definitions;
        }

        [System.Serializable]
        public class CustomEvents
        {
            public SerializableDictionary<string, SerializableDictionary<string, string>> events;
        }

        // ------------------------------------------------------------
        // Compatibilidad vieja (por si Unity la usa internamente)
        // ------------------------------------------------------------
        [System.Serializable]
        public class ExperimentSettings
        {
            public int target_count;
            public float target_speed;
            public int session_time_limit;
            public string difficulty;
        }

        [System.Serializable]
        public class TrackingSettings
        {
            public int gaze_sampling_ms;
            public int movement_sampling_ms;
        }
    }
}
