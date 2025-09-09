from pymongo import MongoClient

# Conexión a Mongo
client = MongoClient("mongodb://localhost:27017/")
db = client["test"]
collection = db["tfg"]

# Leer los últimos 5 documentos
print("📊 Últimos documentos en la colección 'tfg':")
for doc in collection.find().sort("timestamp", -1).limit(5):
    print(doc)
