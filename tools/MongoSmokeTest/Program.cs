using MongoDB.Bson;
using MongoDB.Driver;

class Program
{
    static void Main()
    {
        var client = new MongoClient("mongodb://localhost:27017/");
        var db = client.GetDatabase("admin");
        var col = db.GetCollection<BsonDocument>("events");

        var doc = new BsonDocument
        {
            { "user_id", "test_user_csharp" },
            { "event_type", "test" },
            { "message", "C# ha insertado en MongoDB" },
            { "timestamp", BsonDateTime.Create(System.DateTime.UtcNow) }
        };

        col.InsertOne(doc);
        Console.WriteLine("✅ Documento insertado en MongoDB desde C#");
    }
}