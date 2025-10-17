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

#SI NO SE DESACTIVA EL MODO AGRUPADO, POR DEFECTO SE VERÁ ESTE EN EL PDF (en el dashboard se podrá elegir el modo)
# ============================================================
# CONFIGURACIÓN DE MODOS
# ============================================================
GENERAR_GLOBAL = True          # ⬅️ Cambia a False para omitir el modo global
GENERAR_AGRUPADO = True        # ⬅️ Cambia a False para omitir el modo agrupado

# ============================================================
# 6️⃣ Generar visualizaciones
# ============================================================
from python_visualization.visualize_groups import Visualizer

print("\n📈 Generando gráficas...")

group_results_path = export_dir / "group_results.json"
grouped_metrics_path = export_dir / "grouped_metrics.csv"

generated_figures = 0

# --- FIGURAS GLOBALES ---
if GENERAR_GLOBAL and group_results_path.exists():
    print(f"[vr_analysis] 🔍 Generando figuras globales desde {group_results_path.name}")
    global_dir = figures_dir / "global"
    viz_global = Visualizer(str(group_results_path), output_dir=global_dir)
    viz_global.generate_all()
    generated_figures += len(list(global_dir.glob("*.png")))
elif GENERAR_GLOBAL:
    print("[vr_analysis] ⚠️ No se encontró group_results.json, se omiten las gráficas globales.")
else:
    print("[vr_analysis] ⏩ Modo global desactivado por el usuario.")

# --- FIGURAS AGRUPADAS ---
if GENERAR_AGRUPADO and grouped_metrics_path.exists():
    print(f"[vr_analysis] 🔍 Generando figuras agrupadas desde {grouped_metrics_path.name}")
    grouped_dir = figures_dir / "agrupado"
    viz_grouped = Visualizer(str(grouped_metrics_path), output_dir=grouped_dir)
    viz_grouped.generate_all()
    generated_figures += len(list(grouped_dir.glob("*.png")))
elif GENERAR_AGRUPADO:
    print("[vr_analysis] ⚠️ No se encontró grouped_metrics.csv, se omiten las gráficas agrupadas.")
else:
    print("[vr_analysis] ⏩ Modo agrupado desactivado por el usuario.")

if generated_figures > 0:
    print(f"✅ {generated_figures} figuras generadas en total dentro de {figures_dir}")
else:
    print("⚠️ No se generaron figuras. Verifica que los resultados contengan métricas numéricas.")

# ============================================================
# 7️⃣ Generar informes PDF
# ============================================================
from python_visualization.pdf_reporter import PDFReport

print("\n📄 Creando informe PDF final...")

# PDF global
if GENERAR_GLOBAL and group_results_path.exists():
    report_global = PDFReport(
        results_file=str(group_results_path),
        figures_dir=figures_dir / "global",
        base_dir=base_dir
    )
    report_global.generate()
elif GENERAR_GLOBAL:
    print("[vr_analysis] ⚠️ No se generó PDF global (no hay archivo JSON).")

# PDF agrupado
if GENERAR_AGRUPADO and grouped_metrics_path.exists():
    report_grouped = PDFReport(
        results_file=str(grouped_metrics_path),
        figures_dir=figures_dir / "agrupado",
        base_dir=base_dir
    )
    report_grouped.generate()
elif GENERAR_AGRUPADO:
    print("[vr_analysis] ⚠️ No se generó PDF agrupado (no hay archivo CSV).")

print(f"✅ Informes PDF generados en {base_dir}")
print("\n🎉 Análisis completo terminado con éxito. Revisa las carpetas generadas.")
