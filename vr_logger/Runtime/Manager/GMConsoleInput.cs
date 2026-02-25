using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

namespace VRLogger
{
    public class GMConsoleInput : MonoBehaviour
    {
        private bool enabledControls = true; // Default ON

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

            if (InputWrapper.GetKeyDown(keyPause))
            {
                Debug.Log($"[GMConsoleInput] ⌨️ Detectado KEY PAUSE vía Update: {keyPause}");
                ParticipantFlowController.Instance.TogglePause();
            }

            if (InputWrapper.GetKeyDown(keyEnd))
            {
                Debug.Log($"[GMConsoleInput] ⌨️ Detectado KEY END vía Update: {keyEnd}");
                ParticipantFlowController.Instance.GM_EndTurn();
            }

            if (InputWrapper.GetKeyDown(keyNext))
            {
                Debug.Log($"[GMConsoleInput] ⌨️ Detectado KEY NEXT vía Update: {keyNext}");
                ParticipantFlowController.Instance.GM_NextParticipant();
            }
        }

        // Método de seguridad: OnGUI puede capturar teclas a veces incluso cuando el sistema de Input
        // principal las ignora (ej. problemas de foco en el editor o conflictos de InputSystem)
        void OnGUI()
        {
            if (!enabledControls) return;
            if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                KeyCode pressed = Event.current.keyCode;
                if (pressed == KeyCode.None) return;

                if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsRunning())
                {
                    if (pressed == keyPause)
                    {
                        Debug.Log($"[GMConsoleInput] ⌨️ Detectado PAUSE vía OnGUI: {keyPause}");
                        ParticipantFlowController.Instance.TogglePause();
                        Event.current.Use();
                    }
                    else if (pressed == keyEnd)
                    {
                        Debug.Log($"[GMConsoleInput] ⌨️ Detectado END vía OnGUI: {keyEnd}");
                        ParticipantFlowController.Instance.GM_EndTurn();
                        Event.current.Use();
                    }
                    else if (pressed == keyNext)
                    {
                        Debug.Log($"[GMConsoleInput] ⌨️ Detectado NEXT vía OnGUI: {keyNext}");
                        ParticipantFlowController.Instance.GM_NextParticipant();
                        Event.current.Use();
                    }
                }
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
