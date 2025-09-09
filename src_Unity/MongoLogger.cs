using MongoDB.Bson;
using MongoDB.Driver;
using System;

public static class Logger
{
    private static IMongoCollection<BsonDocument> collection;

    public static void Init()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("vr_experiment");
        collection = database.GetCollection<BsonDocument>("events");
        Console.WriteLine("✔️ Conectado a MongoDB");
    }

    public static void LogEvent(string userId, string eventType, BsonDocument data)
    {
        var doc = new BsonDocument
        {
            { "timestamp", DateTime.UtcNow },
            { "user_id", userId },
            { "event_type", eventType },
            { "data", data }
        };

        collection.InsertOne(doc);
        Console.WriteLine($"📤 Evento insertado: {eventType}");
    }
}
