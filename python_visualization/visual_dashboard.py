import streamlit as st
import pandas as pd
import json
import plotly.express as px
from pathlib import Path
import glob

# ============================================================
# Cargar datos
# ============================================================
def load_results(results_file):
    results_path = Path(results_file)
    if results_path.suffix == ".json":
        with open(results_path, "r", encoding="utf-8") as f:
            results = json.load(f)
        rows = []
        for id_, res in results.items():
            flat = {"id": id_}
            for cat, metrics in res.items():
                if isinstance(metrics, dict):
                    for key, value in metrics.items():
                        flat[f"{cat}_{key}"] = value
            rows.append(flat)
        return pd.DataFrame(rows)
    elif results_path.suffix == ".csv":
        return pd.read_csv(results_path)
    else:
        st.error("Formato de archivo no soportado. Usa JSON o CSV.")
        return pd.DataFrame()

# ============================================================
# Interfaz Streamlit
# ============================================================
def main():
    st.set_page_config(page_title="VR User Evaluation Dashboard", layout="wide")
    st.title("📊 VR User Evaluation - Dashboard Interactivo")

    # Buscar automáticamente el último group_results.json
    default_files = sorted(glob.glob("python_analysis/pruebas/exports_*/group_results.json"), reverse=True)
    default_file = default_files[0] if default_files else ""

    # Input de usuario con valor por defecto
    results_file = st.text_input("Ruta del archivo de resultados (JSON o CSV):", default_file)

    if not Path(results_file).exists():
        st.warning("Introduce la ruta correcta al archivo exportado (group_results.json o results.csv).")
        return

    df = load_results(results_file)

    if df.empty:
        st.warning("No se pudieron cargar los resultados.")
        return

    st.success(f"Resultados cargados: {len(df)} grupos/usuarios")

    # ============================================================
    # Visualización de indicadores oficiales
    # ============================================================
    st.header("Indicadores de la Tabla Oficial")

    indicadores = [
        ("efectividad_hit_ratio", "Efectividad - Hit Ratio"),
        ("efectividad_success_rate", "Efectividad - Porcentaje de tareas completadas"),
        ("eficiencia_avg_reaction_time_ms", "Eficiencia - Tiempo medio de reacción (ms)"),
        ("eficiencia_avg_task_duration_ms", "Eficiencia - Duración media de tarea (ms)"),
        ("presencia_activity_level_per_min", "Presencia - Actividad por minuto"),
    ]

    for col, titulo in indicadores:
        if col in df.columns:
            fig = px.bar(df, x="id", y=col, color="id", title=titulo, text_auto=True)
            st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # Visualización de eventos personalizados
    # ============================================================
    st.header("Eventos Personalizados (Custom Events)")

    custom_cols = [c for c in df.columns if c.startswith("custom_events_")]
    if not custom_cols:
        st.info("No se encontraron eventos personalizados en los resultados.")
    else:
        melted = df.melt(id_vars="id", value_vars=custom_cols,
                         var_name="custom_event", value_name="count")
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")

        fig = px.bar(melted, x="id", y="count", color="custom_event",
                     barmode="group", title="Frecuencia de Custom Events", text_auto=True)
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # Tabla completa
    # ============================================================
    st.header("Tabla completa de métricas")
    st.dataframe(df)

if __name__ == "__main__":
    main()
