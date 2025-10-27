using UnityEngine;
using System;
using System.Threading.Tasks;

namespace VRLogger
{
    public class UserSessionManager : MonoBehaviour
    {
        // ✅ Patrón Singleton
        public static UserSessionManager Instance;

        [Header("Mongo Config")]
        public string connectionString = "mongodb://localhost:27017";
        public string dbName = "test";
        public string collectionName = "tfg";

        [Header("User Config")]
        public string userId = "U001";      // Se puede asignar dinámicamente
        public string groupId = "control";  // Ej: "control", "experimental_eyeTracking"

        [Header("User Profile")]
        public string userName;
        public int age;
        public string gender;
        public string handedness;          // left / right
        public string experienceLevel;     // novice / intermediate / expert
        public string cognitiveProfile;    // Diagnóstico cognitivo o etiqueta

        private string sessionId;

        void Awake()
        {
            // ✅ Establecer instancia global
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            // ✅ Generar session_id único
            sessionId = Guid.NewGuid().ToString();

            // ✅ Inicializar logger
            LoggerService.Init(connectionString, dbName, collectionName, userId);

            // ✅ Registrar inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId);

            // ✅ Registrar metadatos del usuario
            _ = LoggerService.LogEvent(
                "session",
                "session_metadata",
                null,
                new
                {
                    session_id = sessionId,
                    user_id = userId,
                    group_id = groupId,
                    profile = new
                    {
                        name = userName,
                        age = age,
                        gender = gender,
                        handedness = handedness,
                        experience = experienceLevel,
                        cognitive_profile = cognitiveProfile
                    }
                }
            );

            Debug.Log($"[UserSessionManager] Session started: {sessionId} (User {userId}, Group {groupId})");
        }

        void OnApplicationQuit()
        {
            // ✅ Log de fin de sesión
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
