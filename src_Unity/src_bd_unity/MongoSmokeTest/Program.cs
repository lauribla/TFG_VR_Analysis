using MongoDB.Bson;
using MongoDB.Driver;

//esto es lo que se hace "run" e inserta algo en la bd, es el "main" de la solution
class Program
{
    static void Main()
    {
        var client = new MongoClient("mongodb://localhost:27017/");
        var db = client.GetDatabase("test");
        var col = db.GetCollection<BsonDocument>("tfg");

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