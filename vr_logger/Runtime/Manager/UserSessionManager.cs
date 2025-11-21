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

        [Header("User Config")]
        public string userId = "U001";
        public string groupId = "default"; 

        [Header("User Profile")]
        public string userName;
        public int age;
        public string gender;
        public string handedness;
        public string experienceLevel;
        public string cognitiveProfile;

        private string sessionId;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            // --- LEER CONFIG DESDE EXPERIMENT CONFIG ---
            var cfg = ExperimentConfig.Instance.config;

            // Session y grupo vienen del JSON
            groupId = cfg.session.group_name;

            // sessionId único para este sujeto
            sessionId = Guid.NewGuid().ToString();

            // Inicializa Mongo
            LoggerService.Init(connectionString, dbName, collectionName, userId);

            // Registrar inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId);

            // Log de metadatos del usuario
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
            _ = LogAPI.LogSessionEnd(sessionId);
            Debug.Log($"[UserSessionManager] Session ended: {sessionId}");
        }

        public string GetUserId() => userId;
        public string GetSessionId() => sessionId;
        public string GetGroupId() => groupId;

        public async Task LogEventWithSession(string t, string n, object v = null, object ctx = null)
        {
            await LoggerService.LogEvent(
                t, n, v,
                new { session_id = sessionId, group_id = groupId, context = ctx }
            );
        }
    }
}
