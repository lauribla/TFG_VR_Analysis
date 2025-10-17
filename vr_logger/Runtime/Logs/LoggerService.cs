using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace VRLogger
{
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
    if (!_initialized)
    {
        UnityEngine.Debug.LogError("[LoggerService] ⚠️ Not initialized! Llama primero a LoggerService.Init().");
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
        // Añadir event_value correctamente tipado
if (eventValue != null)
{
    if (eventValue is string s)
    {
        logDoc.Add("event_value", s);
    }
    else
    {
        try
        {
            // Si es un objeto o estructura, lo convertimos a BSON válido
            var valJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventValue);
            var valBson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonValue>(valJson);
            logDoc.Add("event_value", valBson);
        }
        catch
        {
            // En caso de error, lo guardamos como texto
            logDoc.Add("event_value", eventValue.ToString());
        }
    }
}
else
{
    // Si no hay valor, guardamos un nulo real
    logDoc.Add("event_value", BsonNull.Value);
}

        { "save", save }
    };

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
            var contextJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventContext);
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
