using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VRLogger
{
    public class UserSessionManager : MonoBehaviour
    {
        public static UserSessionManager Instance;

        [Header("Mongo Config")]
        public string connectionString = "mongodb://localhost:27017";
        public string dbName = "test";
        public string collectionName = "tfg";

        private string userId = "NO_USER";
        private string groupId = "NO_GROUP";
        private string sessionId;

        private bool started = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.parent = null; // Ensure we are root for DontDestroyOnLoad
                DontDestroyOnLoad(gameObject); // Ensure persistence across scenes
                this.enabled = true; // Ensure script is enabled so Update() runs

                // -----------------------------------------------------------
                // AUTO-INSTANTIATION (Bootstrapper) - SAFE LOOKUP
                // -----------------------------------------------------------
                if (UnityEngine.Object.FindFirstObjectByType<ExperimentConfig>() == null)
                {
                    Debug.Log("[UserSessionManager] 🛠️ Auto-Creating ExperimentConfig...");
                    new GameObject("ExperimentConfig").AddComponent<ExperimentConfig>();
                }
                else if (ExperimentConfig.Instance == null)
                {
                    ExperimentConfig.Instance = UnityEngine.Object.FindFirstObjectByType<ExperimentConfig>();
                }

                if (UnityEngine.Object.FindFirstObjectByType<VRTrackingManager>() == null)
                {
                    Debug.Log("[UserSessionManager] 🛠️ Auto-Creating VRTrackingManager...");
                    new GameObject("VRTrackingManager").AddComponent<VRTrackingManager>();
                }
                else if (VRTrackingManager.Instance == null)
                {
                    VRTrackingManager.Instance = UnityEngine.Object.FindFirstObjectByType<VRTrackingManager>();
                }

                if (UnityEngine.Object.FindFirstObjectByType<ParticipantFlowController>() == null)
                {
                    Debug.Log("[UserSessionManager] 🛠️ Auto-Creating ParticipantFlowController...");
                    new GameObject("ParticipantFlowController").AddComponent<ParticipantFlowController>();
                }
                else if (ParticipantFlowController.Instance == null)
                {
                    ParticipantFlowController.Instance = UnityEngine.Object.FindFirstObjectByType<ParticipantFlowController>();
                }

                if (UnityEngine.Object.FindFirstObjectByType<GMConsoleInput>() == null)
                {
                     Debug.Log("[UserSessionManager] 🛠️ Auto-Creating GMConsoleInput...");
                     new GameObject("GMConsoleInput").AddComponent<GMConsoleInput>();
                }
                // -----------------------------------------------------------
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"[UserSessionManager] Destruyendo script duplicado en {gameObject.name}");
                Destroy(this);
                return;
            }
        }

        private bool hasRunStartup = false;

        void Update()
        {
            // Robust first-frame startup pattern to bypass any Unity Start() skipping issues
            if (!hasRunStartup)
            {
                hasRunStartup = true;
                RunStartupFlow();
            }
        }

        private void RunStartupFlow()
        {
            Debug.Log("[UserSessionManager] 🚀 Auto-Starting Experiment...");

            // ---------------------------------------------------------------
            // MANUAL UI INSTANTIATION (Per Scene)
            // ---------------------------------------------------------------
            JObject cfg = ExperimentConfig.Instance.GetConfig();
            if (cfg != null)
            {
                // GM HUD (Visual UI Removed by request, only Input remains via GMConsoleInput)
                bool gmEnabled = (bool?)cfg["participant_flow"]?["gm_controls"]?["enabled"] ?? false;
                if (gmEnabled)
                {
                    Debug.Log("[UserSessionManager] GM Controls Enabled (Input Only)");
                }

                // TIMER UI (Visual Removed by request, logic in ParticipantFlowController)
                string endCond = (string)cfg["participant_flow"]?["end_condition"];
                if (endCond == "timer")
                {
                    Debug.Log("[UserSessionManager] Timer Condition Active (Logic Only)");
                }
            }
            // ---------------------------------------------------------------

            if (ParticipantFlowController.Instance != null)
            {
                ParticipantFlowController.Instance.RestartExperiment();
            }
        }

        // -------------------------------------------------------------------
        // NUEVO: Iniciar sesión para un usuario dinámico
        // -------------------------------------------------------------------
        public void StartSessionForUser(string newUserId, string newGroupId)
        {
            if (started) return;

            userId = newUserId;
            groupId = newGroupId;
            sessionId = Guid.NewGuid().ToString();

            // 1️⃣ Inicializar conexión Mongo
            LoggerService.Init(connectionString, dbName, collectionName, userId);

            // 2️⃣ Enviar CONFIG REAL (ahora sí está cargado y LoggerService está listo)
            ExperimentConfig.Instance.SendConfigAsLog();

            // 2b️⃣ Obtener Independent Variable (si existe)
            string iv = (string)ExperimentConfig.Instance.GetConfig()?["session"]?["independent_variable"];

            // 3️⃣ Registrar inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId, iv);

            started = true;
        }



        // -------------------------------------------------------------------
        // Fin de sesión
        // -------------------------------------------------------------------
        public void EndSession()
        {
            if (!started) return;

            _ = LogAPI.LogSessionEnd(sessionId);
            Debug.Log($"[UserSessionManager] 🔴 Sesión finalizada → {sessionId}");
            started = false;
        }

        // Getters
        public string GetUserId() => userId;
        public string GetGroupId() => groupId;
        public string GetSessionId() => sessionId;
    }
}
