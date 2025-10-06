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
COLLECTION_NAME = "tfg"   # Debe coincidir con la colección de Unity
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

print(f"✅ {len(df)} documentos cargados correctamente desde MongoDB.")

# ============================================================
# 2️⃣ Calcular métricas de usuario y grupo
# ============================================================
print("\n📊 Calculando métricas (eficiencia, efectividad, satisfacción)...")
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados ===")
print(json.dumps(results, indent=4))

# ============================================================
# 3️⃣ Crear estructura de carpetas con timestamp
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
# 4️⃣ Exportar resultados a JSON y CSV
# ============================================================
print("\n💾 Exportando resultados...")
exporter = MetricsExporter(results, output_dir=export_dir)
exporter.to_json("results.json")
exporter.to_csv("results.csv")

MetricsExporter.export_multiple(
    [results],
    ["Global"],
    mode="json",
    output_dir=export_dir,
    filename="group_results"
)

print(f"✅ Resultados exportados correctamente en {export_dir}")

# ============================================================
# 5️⃣ Generar visualizaciones
# ============================================================
print("\n📈 Generando gráficas...")
viz = Visualizer(str(export_dir / "group_results.json"), output_dir=figures_dir)
viz.generate_all()
print(f"✅ Figuras generadas en {figures_dir}")

# ============================================================
# 6️⃣ Generar informe PDF final
# ============================================================
print("\n📄 Creando informe PDF final...")
report = PDFReport(
    results_file=str(export_dir / "group_results.json"),
    figures_dir=figures_dir
)
report.generate()
print(f"✅ Informe PDF generado en {report.output_file}")

print("\n🎉 Análisis completo terminado con éxito. Revisa las carpetas generadas.")
