using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VRLogger;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace VRLogger.UI
{
    public class ConfigUI : MonoBehaviour
    {
        // Static flag to indicate config has been accepted (used by TimerUILoader)
        public static bool ConfigAccepted { get; private set; } = false;

        [Header("UI References")]
        public Button startButton;

        [Header("Game References")]
        // Use interface for project-agnostic camera control
        private ICameraController cameraController;

        // Diccionarios para guardar referencias a los inputs generados dinamicamente
        // Clave: "path/al/dato" (ej: "session.session_name")
        private Dictionary<string, TMP_InputField> _stringInputs = new Dictionary<string, TMP_InputField>();
        private Dictionary<string, Toggle> _boolInputs = new Dictionary<string, Toggle>();

        private JObject _currentConfig;

        void Start()
        {
            // PAUSA EL JUEGO AL INICIO
            Time.timeScale = 0f;
            ConfigAccepted = false; // Reset for new session

            _currentConfig = ExperimentConfig.Instance.GetConfig();
            
            // Setup botón start
            if (startButton)
            {
                startButton.onClick.AddListener(OnStartPressed);
            }

            FindCameraController();
        }

        private void FindCameraController()
        {
            // Buscar cualquier MonoBehaviour que implemente ICameraController
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb is ICameraController icc)
                {
                    cameraController = icc;
                    break;
                }
            }
        }

        void Update()
        {
            // WHILE CONFIG IS OPEN: Enforce cursor visibility and unlock
            // This prevents other scripts in a new project from locking the cursor in their own Update()
            if (!ConfigAccepted)
            {
                if (cameraController != null)
                {
                    cameraController.DisableControl();
                }
                
                // Extra safeguard: force cursor state regardless of controller
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
                
                if (!Cursor.visible)
                    Cursor.visible = true;
            }
        }

        // Métodos para registrar inputs desde el Loader
        public void RegisterInput(string jsonPath, TMP_InputField input)
        {
            if (!_stringInputs.ContainsKey(jsonPath))
            {
                _stringInputs.Add(jsonPath, input);
            }
        }

        public void RegisterToggle(string jsonPath, Toggle toggle)
        {
            if (!_boolInputs.ContainsKey(jsonPath))
            {
                _boolInputs.Add(jsonPath, toggle);
            }
        }

        public void OnStartPressed()
        {
            Debug.Log("[ConfigUI] Botón Start presionado.");
            ConfigAccepted = true;

            if (_currentConfig != null)
            {
                // 1. Guardar Inputs de Texto/Numeros
                foreach (var kvp in _stringInputs)
                {
                    UpdateJsonValue(kvp.Key, kvp.Value.text);
                }

                // 2. Guardar Toggles
                foreach (var kvp in _boolInputs)
                {
                    // Logic for Virtual Toggle "use_timer"
                    if (kvp.Key == "participant_flow.use_timer")
                    {
                        bool isTimer = kvp.Value.isOn;
                        UpdateJsonValue("participant_flow.end_condition", isTimer ? "timer" : "gm");
                        continue; // Do not save "use_timer" to potential disk JSON (though it's in memory)
                    }

                    UpdateJsonValue(kvp.Key, kvp.Value.isOn);
                }

                // 3. Obtener User ID y Group ID para la sesión (helpers)
                string userId = GetValueFromPath("session.user_id") ?? GetValueFromPath("participants.order[0]") ?? "UnknownUser";
                string groupId = GetValueFromPath("session.group_name") ?? "DefaultGroup";

                // 4a. RESTART EXPERIMENT with new settings (Timer, participants, etc.)
                if (ParticipantFlowController.Instance != null)
                {
                    ParticipantFlowController.Instance.RestartExperiment();
                }

                // 4b. Iniciar sesión (handled by RestartExperiment now, but keep for safety)
                // UserSessionManager.Instance.StartSessionForUser(userId, groupId);
            }

            // 5. RESUMIR EL JUEGO
            Time.timeScale = 1f;

            // 6. Activar la cámara
            if (cameraController != null)
            {
                cameraController.EnableControl();
            }
            else
            {
                // Fallback: Default Unity FPS behavior when starting experiment
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Ensure GM view is active after start
            var gmHud = FindObjectOfType<GMHUDLoader>();
            if (gmHud != null)
            {
                gmHud.ActivateGMView();
            }

            // 7. Ocultar UI
            gameObject.SetActive(false);
        }

        // Helper para actualizar el JObject usando un path tipo "section.subkey"
        private void UpdateJsonValue(string path, object value)
        {
            string[] parts = path.Split('.');
            JToken current = _currentConfig;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current[parts[i]] == null) return; // Path no existe
                current = current[parts[i]];
            }

            string key = parts[parts.Length - 1];
            if (current is JObject obj)
            {
                // Safety check: if key doesn't exist, we can't check its Type.
                // Depending on logic, we might want to create it, but for strict config mapping, we ignore or default to string.
                if (obj[key] == null)
                {
                     // If it's a new key, just set it directly
                     obj[key] = value is bool ? (bool)value : value.ToString();
                     return;
                }

                // Detectar si era int o bool original para mantener tipo?
                if (value is bool b)
                {
                     obj[key] = b;
                }
                else
                {
                    // Intentar mantener números si el original era número
                    if (obj[key].Type == JTokenType.Integer && int.TryParse(value.ToString(), out int res))
                    {
                        obj[key] = res;
                    }
                    else if (obj[key].Type == JTokenType.Float && float.TryParse(value.ToString(), out float resF))
                    {
                        obj[key] = resF;
                    }
                    else
                    {
                        obj[key] = value.ToString();
                    }
                }
            }
        }

        private string GetValueFromPath(string path)
        {
             if (_stringInputs.ContainsKey(path)) return _stringInputs[path].text;
             return null;
        }
    }
}
