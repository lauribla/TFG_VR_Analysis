import streamlit as st
import pandas as pd
import json
import plotly.express as px
from pathlib import Path
import glob

# ============================================================
# 🔹 Funciones de carga
# ============================================================
def load_results(results_file):
    """Carga resultados desde JSON o CSV, adaptándose al formato global o agrupado."""
    results_path = Path(results_file)

    if results_path.suffix == ".json":
        with open(results_path, "r", encoding="utf-8") as f:
            results = json.load(f)

        # Caso 1: JSON jerárquico (global)
        if isinstance(results, dict) and all(isinstance(v, dict) for v in results.values()):
            rows = []
            for id_, res in results.items():
                flat = {"id": id_}
                for cat, metrics in res.items():
                    if isinstance(metrics, dict):
                        for key, value in metrics.items():
                            flat[f"{cat}_{key}"] = value
                rows.append(flat)
            return pd.DataFrame(rows)

        # Caso 2: JSON plano (lista de dicts agrupados)
        elif isinstance(results, list):
            return pd.DataFrame(results)

    elif results_path.suffix == ".csv":
        return pd.read_csv(results_path)

    st.error("Formato de archivo no soportado. Usa JSON o CSV.")
    return pd.DataFrame()

# ============================================================
# 🔹 Configuración principal del Dashboard
# ============================================================
def main():
    st.set_page_config(page_title="VR User Evaluation Dashboard", layout="wide")
    st.title("📊 VR User Evaluation - Dashboard Interactivo")

    # Buscar automáticamente el último archivo de resultados
    default_files = sorted(glob.glob("python_analysis/pruebas/exports_*/grouped_metrics.csv"), reverse=True)
    default_files += sorted(glob.glob("python_analysis/pruebas/exports_*/group_results.json"), reverse=True)
    default_file = default_files[0] if default_files else ""

    results_file = st.text_input("Ruta del archivo de resultados (JSON o CSV):", default_file)

    if not Path(results_file).exists():
        st.warning("Introduce la ruta correcta al archivo exportado (grouped_metrics.csv o group_results.json).")
        return

    df = load_results(results_file)
    if df.empty:
        st.warning("⚠️ No se pudieron cargar los resultados.")
        return

    # ============================================================
    # 🔹 Detección de tipo de datos (agrupado o global)
    # ============================================================
    mode = "agrupado" if {"user_id", "group_id", "session_id"}.issubset(df.columns) else "global"
    st.success(f"✅ Datos cargados correctamente ({mode.upper()})")

    # ============================================================
    # 🔹 Filtros interactivos (solo modo agrupado)
    # ============================================================
    if mode == "agrupado":
        st.sidebar.header("Filtros de exploración")

        groups = sorted(df["group_id"].dropna().unique())
        users = sorted(df["user_id"].dropna().unique())
        sessions = sorted(df["session_id"].dropna().unique())

        selected_groups = st.sidebar.multiselect("Selecciona grupos:", groups, default=groups)
        selected_users = st.sidebar.multiselect("Selecciona usuarios:", users, default=users)
        selected_sessions = st.sidebar.multiselect("Selecciona sesiones:", sessions, default=sessions)

        # Filtrar DataFrame dinámicamente
        df_filtered = df[
            df["group_id"].isin(selected_groups)
            & df["user_id"].isin(selected_users)
            & df["session_id"].isin(selected_sessions)
        ]
        st.info(f"{len(df_filtered)} filas después de aplicar filtros.")
    else:
        df_filtered = df

    # ============================================================
    # 🔹 Resumen general
    # ============================================================
    st.subheader("Resumen de la base de datos")
    if mode == "agrupado":
        col1, col2, col3 = st.columns(3)
        col1.metric("👥 Usuarios únicos", df["user_id"].nunique())
        col2.metric("🎯 Grupos experimentales", df["group_id"].nunique())
        col3.metric("🕹️ Sesiones registradas", df["session_id"].nunique())

    # ============================================================
    # 🔹 Indicadores oficiales
    # ============================================================
    st.header("📈 Indicadores de la Tabla Oficial")

    if mode == "agrupado":
        indicadores = [
            ("hit_ratio", "Efectividad - Hit Ratio"),
            ("success_rate", "Efectividad - Porcentaje de tareas completadas"),
            ("avg_reaction_time_ms", "Eficiencia - Tiempo medio de reacción (ms)"),
            ("avg_task_duration_ms", "Eficiencia - Duración media de tarea (ms)"),
            ("activity_level", "Presencia - Nivel de actividad (eventos/minuto)"),
        ]
        eje_x = "group_id"
    else:
        indicadores = [
            ("efectividad_hit_ratio", "Efectividad - Hit Ratio"),
            ("efectividad_success_rate", "Efectividad - Porcentaje de tareas completadas"),
            ("eficiencia_avg_reaction_time_ms", "Eficiencia - Tiempo medio de reacción (ms)"),
            ("eficiencia_avg_task_duration_ms", "Eficiencia - Duración media de tarea (ms)"),
            ("presencia_activity_level_per_min", "Presencia - Actividad por minuto"),
        ]
        eje_x = "id"

    for col, titulo in indicadores:
        if col in df_filtered.columns:
            fig = px.bar(df_filtered, x=eje_x, y=col, color=eje_x, title=titulo, text_auto=True)
            st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Comparaciones personalizadas
    # ============================================================
    st.header("🔍 Comparación personalizada")

    numeric_cols = [c for c in df_filtered.columns if df_filtered[c].dtype in ["float64", "int64"]]
    col_x = st.selectbox("Eje X:", options=["group_id", "user_id", "session_id"] if mode == "agrupado" else ["id"])
    col_y = st.selectbox("Métrica:", options=numeric_cols)

    if col_y:
        fig = px.box(df_filtered, x=col_x, y=col_y, color=col_x,
                     title=f"Comparativa de {col_y} por {col_x}")
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Eventos personalizados
    # ============================================================
    st.header("🎨 Eventos Personalizados (Custom Events)")

    custom_cols = [c for c in df_filtered.columns if c.startswith("custom_events_")]
    if not custom_cols:
        st.info("No se encontraron eventos personalizados en los resultados.")
    else:
        melted = df_filtered.melt(
            id_vars=["user_id", "group_id"] if mode == "agrupado" else ["id"],
            value_vars=custom_cols,
            var_name="custom_event",
            value_name="count"
        )
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")

        fig = px.bar(melted, x="group_id" if mode == "agrupado" else "id",
                     y="count", color="custom_event", barmode="group",
                     title="Frecuencia de Custom Events", text_auto=True)
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Tabla completa
    # ============================================================
    st.header("📋 Tabla completa de métricas")
    st.dataframe(df_filtered)

    # Descargar CSV filtrado
    csv = df_filtered.to_csv(index=False).encode("utf-8")
    st.download_button(
        label="💾 Descargar datos filtrados (CSV)",
        data=csv,
        file_name="vr_user_filtered_results.csv",
        mime="text/csv"
    )

# ============================================================
# 🔹 Entrada principal
# ============================================================
if __name__ == "__main__":
    main()
