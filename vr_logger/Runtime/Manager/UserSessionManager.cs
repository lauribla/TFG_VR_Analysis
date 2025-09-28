using UnityEngine;
using System;
using System.Threading.Tasks;

namespace VRLogger
{
    public class UserSessionManager : MonoBehaviour
    {
        [Header("Mongo Config")]
        public string connectionString = "mongodb://localhost:27017";
        public string dbName = "VRLogsDB";
        public string collectionName = "events";

        [Header("User Config")]
        public string userId = "U001";      // Se puede asignar dinámicamente
        public string groupId = "control";  // Ej: "control", "experimental_eyeTracking"

        private string sessionId;

        void Start()
        {
            // Generar session_id único
            sessionId = Guid.NewGuid().ToString();

            // Inicializar logger
            LoggerService.Init(connectionString, dbName, collectionName, userId);

            // Log de inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId);
            Debug.Log($"[UserSessionManager] Session started: {sessionId} (User {userId}, Group {groupId})");
        }

        void OnApplicationQuit()
        {
            // Log de fin de sesión
            _ = LogAPI.LogSessionEnd(sessionId);
            Debug.Log($"[UserSessionManager] Session ended: {sessionId}");
        }

        // -----------------------
        // Acceso a metadatos
        // -----------------------
        public string GetUserId() => userId;
        public string GetSessionId() => sessionId;
        public string GetGroupId() => groupId;

        // -----------------------
        // Helper: log custom con session y grupo
        // -----------------------
        public async Task LogEventWithSession(string eventType, string eventName, object eventValue = null, object eventContext = null)
        {
            await LoggerService.LogEvent(
                eventType,
                eventName,
                eventValue,
                new
                {
                    session_id = sessionId,
                    group_id = groupId,
                    context = eventContext
                }
            );
        }
    }
}
