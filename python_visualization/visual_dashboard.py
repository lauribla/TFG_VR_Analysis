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

        # --- Caso GLOBAL: dict con categorías anidadas ---
        if isinstance(results, dict):
            # Compatibilidad: si no hay "Global", trátalo como global igualmente
            if "Global" in results:
                results = {"Global": results["Global"]}
            else:
                results = {"Global": results}

            rows = []
            for id_, res in results.items():
                flat = {"id": id_}
                for cat, metrics in res.items():
                    if isinstance(metrics, dict):
                        for key, value in metrics.items():
                            flat[f"{cat}_{key}"] = value
                    # Si una métrica viene como lista (p.ej. learning_curve), la ignoramos en tablas/gráficas
                rows.append(flat)
            return pd.DataFrame(rows), "global"

            rows = []
            for id_, res in results.items():
                flat = {"id": id_}
                for cat, metrics in res.items():
                    if isinstance(metrics, dict):
                        for key, value in metrics.items():
                            flat[f"{cat}_{key}"] = value
                rows.append(flat)
            return pd.DataFrame(rows), "global"

        # --- Caso AGRUPADO (lista de diccionarios) ---
        elif isinstance(results, list):
            return pd.DataFrame(results), "agrupado"

        elif isinstance(results, list):

            return pd.DataFrame(results), "agrupado"
    elif results_path.suffix == ".csv":
        df = pd.read_csv(results_path)
        mode = "agrupado" if {"user_id", "group_id", "session_id"}.issubset(df.columns) else "global"
        return df, mode
    st.error("Formato de archivo no soportado.")
    return pd.DataFrame(), "desconocido"

# ============================================================
# 🔹 Interfaz principal
# ============================================================
def main():
    st.set_page_config(page_title="VR User Evaluation Dashboard", layout="wide")
    st.title("📊 VR User Evaluation - Dashboard Interactivo")

    # Buscar exportaciones recientes
    export_dirs = sorted(glob.glob("python_analysis/pruebas/exports_*"), reverse=True)
    if not export_dirs:
        st.warning("No se encontraron resultados exportados. Ejecuta primero vr_analysis.py.")
        return

    latest_dir = Path(export_dirs[0])
    group_results = latest_dir / "group_results.json"
    grouped_metrics = latest_dir / "grouped_metrics.csv"

    # Selector de modo
    available_modes = []
    if group_results.exists():
        available_modes.append("Global")
    if grouped_metrics.exists():
        available_modes.append("Agrupado")
    mode_choice = st.radio("🎚️ Modo de análisis", available_modes, horizontal=True)
    results_file = group_results if mode_choice == "Global" else grouped_metrics

    df, detected_mode = load_results(results_file)
    if df.empty:
        st.warning("⚠️ No se pudieron cargar los datos.")
        return
    st.success(f"✅ Datos cargados correctamente ({detected_mode.upper()})")

    # ============================================================
    # 🔹 Filtros (solo agrupado)
    # ============================================================
    if detected_mode == "agrupado":
        st.sidebar.header("Filtros")
        groups = sorted(df["group_id"].dropna().unique())
        users = sorted(df["user_id"].dropna().unique())
        sessions = sorted(df["session_id"].dropna().unique())
        selected_groups = st.sidebar.multiselect("Grupos:", groups, default=groups)
        selected_users = st.sidebar.multiselect("Usuarios:", users, default=users)
        selected_sessions = st.sidebar.multiselect("Sesiones:", sessions, default=sessions)
        df = df[
            df["group_id"].isin(selected_groups)
            & df["user_id"].isin(selected_users)
            & df["session_id"].isin(selected_sessions)
        ]
        st.info(f"{len(df)} filas después de aplicar filtros.")
    # Normalizar nombres en modo GLOBAL: crear alias sin prefijo
    if detected_mode == "global":
        prefixes = ["efectividad", "eficiencia", "satisfaccion", "presencia"]
        for p in prefixes:
            pref = f"{p}_"
            pref_cols = [c for c in df.columns if c.startswith(pref)]
            for c in pref_cols:
                alias = c[len(pref):]  # quita "efectividad_", etc.
                # No sobreescribimos si ya existiera una columna sin prefijo
                if alias not in df.columns:
                    df[alias] = df[c]

    # ============================================================
    # 🔹 Definir categorías de métricas
    # ============================================================
    cat_cols = {
        "🟢 Efectividad": [
            "hit_ratio", "precision", "success_rate", "learning_curve_mean",
            "progression", "success_after_restart", "attempts_per_target"
        ],
        "🟠 Eficiencia": [
            "avg_reaction_time_ms", "avg_task_duration_ms", "time_per_success_s",
            "navigation_errors", "aim_errors", "task_duration_success", "task_duration_fail"
        ],
        "🟣 Satisfacción": [
            "retries_after_end", "voluntary_play_time_s", "aid_usage",
            "interface_errors", "learning_stability", "error_reduction_rate"
        ],
        "🔵 Presencia": [
            "inactivity_time_s", "first_success_time_s", "sound_localization_time_s",
            "activity_level_per_min", "audio_performance_gain"
        ]
    }

    eje_x = "group_id" if detected_mode == "agrupado" else "id"

    # ============================================================
    # 🔹 Mostrar gráficas por categoría
    # ============================================================
    for cat_name, cols in cat_cols.items():
        st.header(cat_name)
        found = [c for c in cols if c in df.columns]
        if not found:
            st.info(f"No hay métricas de {cat_name}.")
            continue
        for col in found:
            fig = px.bar(df, x=eje_x, y=col, color=eje_x,
                         title=col.replace("_", " ").title(), text_auto=True)
            st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Resumen de Scores Ponderados
    # ============================================================
    st.header("🏁 Resultados ponderados por categoría")

    score_cols = [
        "efectividad_score", "eficiencia_score", "satisfaccion_score", "presencia_score", "total_score"
    ]

    found_scores = [c for c in score_cols if c in df.columns]
    if not found_scores:
        st.info("No se encontraron puntuaciones ponderadas en los resultados.")
    else:
        st.dataframe(df[["id"] + found_scores] if "id" in df.columns else df[found_scores])

        # Mostrar gráfico de barras de resumen
        avg_scores = df[found_scores].mean(numeric_only=True)
        score_df = pd.DataFrame({
            "Métrica": avg_scores.index,
            "Valor promedio (%)": avg_scores.values
        })

        fig_scores = px.bar(score_df, x="Métrica", y="Valor promedio (%)",
                            color="Métrica", text_auto=True,
                            title="Resumen promedio de puntuaciones por categoría")
        st.plotly_chart(fig_scores, use_container_width=True)

    # ============================================================
    # 🔹 Eventos personalizados
    # ============================================================
    st.header("🎯 Custom Events")
    custom_cols = [c for c in df.columns if c.startswith("custom_events_")]
    if not custom_cols:
        st.info("No se encontraron eventos personalizados.")
    else:
        melted = df.melt(id_vars=eje_x, value_vars=custom_cols,
                         var_name="custom_event", value_name="count")
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")
        fig = px.bar(melted, x=eje_x, y="count", color="custom_event",
                     title="Frecuencia de Custom Events", barmode="group", text_auto=True)
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Tabla completa
    # ============================================================
    st.header("📋 Tabla completa de métricas")
    st.dataframe(df)
    csv = df.to_csv(index=False).encode("utf-8")
    st.download_button("💾 Descargar CSV filtrado", data=csv, file_name="vr_user_metrics.csv", mime="text/csv")


if __name__ == "__main__":
    main()
