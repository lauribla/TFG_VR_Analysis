import streamlit as st
import pandas as pd
import json
import plotly.express as px
from pathlib import Path
import glob

# ============================================================
# 🔹 Cargar resultados dinámicamente
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
            return pd.DataFrame(rows), "global"

        # Caso 2: JSON plano (lista de dicts agrupados)
        elif isinstance(results, list):
            return pd.DataFrame(results), "agrupado"

    elif results_path.suffix == ".csv":
        df = pd.read_csv(results_path)
        # Detectar modo automáticamente por columnas
        mode = "agrupado" if {"user_id", "group_id", "session_id"}.issubset(df.columns) else "global"
        return df, mode

    st.error("Formato de archivo no soportado. Usa JSON o CSV.")
    return pd.DataFrame(), "desconocido"

# ============================================================
# 🔹 Interfaz principal
# ============================================================
def main():
    st.set_page_config(page_title="VR User Evaluation Dashboard", layout="wide")
    st.title("📊 VR User Evaluation - Dashboard Interactivo")

    # Buscar los últimos resultados exportados automáticamente
    export_dirs = sorted(glob.glob("python_analysis/pruebas/exports_*"), reverse=True)
    if not export_dirs:
        st.warning("No se encontraron resultados exportados. Ejecuta primero vr_analysis.py.")
        return

    latest_dir = Path(export_dirs[0])

    # Detectar archivos disponibles
    group_results = latest_dir / "group_results.json"
    grouped_metrics = latest_dir / "grouped_metrics.csv"

    # Selección de modo visual
    available_modes = []
    if group_results.exists():
        available_modes.append("Global")
    if grouped_metrics.exists():
        available_modes.append("Agrupado")

    if not available_modes:
        st.error("No se encontraron archivos de resultados (ni global ni agrupado).")
        return

    mode_choice = st.radio("🎚️ Modo de análisis", available_modes, horizontal=True)

    # Determinar archivo según el modo elegido
    if mode_choice == "Global":
        results_file = group_results
    else:
        results_file = grouped_metrics

    st.write(f"📁 Analizando archivo: `{results_file.name}`")

    df, detected_mode = load_results(results_file)

    if df.empty:
        st.warning("⚠️ No se pudieron cargar los datos.")
        return

    st.success(f"✅ Datos cargados correctamente ({detected_mode.upper()})")

    # ============================================================
    # 🔹 Filtros interactivos (solo modo agrupado)
    # ============================================================
    if detected_mode == "agrupado":
        st.sidebar.header("Filtros de exploración")

        groups = sorted(df["group_id"].dropna().unique())
        users = sorted(df["user_id"].dropna().unique())
        sessions = sorted(df["session_id"].dropna().unique())

        selected_groups = st.sidebar.multiselect("Selecciona grupos:", groups, default=groups)
        selected_users = st.sidebar.multiselect("Selecciona usuarios:", users, default=users)
        selected_sessions = st.sidebar.multiselect("Selecciona sesiones:", sessions, default=sessions)

        df_filtered = df[
            df["group_id"].isin(selected_groups)
            & df["user_id"].isin(selected_users)
            & df["session_id"].isin(selected_sessions)
        ]
        st.info(f"{len(df_filtered)} filas después de aplicar filtros.")
    else:
        df_filtered = df

    # ============================================================
    # 🔹 Indicadores oficiales (auto-detección)
    # ============================================================
    st.header("📈 Indicadores de la Tabla Oficial")

    numeric_cols = [c for c in df_filtered.columns if df_filtered[c].dtype in ["float64", "int64"]]

    if not numeric_cols:
        st.warning("No se encontraron métricas numéricas para graficar.")
        return

    eje_x = "group_id" if detected_mode == "agrupado" else "id"

    for col in numeric_cols[:12]:  # limita a las primeras 12 métricas para no saturar
        fig = px.bar(df_filtered, x=eje_x, y=col, color=eje_x,
                     title=col.replace("_", " ").title(), text_auto=True)
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Comparación personalizada
    # ============================================================
    st.header("🔍 Comparación personalizada")

    col_x = st.selectbox("Eje X:", options=[eje_x])
    col_y = st.selectbox("Métrica:", options=numeric_cols)

    if col_y:
        fig = px.box(df_filtered, x=col_x, y=col_y, color=col_x,
                     title=f"Comparativa de {col_y} por {col_x}")
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Tabla completa de métricas
    # ============================================================
    st.header("📋 Tabla completa de métricas")
    st.dataframe(df_filtered)

    csv = df_filtered.to_csv(index=False).encode("utf-8")
    st.download_button(
        label="💾 Descargar datos filtrados (CSV)",
        data=csv,
        file_name=f"vr_user_{mode_choice.lower()}_filtered.csv",
        mime="text/csv"
    )


if __name__ == "__main__":
    main()
