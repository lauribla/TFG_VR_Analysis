using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRLogger
{
    // Custom Converters para evitar StackOverflow con Vector3 y Quaternion al serializar
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WriteEndObject();
        }
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WritePropertyName("w"); writer.WriteValue(value.w);
            writer.WriteEndObject();
        }
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
    }

    public static class LoggerService
    {
        private static IMongoCollection<BsonDocument> _collection;
        private static string _userId = "UNKNOWN";
        private static bool _initialized = false;

        // 🔹 Propiedad para comprobar si está inicializado
        public static bool IsInitialized => _initialized;

        // ==============================================================
        // 🔸 INIT
        // ==============================================================
        public static void Init(string connectionString, string dbName, string collectionName, string userId)
        {
            try
            {
                UnityEngine.Debug.Log($"[LoggerService] 🟡 Intentando conectar a MongoDB en: {connectionString}");

                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(dbName);
                _collection = database.GetCollection<BsonDocument>(collectionName);

                _userId = userId;
                _initialized = true;

                UnityEngine.Debug.Log($"[LoggerService] ✅ Conectado a MongoDB → DB: {dbName}, Collection: {collectionName}, User: {_userId}");
            }
            catch (Exception ex)
            {
                _initialized = false;
                UnityEngine.Debug.LogError($"[LoggerService] ❌ Error de conexión a MongoDB: {ex.Message}");
            }
        }

        // ==============================================================
        // 🔸 LOG EVENT
        // ==============================================================
        public static async Task LogEvent(
        string eventType,
        string eventName,
        object eventValue = null,
        object eventContext = null,
        bool save = true)
    {
        // GLOBAL PAUSE BLOCK: Prevent any logging if experiment is paused
        if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsPaused)
        {
            return;
        }

        if (!_initialized)
        {
            // Silently ignore or just warning if called before Init.
            // Most likely it's just startup noise.
            // UnityEngine.Debug.LogWarning("[LoggerService] ⚠️ LogEvent called before Init. Ignoring.");
            return;
        }

    if (_collection == null)
    {
        UnityEngine.Debug.LogError("[LoggerService] ❌ Colección nula. Revisa el nombre de la base o colección.");
        return;
    }

    // -------------------------
    // Construir documento BSON
    // -------------------------
    var logDoc = new BsonDocument
{
    { "timestamp", DateTime.UtcNow },
    { "user_id", _userId },
    { "event_type", eventType ?? "undefined" },
    { "event_name", eventName ?? "undefined" },
    { "save", save }
};

if (eventValue != null)
{
    try
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter> { new Vector3Converter(), new QuaternionConverter() }
        };
        var valJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventValue, settings);
        var valBson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonValue>(valJson);
        logDoc.Add("event_value", valBson);
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogWarning($"[LoggerService] ⚠️ No se pudo serializar event_value: {ex.Message}");
        logDoc.Add("event_value", eventValue.ToString());
    }
}
else
{
    logDoc.Add("event_value", BsonNull.Value);
}

    // Añadir sesión y grupo automáticamente si hay UserSessionManager activo
    if (UserSessionManager.Instance != null)
    {
        logDoc.Add("session_id", UserSessionManager.Instance.GetSessionId());
        logDoc.Add("group_id", UserSessionManager.Instance.GetGroupId());
    }

    // Añadir contexto (si hay)
    if (eventContext != null)
    {
        try
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Vector3Converter(), new QuaternionConverter() }
            };
            var contextJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventContext, settings);
            var contextBson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(contextJson);
            logDoc.Add("event_context", contextBson);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[LoggerService] ⚠️ No se pudo serializar event_context: {ex.Message}");
        }
    }

    // -------------------------
    // Log de depuración
    // -------------------------
    UnityEngine.Debug.Log($"[LoggerService] 📨 Insertando documento en MongoDB...");
    UnityEngine.Debug.Log($"[LoggerService] Documento JSON → {logDoc.ToJson()}");

    // -------------------------
    // Inserción en MongoDB
    // -------------------------
    try
    {
        if (save)
        {
            await _collection.InsertOneAsync(logDoc);
            UnityEngine.Debug.Log($"[LoggerService] ✅ Documento insertado correctamente en MongoDB ({eventName})");
        }
        else
        {
            UnityEngine.Debug.Log($"[LoggerService] 💾 Simulación: no se guardó (save=false)");
        }
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogError($"[LoggerService] ❌ Error al insertar documento: {ex.Message}");
    }
}


    }
}
