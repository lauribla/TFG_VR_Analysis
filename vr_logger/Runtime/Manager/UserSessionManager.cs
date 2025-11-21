using UnityEngine;
using System;
using System.Threading.Tasks;

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
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
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

            // 3️⃣ Registrar inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId);

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
