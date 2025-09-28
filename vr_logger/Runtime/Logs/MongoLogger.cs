using MongoDB.Bson;
using MongoDB.Driver;
using System;

public static class Logger
{
//esto será lo que llame unity para insertar en la tabla "test" (cambiar esta a la oficial después)
    private static IMongoCollection<BsonDocument> collection;

    public static void Init()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("test");
        collection = database.GetCollection<BsonDocument>("tfg");
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
