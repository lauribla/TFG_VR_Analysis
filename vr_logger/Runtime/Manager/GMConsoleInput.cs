using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

namespace VRLogger
{
    public class GMConsoleInput : MonoBehaviour
    {
        private bool enabledControls = false;

        private KeyCode keyNext = KeyCode.N;
        private KeyCode keyEnd = KeyCode.E;
        private KeyCode keyPause = KeyCode.P;

        void Start()
        {
            JObject cfg = ExperimentConfig.Instance.GetConfig();
            if (cfg == null) return;

            JObject pf = (JObject)cfg["participant_flow"];
            if (pf == null) return;

            JObject gm = (JObject)pf["gm_controls"];
            if (gm == null) return;

            enabledControls = (bool?)gm["enabled"] ?? false;

            keyNext = ParseKey((string)gm["next_key"], KeyCode.N);
            keyEnd = ParseKey((string)gm["end_key"], KeyCode.E);
            keyPause = ParseKey((string)gm["pause_key"], KeyCode.P);

            Debug.Log("[GMConsoleInput] enabled=" + enabledControls
                + " next=" + keyNext + " end=" + keyEnd + " pause=" + keyPause);
        }

        void Update()
        {
            if (!enabledControls) return;
            if (ParticipantFlowController.Instance == null) return;
            if (!ParticipantFlowController.Instance.IsRunning()) return;

            if (Input.GetKeyDown(keyPause))
            {
                ParticipantFlowController.Instance.TogglePause();
            }

            if (Input.GetKeyDown(keyEnd))
            {
                ParticipantFlowController.Instance.GM_EndTurn();
            }

            if (Input.GetKeyDown(keyNext))
            {
                ParticipantFlowController.Instance.GM_NextParticipant();
            }
        }

        private KeyCode ParseKey(string s, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            try
            {
                return (KeyCode)Enum.Parse(typeof(KeyCode), s, true);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
