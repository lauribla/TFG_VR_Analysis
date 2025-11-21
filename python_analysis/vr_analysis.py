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
DB_NAME = "test"
COLLECTION_NAME = "tfg"
MONGO_URI = "mongodb://localhost:27017"

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
# EXTRAER CONFIG INICIAL DEL LOG (primer log enviado por Unity)
# ============================================================

print("\n⚙️  Buscando configuración del experimento en MongoDB...")

config_df = df[df["event_type"] == "config"]

if not config_df.empty:
    row = config_df.sort_values("timestamp").iloc[0]

    # Extraer todas las columnas que empiezan por "event_context."
    context_cols = {col: row[col] for col in df.columns if col.startswith("event_context.")}

    # Reconstruir jerarquía
    experiment_config = {}

    for key, value in context_cols.items():
        parts = key.split(".")[1:]      # quitar "event_context"
        cursor = experiment_config
        for p in parts[:-1]:
            cursor = cursor.setdefault(p, {})
        cursor[parts[-1]] = value

    print("✅ Config reconstruida desde columnas expandidas.\n")

else:
    experiment_config = None
    print("⚠️  No existe configuración registrada (event_type='config').\n")


# ============================================================
# 3️⃣ Resumen inicial de sesiones y usuarios
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
# 4️⃣ Calcular métricas
# ============================================================

print("\n📊 Calculando métricas (eficiencia, efectividad, satisfacción)...")
metrics = MetricsCalculator(df)
results = metrics.compute_all()

print("\n=== Resultados globales ===")
print(json.dumps(results, indent=4))

# ============================================================
# 5️⃣ Crear carpetas de exportación
# ============================================================

timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

base_dir = Path(__file__).parent
export_dir = base_dir / f"pruebas/exports_{timestamp}"
figures_dir = base_dir / f"pruebas/figures_{timestamp}"

os.makedirs(export_dir, exist_ok=True)
os.makedirs(figures_dir, exist_ok=True)

print(f"\n📁 Directorios creados:")
print(f"  - Exportaciones: {export_dir}")
print(f"  - Figuras:       {figures_dir}")

# ============================================================
# 6️⃣ Guardar CONFIG extraído
# ============================================================

if experiment_config is not None:
    config_path = export_dir / "experiment_config_from_mongo.json"
    with open(config_path, "w", encoding="utf-8") as f:
        json.dump(experiment_config, f, indent=4)
    print(f"📄 Config del experimento exportado a {config_path}")
else:
    print("⚠️  No se pudo exportar config (no existe).")

# ============================================================
# 7️⃣ Exportar resultados a JSON y CSV
# ============================================================

print("\n💾 Exportando resultados...")

exporter = MetricsExporter(results, output_dir=export_dir)
exporter.to_json("results.json")
exporter.to_csv("results.csv")

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

print(f"✅ Resultados exportados correctamente.\n")

# ============================================================
# 8️⃣ Generar figuras
# ============================================================

GENERAR_GLOBAL = True
GENERAR_AGRUPADO = True

print("📈 Generando gráficas...")

group_results_path = export_dir / "group_results.json"
grouped_metrics_path = export_dir / "grouped_metrics.csv"

generated_figures = 0

if GENERAR_GLOBAL and group_results_path.exists():
    global_dir = figures_dir / "global"
    viz_global = Visualizer(str(group_results_path), output_dir=global_dir)
    viz_global.generate_all()
    generated_figures += len(list(global_dir.glob("*.png")))

if GENERAR_AGRUPADO and grouped_metrics_path.exists():
    grouped_dir = figures_dir / "agrupado"
    viz_grouped = Visualizer(str(grouped_metrics_path), output_dir=grouped_dir)
    viz_grouped.generate_all()
    generated_figures += len(list(grouped_dir.glob("*.png")))

print(f"📊 Figuras generadas: {generated_figures}")

# ============================================================
# 9️⃣ Generar informes PDF
# ============================================================

print("\n📄 Generando informe PDF...")

if GENERAR_GLOBAL and group_results_path.exists():
    report_global = PDFReport(
        results_file=str(group_results_path),
        figures_dir=figures_dir / "global",
        base_dir=base_dir
    )
    report_global.generate()

if GENERAR_AGRUPADO and grouped_metrics_path.exists():
    report_grouped = PDFReport(
        results_file=str(grouped_metrics_path),
        figures_dir=figures_dir / "agrupado",
        base_dir=base_dir
    )
    report_grouped.generate()

print("\n🎉 Análisis completo terminado.")
