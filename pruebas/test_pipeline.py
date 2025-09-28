import pandas as pd
from log_parser import LogParser   # usa el tuyo
from metrics import MetricsCalculator
from exporter import MetricsExporter
from visualize_groups import Visualizer
from pdf_reporter import PDFReport
import json
import os

# ============================================================
# 1. Crear logs de prueba (simulados, sin Unity ni MongoDB)
# ============================================================
fake_logs = [
    {"timestamp": "2025-09-26T14:36:12Z", "user_id": "U001", "event_type": "task",
     "event_name": "target_hit", "event_value": 1,
     "event_context": {"session_id": "S1", "group_id": "control", "target_id": "TGT_01", "reaction_time_ms": 450}},
    {"timestamp": "2025-09-26T14:37:12Z", "user_id": "U001", "event_type": "task",
     "event_name": "target_miss", "event_value": 0,
     "event_context": {"session_id": "S1", "group_id": "control", "target_id": "TGT_02", "reaction_time_ms": 700}},
    {"timestamp": "2025-09-26T14:38:12Z", "user_id": "U001", "event_type": "task",
     "event_name": "task_end", "event_value": "success",
     "event_context": {"session_id": "S1", "group_id": "control", "duration_ms": 10000}},
    {"timestamp": "2025-09-26T14:39:12Z", "user_id": "U001", "event_type": "gaze",
     "event_name": "gaze_frame", "event_value": None,
     "event_context": {"session_id": "S1", "group_id": "control", "target": "Enemy_01"}},
    # Custom inventado
    {"timestamp": "2025-09-26T14:40:12Z", "user_id": "U001", "event_type": "custom",
     "event_name": "hand_gesture", "event_value": None,
     "event_context": {"session_id": "S1", "group_id": "control", "gesture_id": 3}}
]

# ============================================================
# 2. Parsear (simulamos lo que haría LogParser.parse_logs)
# ============================================================
df = pd.DataFrame(fake_logs)
df["timestamp"] = pd.to_datetime(df["timestamp"])

# ============================================================
# 3. Calcular métricas
# ============================================================
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados calculados ===")
print(json.dumps(results, indent=4))

# ============================================================
# 4. Exportar resultados (JSON y CSV)
# ============================================================
os.makedirs("exports", exist_ok=True)
exporter = MetricsExporter(results, output_dir="exports/U001")
exporter.to_json("results.json")
exporter.to_csv("results.csv")

# Para simular varios usuarios/grupos:
MetricsExporter.export_multiple([results], ["U001"], mode="json", filename="group_results")

# ============================================================
# 5. Generar gráficos
# ============================================================
viz = Visualizer("exports/group_results.json", output_dir="figures")
viz.generate_all()

# ============================================================
# 6. Generar PDF final
# ============================================================
report = PDFReport(
    results_file="exports/group_results.json",
    figures_dir="figures",
    output_file="exports/final_report.pdf"
)
report.generate()

print("\n✅ Pipeline de prueba completado: revisa exports/ y figures/")
