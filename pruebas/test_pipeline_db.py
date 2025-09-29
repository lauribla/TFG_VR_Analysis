import pandas as pd
from python_analysis.log_parser import LogParser
from python_analysis.metrics import MetricsCalculator
from python_analysis.exporter import MetricsExporter
from python_visualization.visualize_groups import Visualizer
from python_visualization.pdf_reporter import PDFReport
from pymongo import MongoClient
from datetime import datetime
import os
import json
from pathlib import Path

# ============================================================
# 1. Insertar logs de prueba en MongoDB
# ============================================================
client = MongoClient("mongodb://localhost:27017")
db = client["test"]          # usa "vr_experiment" si quieres base real
collection = db["tfg"]

# Limpiar colección para la prueba
collection.delete_many({})

fake_logs = [
    {"timestamp": datetime.utcnow(), "user_id": "U001", "event_type": "task",
     "event_name": "target_hit", "event_value": 1,
     "event_context": {"session_id": "S1", "group_id": "control", "target_id": "TGT_01", "reaction_time_ms": 450}},
    {"timestamp": datetime.utcnow(), "user_id": "U001", "event_type": "task",
     "event_name": "target_miss", "event_value": 0,
     "event_context": {"session_id": "S1", "group_id": "control", "target_id": "TGT_02", "reaction_time_ms": 700}},
    {"timestamp": datetime.utcnow(), "user_id": "U001", "event_type": "task",
     "event_name": "task_end", "event_value": "success",
     "event_context": {"session_id": "S1", "group_id": "control", "duration_ms": 10000}},
    {"timestamp": datetime.utcnow(), "user_id": "U001", "event_type": "custom",
     "event_name": "hand_gesture", "event_value": None,
     "event_context": {"session_id": "S1", "group_id": "control", "gesture_id": 3}}
]

collection.insert_many(fake_logs)
print("✅ Logs de prueba insertados en MongoDB")

# ============================================================
# 2. Extraer logs con LogParser
# ============================================================
parser = LogParser(db_name="test", collection_name="tfg")
logs = parser.fetch_logs()
df = parser.parse_logs(logs, expand_context=True)
parser.close()

print("\n=== DataFrame cargado desde Mongo ===")
print(df.head())

# ============================================================
# 3. Calcular métricas
# ============================================================
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados calculados ===")
print(json.dumps(results, indent=4))

# ============================================================
# 4. Exportar resultados
# ============================================================
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

# Directorios locales dentro de la carpeta actual ("pruebas/")
base_dir = Path(__file__).parent
export_dir = base_dir / f"exports_{timestamp}"
figures_dir = base_dir / f"figures_{timestamp}"

os.makedirs(export_dir, exist_ok=True)
os.makedirs(figures_dir, exist_ok=True)

exporter = MetricsExporter(results, output_dir=export_dir / "U001")
exporter.to_json("results.json")
exporter.to_csv("results.csv")
MetricsExporter.export_multiple([results], ["U001"], mode="json", output_dir=export_dir, filename="group_results")

# ============================================================
# 5. Generar gráficos
# ============================================================
viz = Visualizer(str(export_dir / "group_results.json"), output_dir=figures_dir)
viz.generate_all()

# ============================================================
# 6. Generar informe PDF
# ============================================================
report = PDFReport(
    results_file=str(export_dir / "group_results.json"),
    figures_dir=figures_dir,
    output_file=str(export_dir / "final_report.pdf")
)
report.generate()

print(f"\n✅ Pipeline completo con BD listo: revisa {export_dir}/ y {figures_dir}/")
