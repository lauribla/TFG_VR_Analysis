using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Collections.Generic;


namespace VRLogger
{
    public static class LoggerService
    {
        private static IMongoCollection<LogEventModel> _collection;
        private static string _userId = "UNKNOWN";

        // Inicializa la conexión a Mongo
        public static void Init(string connectionString, string dbName, string collectionName, string userId)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(dbName);
            _collection = database.GetCollection<LogEventModel>(collectionName);
            _userId = userId;
            UnityEngine.Debug.Log($"[LoggerService] Connected to MongoDB: {dbName}/{collectionName}");
        }

        // Función principal de log
        public static async Task LogEvent(string eventType, string eventName, object eventValue = null, object eventContext = null, bool save = true)
        {
            if (_collection == null)
            {
                UnityEngine.Debug.LogError("[LoggerService] Not initialized!");
                return;
            }

            var log = new LogEventModel
            {
                timestamp = DateTime.UtcNow,
                user_id = _userId,
                event_type = eventType,
                event_name = eventName,
                event_value = eventValue,
                event_context = eventContext != null
                    ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        Newtonsoft.Json.JsonConvert.SerializeObject(eventContext))
                    : null,
                save = save
            };

            try
            {
                if (save)
                {
                    await _collection.InsertOneAsync(log);
                }
                UnityEngine.Debug.Log($"[LoggerService] Logged event: {eventName}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LoggerService] Error logging event: {ex.Message}");
            }
        }
    }
}
