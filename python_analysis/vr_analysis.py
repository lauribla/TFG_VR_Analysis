"""
VR USER EVALUATION - Análisis completo de la base de datos MongoDB
------------------------------------------------------------------
Este script conecta con la base de datos donde Unity guarda los logs (test.tfg),
los analiza y genera automáticamente:
    - Métricas por categoría (efectividad, eficiencia, satisfacción, presencia)
    - Métricas globales ponderadas
    - Archivos CSV/JSON exportados
    - Gráficas comparativas
    - Informe PDF con los resultados
"""

import pandas as pd
import shutil
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
# 1️⃣ Conectar con MongoDB y cargar logs
# ============================================================

# Conectando con parámetros del .env (gestión automática en LogParser)
parser = LogParser()
print(f"🔗 Conectando a MongoDB → URI: {parser.mongo_uri} | DB: {parser.db_name} | COL: {parser.collection_name}")
logs = parser.fetch_logs()

# df sin expandir → recuperar config
df_raw = parser.parse_logs(logs, expand_context=False)

# df expandido → métricas
df = parser.parse_logs(logs, expand_context=True)
print(df.columns)

parser.close()

if df.empty:
    print("⚠️  No se encontraron logs en Mongo.")
    exit()

print(f"✅ {len(df)} documentos cargados desde Mongo.\n")


# ============================================================
# 2️⃣ Extraer config (Log vs Local override)
# ============================================================

print("⚙️  Leyendo configuración del experimento...\n")

experiment_config = None

# Check override
if os.environ.get("FORCE_LOCAL_CONFIG", "false").lower() == "true":
    config_path = Path("vr_logger/experiment_config.json")
    if config_path.exists():
        print(f"⚠️  FORZANDO CONFIGURACIÓN LOCAL: {config_path}")
        with open(config_path, "r", encoding="utf-8") as f:
            experiment_config = json.load(f)
    else:
        print(f"❌  No se encontró la configuración local en {config_path}")

# Fallback/Default: Extract from logs
if experiment_config is None:
    for entry in logs:
        if entry.get("event_type") == "config":
            experiment_config = entry.get("event_context")
            break

if experiment_config is not None:
    print("✅ Config cargada correctamente.\n")
else:
    print("⚠️  No existe configuración en los logs y no se forzó local.\n")


# ============================================================
# 3️⃣ Resumen de sesiones y usuarios
# ============================================================

print("👥 Resumen de usuarios, grupos y sesiones:")

usuarios = df["user_id"].nunique()
grupos = df["group_id"].nunique()
sesiones = df["session_id"].nunique()

print(f"  • Usuarios: {usuarios}")
print(f"  • Grupos: {grupos}")
print(f"  • Sesiones: {sesiones}\n")

print("📄 Lista de sesiones detectadas:")
print(df[["user_id", "group_id", "session_id"]].drop_duplicates().to_string(index=False))


# ============================================================
# 4️⃣ Calcular métricas usando MetricsCalculator
# ============================================================

print("\n📊 Calculando métricas ponderadas del experimento...\n")

metrics = MetricsCalculator(df, experiment_config=experiment_config)
raw_results = metrics.compute_all()

# ------------------------------------------------------------
# ADAPTAR RESULTADO a FORMATO PARA EL PDF Y EXPORTER
# ------------------------------------------------------------
results_for_export = {}

for categoria, contenido in raw_results["categorias"].items():

    # Subestructura compatible con PDFReporter
    results_for_export[categoria] = {
        "score": contenido["score"]
    }

    for metric_name, metric_data in contenido.items():
        if isinstance(metric_data, dict):
            results_for_export[categoria][metric_name] = metric_data["raw"]

# añadir puntuación global
results_for_export["global_score"] = raw_results["global_score"]

print(json.dumps(results_for_export, indent=4))


# ============================================================
# 5️⃣ Crear carpetas de exportación UNIFICADAS
# ============================================================

timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

base_dir = Path(__file__).parent / "pruebas"
output_dir = base_dir / f"analysis_{timestamp}"
results_dir = output_dir / "results"
figures_dir = output_dir / "figures"

# Crear estructura
os.makedirs(results_dir, exist_ok=True)
os.makedirs(figures_dir, exist_ok=True)

print(f"📂 Carpeta de salida creada: {output_dir}")


# ============================================================
# 6️⃣ Guardar config en archivo
# ============================================================

if experiment_config is not None:
    config_path = results_dir / "experiment_config_from_mongo.json"
    with open(config_path, "w", encoding="utf-8") as f:
        json.dump(experiment_config, f, indent=4)
    print(f"📄 Config exportada: {config_path.name}\n")


# ============================================================
# 7️⃣ Exportar resultados JSON + CSV
# ============================================================

print("💾 Exportando métricas...")

exporter = MetricsExporter(results_for_export, output_dir=results_dir)
exporter.to_json("results.json")
exporter.to_csv("results.csv")

grouped_df = metrics.compute_grouped_metrics()
grouped_path = results_dir / "grouped_metrics.csv"
grouped_df.to_csv(grouped_path, index=False)

# También exportar versión agrupada como JSON
MetricsExporter.export_multiple(
    [results_for_export],
    ["Global"],
    mode="json",
    output_dir=results_dir,
    filename="group_results"
)

print("✅ Exportación completada.\n")


# ============================================================
# 8️⃣ Generar figuras
# ============================================================

print("📈 Generando gráficas...")

global_json = results_dir / "group_results.json"
generated_figures = 0

if global_json.exists():
    global_dir = figures_dir / "global"
    viz_global = Visualizer(str(global_json), output_dir=global_dir)
    viz_global.generate_all()
    generated_figures += len(list(global_dir.glob("*.png")))

if grouped_path.exists():
    grouped_dir = figures_dir / "agrupado"
    viz_grouped = Visualizer(str(grouped_path), output_dir=grouped_dir)
    viz_grouped.generate_all()
    generated_figures += len(list(grouped_dir.glob("*.png")))

print(f"📊 Figuras generadas: {generated_figures}\n")


# ============================================================
# 9️⃣ Generar informes PDF
# ============================================================

print("📄 Generando informe PDF...\n")

# Usar el path consolidado 'output_dir'
pdf_output_path = output_dir / "final_report.pdf"

# Priorizamos 'agrupado' para el reporte si existe, ya que es más completo para gráficas
report_file = grouped_path if grouped_path.exists() else global_json

if report_file.exists():
    report = PDFReport(
        results_file=str(report_file),
        figures_dir=figures_dir,  # Pasamos la raíz de figuras
        output_dir=output_dir     # Pasamos la raíz de output
    )
    report.generate()

print("🎉 ANÁLISIS COMPLETO FINALIZADO.\n")
