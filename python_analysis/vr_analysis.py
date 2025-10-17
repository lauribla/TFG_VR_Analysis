"""
VR USER EVALUATION - Análisis completo de la base de datos MongoDB
------------------------------------------------------------------
Este script conecta con la base de datos donde Unity guarda los logs (test.tfg),
los analiza y genera automáticamente:
    - Métricas de usuario y grupo (eficiencia, efectividad, satisfacción)
    - Archivos CSV/JSON exportados
    - Gráficas comparativas
    - Informe PDF final con resultados
"""

import pandas as pd
from python_analysis.log_parser import LogParser
from python_analysis.metrics import MetricsCalculator
from python_analysis.exporter import MetricsExporter
from python_visualization.visualize_groups import Visualizer
from python_visualization.pdf_reporter import PDFReport
from datetime import datetime
import os
import json
from pathlib import Path

# ============================================================
# 1️⃣ Conectar con tu base de datos real
# ============================================================
DB_NAME = "test"          # Cambia a "vr_experiment" si usas la base definitiva
COLLECTION_NAME = "tfg"   # Debe coincidir con la colección usada por Unity
MONGO_URI = "mongodb://localhost:27017"

# Crear parser y leer los logs desde MongoDB
print(f"🔗 Conectando a MongoDB → {MONGO_URI}/{DB_NAME}.{COLLECTION_NAME}")
parser = LogParser(db_name=DB_NAME, collection_name=COLLECTION_NAME)
logs = parser.fetch_logs()
df = parser.parse_logs(logs, expand_context=True)
parser.close()

if df.empty:
    print("⚠️  No se encontraron logs en la base de datos. Asegúrate de que Unity ha enviado datos.")
    exit()

print(f"✅ {len(df)} documentos cargados correctamente desde MongoDB.\n")

# ============================================================
# 2️⃣ Resumen inicial de sesiones y usuarios
# ============================================================
print("👥 Resumen de usuarios y sesiones detectadas:\n")

usuarios = df["user_id"].nunique()
grupos = df["group_id"].nunique()
sesiones = df["session_id"].nunique()

print(f"  • Usuarios únicos: {usuarios}")
print(f"  • Grupos experimentales: {grupos}")
print(f"  • Sesiones registradas: {sesiones}\n")

print("📄 Detalle de sesiones:")
print(df[["user_id", "group_id", "session_id"]].drop_duplicates().to_string(index=False))

# ============================================================
# 3️⃣ Calcular métricas de usuario y grupo
# ============================================================
print("\n📊 Calculando métricas (eficiencia, efectividad, satisfacción)...")
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados globales ===")
print(json.dumps(results, indent=4))

# ============================================================
# 4️⃣ Crear estructura de carpetas con timestamp
# ============================================================
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

base_dir = Path(__file__).parent
export_dir = base_dir / f"pruebas/exports_{timestamp}"
figures_dir = base_dir / f"pruebas/figures_{timestamp}"

os.makedirs(export_dir, exist_ok=True)
os.makedirs(figures_dir, exist_ok=True)

print(f"\n📁 Directorios creados:")
print(f"  - Exportaciones: {export_dir}")
print(f"  - Figuras: {figures_dir}")

# ============================================================
# 5️⃣ Exportar resultados a JSON y CSV
# ============================================================
print("\n💾 Exportando resultados...")

# Exportador global
exporter = MetricsExporter(results, output_dir=export_dir)
exporter.to_json("results.json")
exporter.to_csv("results.csv")

# Exportar métricas agrupadas por usuario y sesión
grouped_df = metrics.compute_grouped_metrics()
grouped_path = export_dir / "grouped_metrics.csv"
grouped_df.to_csv(grouped_path, index=False)

MetricsExporter.export_multiple(
    [results],
    ["Global"],
    mode="json",
    output_dir=export_dir,
    filename="group_results"
)

print(f"✅ Resultados exportados correctamente en {export_dir}")
print(f"✅ Resultados detallados por usuario/sesión guardados en {grouped_path}")

# ============================================================
# 6️⃣ Generar visualizaciones
# ============================================================
print("\n📈 Generando gráficas...")
viz = Visualizer(str(export_dir / "group_results.json"), output_dir=figures_dir)
viz.generate_all()
print(f"✅ Figuras generadas en {figures_dir}")

# ============================================================
# 7️⃣ Generar informe PDF final
# ============================================================
print("\n📄 Creando informe PDF final...")
report = PDFReport(
    results_file=str(export_dir / "group_results.json"),
    figures_dir=figures_dir
)
report.generate()
print(f"✅ Informe PDF generado en {report.output_file}")

print("\n🎉 Análisis completo terminado con éxito. Revisa las carpetas generadas.")
