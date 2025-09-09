using MongoDB.Bson;
using MongoDB.Driver;
using UnityEngine;

public class MongoLogger : MonoBehaviour
{
    private static IMongoCollection<BsonDocument> collection;

    void Start()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("vr_experiment");
        collection = database.GetCollection<BsonDocument>("events");

        var doc = new BsonDocument {
            { "timestamp", System.DateTime.UtcNow },
            { "user_id", "unity_user" },
            { "event_type", "unity_test" }
        };

        collection.InsertOne(doc);
        Debug.Log("Evento enviado a MongoDB");
    }
}
