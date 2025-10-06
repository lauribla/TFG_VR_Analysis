# vr_analysis.py
from pymongo import MongoClient
import pandas as pd

# ===============================
# 🔧 CONFIGURACIÓN DE LA BASE DE DATOS
# ===============================
client = MongoClient("mongodb://localhost:27017/")
db = client["test"]            # Nombre de tu base
collection = db["tfg"]         # Nombre de la colección

# ===============================
# 📥 CARGAR LOS DOCUMENTOS
# ===============================
data = list(collection.find({}))  # Todos los documentos

if not data:
    print("⚠️  No se encontraron datos en la colección.")
else:
    print(f"✅  Se han cargado {len(data)} documentos.")
    print()

# ===============================
# 🧮 CONVERTIR A PANDAS DATAFRAME
# ===============================
df = pd.DataFrame(data)

# Mostrar las primeras filas
print(df.head())

# ===============================
# 📊 ANÁLISIS BÁSICO
# ===============================

# Número de eventos por tipo
print("\n📊 Eventos por tipo:")
print(df["event_type"].value_counts())

# Tiempo de reacción promedio (si existe)
if "event_value" in df.columns:
    mean_reaction = df[df["event_name"] == "reaction_time"]["event_value"].mean()
    print(f"\n⚡ Tiempo de reacción promedio: {mean_reaction:.3f} segundos")

# Usuarios registrados
print("\n👤 Usuarios registrados:")
print(df["user_id"].unique())

# ===============================
# 📤 EXPORTAR A CSV (opcional)
# ===============================
df.to_csv("vr_logs_export.csv", index=False)
print("\n💾 Exportado a vr_logs_export.csv")
