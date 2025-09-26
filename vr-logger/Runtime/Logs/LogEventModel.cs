using System;
using System.Collections.Generic;

namespace VRLogger
{
    [Serializable]
    public class LogEventModel
    {
        public DateTime timestamp { get; set; }
        public string user_id { get; set; }
        public string event_type { get; set; }   // task, navigation, interaction, system...
        public string event_name { get; set; }   // e.g. "target_hit"
        public object event_value { get; set; }  // número o string
        public Dictionary<string, object> event_context { get; set; }
        public bool save { get; set; } = true;   // opcional para descartar logs de prueba
    }
}