using MongoDB.Bson;
using System;

class Program
{
    static void Main(string[] args)
    {
        Logger.Init();

        var data = new BsonDocument {
            { "task_id", "T1" },
            { "result", "success" }
        };

        Logger.LogEvent("user001", "task_result", data);

        Console.WriteLine("✔️ Evento insertado correctamente");
    }
}
