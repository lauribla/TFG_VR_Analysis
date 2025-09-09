from pymongo import MongoClient
from datetime import datetime

    client = MongoClient("mongodb://localhost:27017/")
db = client["vr_experiment"]
col = db["events"]

doc = {
    "user_id": "test_user_python",
    "event_type": "test",
    "message": "Python ha insertado en MongoDB",
    "timestamp": datetime.utcnow()
}

col.insert_one(doc)
print("✅ Documento insertado en MongoDB desde Python")

print("📊 Documentos actuales en la colección:")
for d in col.find().limit(5):
print(d)