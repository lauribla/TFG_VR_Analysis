from pymongo import MongoClient

client = MongoClient("mongodb://localhost:27017/")
db = client["vr_experiment"]
collection = db["events"]

collection.insert_one({
    "user_id": "test_user",
    "event_type": "test",
    "message": "MongoDB está funcionando en Windows"
})

print("¡Evento insertado correctamente!")
