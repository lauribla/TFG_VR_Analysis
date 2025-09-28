import pandas as pd
from python_analysis.metrics import MetricsCalculator
from python_analysis.exporter import MetricsExporter
from python_visualization.visualize_groups import Visualizer
from python_visualization.pdf_reporter import PDFReport
import json
import os
from datetime import datetime

# ============================================================
# Crear un timestamp único para cada ejecución
# ============================================================
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
base_export_dir = f"exports_{timestamp}"
base_figures_dir = f"figures_{timestamp}"

os.makedirs(base_export_dir, exist_ok=True)
os.makedirs(base_figures_dir, exist_ok=True)

# ============================================================
# 1. Crear logs de prueba (simulados)
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
    {"timestamp": "2025-09-26T14:40:12Z", "user_id": "U001", "event_type": "custom",
     "event_name": "hand_gesture", "event_value": None,
     "event_context": {"session_id": "S1", "group_id": "control", "gesture_id": 3}}
]

df = pd.DataFrame(fake_logs)
df["timestamp"] = pd.to_datetime(df["timestamp"])

# ============================================================
# 2. Calcular métricas
# ============================================================
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados calculados ===")
print(json.dumps(results, indent=4))

# ============================================================
# 3. Exportar resultados
# ============================================================
exporter = MetricsExporter(results, output_dir=f"{base_export_dir}/U001")
exporter.to_json("results.json")
exporter.to_csv("results.csv")

MetricsExporter.export_multiple([results], ["U001"], mode="json", output_dir=base_export_dir, filename="group_results")

# ============================================================
# 4. Generar gráficos
# ============================================================
viz = Visualizer(f"{base_export_dir}/group_results.json", output_dir=base_figures_dir)
viz.generate_all()

# ============================================================
# 5. Generar PDF final
# ============================================================
report = PDFReport(
    results_file=f"{base_export_dir}/group_results.json",
    figures_dir=base_figures_dir,
    output_file=f"{base_export_dir}/final_report.pdf"
)
report.generate()

print(f"\n✅ Pipeline de prueba completado: revisa {base_export_dir}/ y {base_figures_dir}/")
