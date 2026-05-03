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
from python_visualization.spatial_plotter import SpatialVisualizer
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

# Buscar cuestionarios (SUS) antes de cerrar conexión
quest_data = []
try:
    quests_col = parser.client[parser.db_name]["questionnaires"]
    quest_data = list(quests_col.find({}))
except Exception as e:
    print(f"⚠️ Warning: Podría no haber cuestionarios. {e}")

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
    configs = [entry for entry in logs if entry.get("event_type") == "config"]
    if configs:
        # Ordenar por timestamp para asegurar que usamos la ÚLTIMA (más reciente)
        # Asumimos que timestamp es comparable (datetime o string ISO)
        try:
            configs.sort(key=lambda x: x.get("timestamp", ""))
            latest_config_log = configs[-1]
            experiment_config = latest_config_log.get("event_context")
            print(f"✅ Configuración cargada desde logs (La más reciente: {latest_config_log.get('timestamp')})")
        except Exception as e:
            print(f"⚠️ Error ordenando configs, usando la última encontrada: {e}")
            experiment_config = configs[-1].get("event_context")

if experiment_config is not None:
    print("✅ Config cargada correctamente.\n")

    # ------------------------------------------------------------
    # FILTRADO POR SESSION_NAME
    # ------------------------------------------------------------
    # Objetivo: Analizar SOLO las sesiones que coincidan con el session_name del config actual
    # para evitar mezclar experimentos distintos (ej: "Experiment_A" vs "Experiment_B")

    target_session_name = experiment_config.get("session", {}).get("session_name")

    if target_session_name:
        print(f"🎯 Target Session Name: '{target_session_name}' (from config)")

        # Estrategia: Buscar en los logs 'config' o 'session_start' qué session_ids tienen este nombre
        # IMPORTANTE: Usamos df_raw porque tiene la columna 'context' como string JSON.
        # df (el expandido) NO tiene 'context' porque lo expandió en columnas.

        # 1. Buscar en configs (usando df_raw)
        matching_configs = []
        if "context" in df_raw.columns:
             matching_configs = df_raw[
                (df_raw["event_type"] == "config") &
                (df_raw["context"].fillna("").apply(lambda x: target_session_name in str(x)))
            ]["session_id"].unique()

        # 2. Buscar en session_start (usando df_raw)
        matching_starts = []
        if "context" in df_raw.columns:
            matching_starts = df_raw[
                (df_raw["event_type"] == "session_start") &
                (df_raw["context"].fillna("").apply(lambda x: target_session_name in str(x)))
            ]["session_id"].unique()

        # 3. Fallback: Si df_raw no funciona, buscar en df expandido (columna 'session' si existe)
        matching_expanded = []
        if "session" in df.columns: # A veces 'session' se expande como columna si el json lo tenía
             matching_expanded = df[
                df["session"].apply(lambda x: isinstance(x, dict) and x.get("session_name") == target_session_name if isinstance(x, dict) else False)
             ]["session_id"].unique()
        elif "session_session_name" in df.columns: # Flattened key pattern
             matching_expanded = df[df["session_session_name"] == target_session_name]["session_id"].unique()


        # Unir todos los session_ids válidos
        valid_sessions = set(matching_configs) | set(matching_starts) | set(matching_expanded)

        # Si no encontramos nada con esos métodos, intentamos mirar si el propio config actual tiene session_id
        current_config_sid = experiment_config.get("session_id")
        if current_config_sid:
            valid_sessions.add(current_config_sid)

        if valid_sessions:
            original_count = df["session_id"].nunique()
            df = df[df["session_id"].isin(valid_sessions)]
            filtered_count = df["session_id"].nunique()
            print(f"🧹 Filtrando logs... Se mantienen {filtered_count} sesiones de {original_count} totales.\n")
        else:
            print(f"⚠️ No se encontraron sesiones coincidiendo con '{target_session_name}' en los logs. Mostrando todo.\n")
    else:
        print("⚠️ El config no tiene 'session_name'. No se puede filtrar por experimento.\n")

else:
    print("⚠️  No existe configuración en los logs y no se forzó local.\n")


# Eliminar los eventos de configuración web puros para que no cuenten como un participante fantasma
df = df[df["user_id"] != "WEB_CONFIG"]

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

# --- INTEGRATING SUBJECTIVE QUESTIONNAIRES ---
print("📋 Integrando cuestionarios subjetivos (SUS)...")
if quest_data and not grouped_df.empty:
    df_q = pd.DataFrame(quest_data)
    cols_to_keep = ["user_id", "sus_score", "subj_efectividad", "subj_eficiencia", "subj_satisfaccion", "subj_presencia", "presence_score", "satisfaction_score"]
    cols_to_keep = [c for c in cols_to_keep if c in df_q.columns]
    
    if len(cols_to_keep) > 1: # user_id + al menos otra columna
        df_q = df_q[cols_to_keep]
        # Quedarse con el último intento en caso de duplicados
        df_q = df_q.drop_duplicates(subset=["user_id"], keep="last")
        
        # Match robusto por si Unity incluye el formato "Grupo_Usuario" y la BD tiene "Usuario" o viceversa
        for col in cols_to_keep:
            if col != "user_id" and col not in grouped_df.columns:
                grouped_df[col] = pd.NA
                
        for i, log_row in grouped_df.iterrows():
            log_uid = str(log_row["user_id"])
            match = None
            for _, q_row in df_q.iterrows():
                q_uid = str(q_row["user_id"])
                if q_uid in log_uid or log_uid in q_uid:
                    match = q_row
                    break
                    
            if match is not None:
                for col in cols_to_keep:
                    if col != "user_id":
                        grouped_df.at[i, col] = match[col]
                        
        print(f"✅ Se cruzaron datos subjetivos (SUS y nuevos) con coincidencia flexible.")
else:
    print("⚠️ No hay datos de cuestionarios para cruzar.")
# ---------------------------------------------

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

    generated_figures += len(list(grouped_dir.glob("*.png")))

print(f"📊 Figuras generadas: {generated_figures}\n")

# ============================================================
# 8.1️⃣  Generar figuras espaciales (Mapas de calor / Trayectorias)
# ============================================================
print("🗺️ Generando visualizaciones espaciales (si existen datos de tracking)...")
play_area_w = None
play_area_d = None
if experiment_config is not None:
    play_area_w = experiment_config.get("session", {}).get("play_area_width")
    play_area_d = experiment_config.get("session", {}).get("play_area_depth")

spatial_viz = SpatialVisualizer(
    df, 
    output_dir=figures_dir / "spatial",
    play_area_width=play_area_w,
    play_area_depth=play_area_d,
    experiment_config=experiment_config
)
spatial_viz.generate_all()
print("\n")


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
