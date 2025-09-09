using MongoDB.Bson;
using System;

class TestLogger
{
    static void Main(string[] args)
    {
        Logger.Init();

        var testData = new BsonDocument
        {
            { "task_id", "test_task_001" },
            { "result", "success" },
            { "note", "Este evento fue insertado desde consola C#" }
        };

        Logger.LogEvent("user_demo", "task_result", testData);

        Console.WriteLine("✅ Fin de la prueba");
    }
}
