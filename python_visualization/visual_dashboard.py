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
                    else:
                        # IMPORTANT: keep scalar values like global_score
                        flat[cat] = metrics
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
    # Robust path resolution: Find project root relative to this script
    script_dir = Path(__file__).resolve().parent
    project_root = script_dir.parent
    pruebas_dir = project_root / "python_analysis" / "pruebas"

    # Search for 'analysis_*' folders (matching vr_analysis output)
    search_pattern = pruebas_dir / "analysis_*"
    export_dirs = sorted(glob.glob(str(search_pattern)), reverse=True)
    if not export_dirs:
        st.warning("No se encontraron resultados exportados. Ejecuta primero vr_analysis.py.")
        return

    latest_dir = Path(export_dirs[0])
    # Correction: files are inside the 'results' subdirectory
    results_dir = latest_dir / "results"

    group_results = results_dir / "results.json"  # Global results usually results.json
    grouped_metrics = results_dir / "grouped_metrics.csv"

    # Selector de modo
    available_modes = []
    if group_results.exists():
        available_modes.append("Global")
    if grouped_metrics.exists():
        available_modes.append("Agrupado")

    if not available_modes:
        st.error(f"No se encontraron archivos de resultados en {results_dir}")
        return

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
    # ============================================================
    # 🔹 Definir categorías de métricas (Dinámico)
    # ============================================================
    known_categories = ["efectividad", "eficiencia", "satisfaccion", "presencia"]
    cat_cols = {
        f"{'🟢' if c == 'efectividad' else '🟠' if c == 'eficiencia' else '🟣' if c == 'satisfaccion' else '🔵'} {c.capitalize()}": []
        for c in known_categories}

    metric_to_cat = {
        "hit_ratio": "efectividad", "precision": "efectividad", "success_rate": "efectividad",
        "learning_curve_mean": "efectividad", "progression": "efectividad", "success_after_restart": "efectividad",

        "avg_reaction_time_ms": "eficiencia", "avg_task_duration_ms": "eficiencia", "time_per_success_s": "eficiencia",
        "navigation_errors": "eficiencia", "aim_errors": "eficiencia",

        "learning_stability": "satisfaccion", "error_reduction_rate": "satisfaccion",
        "voluntary_play_time_s": "satisfaccion", "aid_usage": "satisfaccion", "interface_errors": "satisfaccion",

        "activity_level_per_min": "presencia", "first_success_time_s": "presencia", "inactivity_time_s": "presencia",
        "sound_localization_time_s": "presencia", "audio_performance_gain": "presencia"
    }

    # Rellenar con columnas presentes en DF
    for col in df.columns:
        if col in metric_to_cat:
            cat_key = metric_to_cat[col]
            # Match con emoji key
            for k in cat_cols:
                if cat_key.capitalize() in k:
                    cat_cols[k].append(col)

    # Limpiar categorías vacías
    cat_cols = {k: v for k, v in cat_cols.items() if v}

    eje_x = "group_id" if detected_mode == "agrupado" else "id"

    # ============================================================
    # 🔹 Filtrado por Independent Variable
    # ============================================================
    if detected_mode == "agrupado" and "independent_variable" in df.columns:
        st.sidebar.markdown("---")
        st.sidebar.header("Variable Independiente")
        all_vars = sorted(df["independent_variable"].dropna().astype(str).unique())
        selected_vars = st.sidebar.multiselect("Filtrar por Variable:", all_vars, default=all_vars)

        if selected_vars:
            df = df[df["independent_variable"].astype(str).isin(selected_vars)]
            st.info(f"Mostrando {len(df)} sesiones para variables: {', '.join(selected_vars)}")

        # 🔹 Comparación específica
        st.header("⚖️ Comparación por Variable Independiente")

        # Calculate means for comparison
        numeric_cols = ["efectividad_score", "eficiencia_score", "satisfaccion_score", "presencia_score",
                        "global_score"]
        available_cols = [c for c in numeric_cols if c in df.columns]

        if available_cols and len(selected_vars) > 0:
            comp_df = df.groupby("independent_variable")[available_cols].mean().reset_index()

            # Melt for charting
            comp_melt = comp_df.melt(id_vars="independent_variable", var_name="Metric", value_name="Score")

            fig_comp = px.bar(
                comp_melt,
                x="independent_variable",
                y="Score",
                color="Metric",
                barmode="group",
                title="Promedio de Scores por Variable",
                text_auto=".2f"
            )
            st.plotly_chart(fig_comp, use_container_width=True)
            st.markdown("---")

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
            if detected_mode == "agrupado" and "user_id" in df.columns:
                fig = px.bar(
                    df,
                    x="user_id",  # separa por usuario
                    y=col,
                    color="group_id",  # colorea por grupo
                    barmode="group",
                    title=col.replace("_", " ").title(),
                    text_auto=True
                )
            else:
                fig = px.bar(
                    df,
                    x=eje_x,
                    y=col,
                    color=eje_x,
                    barmode="group",
                    title=col.replace("_", " ").title(),
                    text_auto=True
                )

            st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Resumen de Scores Ponderados
    # ============================================================
    st.header("🏁 Resultados ponderados por categoría")

    # Mapeo: columna posible -> nombre de categoría
    score_candidates = {
        "efectividad_score": "Efectividad",
        "efectividad": "Efectividad",
        "eficiencia_score": "Eficiencia",
        "eficiencia": "Eficiencia",
        "satisfaccion_score": "Satisfacción",
        "satisfaccion": "Satisfacción",
        "presencia_score": "Presencia",
        "presencia": "Presencia",
        "global_score": "Total Global",
        "total_score": "Total Global",
    }

    present = [c for c in score_candidates.keys() if c in df.columns]

    if not present:
        st.info("No se encontraron puntuaciones ponderadas en los resultados.")
    else:
        # --- Tabla por fila (usuario/grupo) con nombres bonitos ---
        pretty_df = pd.DataFrame({score_candidates[c]: pd.to_numeric(df[c], errors="coerce") for c in present})

        # Si quieres que se vea por usuario/grupo:
        st.dataframe(pretty_df)

        # --- Promedio por categoría (una barra por categoría) ---
        mean_scores = pd.DataFrame({
            "Categoría": [score_candidates[c] for c in present],
            "Score": [pd.to_numeric(df[c], errors="coerce").mean() for c in present]
        }).sort_values("Categoría")

        fig = px.bar(mean_scores, x="Categoría", y="Score", text="Score",
                     title="Resumen promedio de puntuaciones por categoría")
        st.plotly_chart(fig, use_container_width=True)

    # ============================================================
    # 🔹 Eventos personalizados
    # ============================================================
    # ============================================================
    # 🔹 Eventos personalizados A (Listas de eventos raw)
    # ============================================================
    st.header("🎯 Custom Events & Metrics")
    
    # 1. Buscar columnas de eventos raw (custom_events_*)
    custom_event_cols = [c for c in df.columns if c.startswith("custom_events_")]
    
    # 2. Buscar métricas personalizadas (con prefijo de categoría pero no estándar)
    # Definimos lo que es "estándar" para excluirlo
    std_metrics = {
        "efectividad": ["hit_ratio", "success_rate", "learning_curve_mean", "progression", "success_after_restart"],
        "eficiencia": ["avg_reaction_time_ms", "avg_task_duration_ms", "time_per_success_s", "navigation_errors"],
        "satisfaccion": ["learning_stability", "error_reduction_rate", "voluntary_play_time_s", "aid_usage", "interface_errors"],
        "presencia": ["activity_level_per_min", "first_success_time_s", "inactivity_time_s", "sound_localization_time_s", "audio_performance_gain", "spatial_coverage", "head_rotation_speed"]
    }
    
    known_std = set()
    for cat, m_list in std_metrics.items():
        for m in m_list:
            known_std.add(m)
            known_std.add(f"{cat}_{m}") # Prefixed version
            
    # Detectar custom metrics (ej: efectividad_shot_fired)
    custom_metric_cols = []
    for col in df.columns:
        if col in known_std or col in ["user_id", "group_id", "session_id", "timestamp"]:
            continue
            
        # Check if starts with a known category
        for cat in std_metrics.keys():
            if col.startswith(f"{cat}_") and not col.startswith("custom_events_") and not col.endswith("_score"):
                custom_metric_cols.append(col)
                break

    # --- Mostrar Eventos Raw ---
    if custom_event_cols:
        st.subheader("Raw Events")
        melted = df.melt(id_vars=eje_x, value_vars=custom_event_cols,
                         var_name="custom_event", value_name="count")
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")
        fig = px.bar(melted, x=eje_x, y="count", color="custom_event",
                     title="Frecuencia de Custom Events (Raw)", barmode="group", text_auto=True)
        st.plotly_chart(fig, use_container_width=True)

    # --- Mostrar Métricas Personalizadas Calculadas ---
    if custom_metric_cols:
        st.subheader("Calculated Custom Metrics")
        # Graficar cada una por separado o agrupadas
        for cm in custom_metric_cols:
             # Clean title: remove category prefix if present
             clean_title = cm
             for cat in std_metrics.keys():
                 if cm.startswith(f"{cat}_"):
                     clean_title = cm[len(cat)+1:]
                     break
             
             fig_cm = px.bar(df, x=eje_x, y=cm, color=eje_x, 
                             title=f"Custom Metric: {clean_title.replace('_', ' ').title()}", text_auto=".2f")
             st.plotly_chart(fig_cm, use_container_width=True)
             
    if not custom_event_cols and not custom_metric_cols:
        st.info("No se encontraron eventos personalizados ni métricas nuevas.")

    # ============================================================
    # 🔹 Visualizaciones Espaciales (Estáticas)
    # ============================================================
    st.header("🗺️ Análisis Espacial (Pre-generado)")
    # results_dir is inside 'results', so go up one level to 'figures/spatial'
    figures_dir = results_dir.parent / "figures" / "spatial"
    
    if figures_dir.exists():

        # Define tuples: (Title, Static Filename, Animated Filename)
        visualizations = [
            ("Trayectorias (Todos)", "Spatial_Trajectories.png", "Spatial_Trajectories.gif"),
            ("Mapa de Calor: Posición", "Spatial_Heatmap_Global.png", None),
            ("Mapa de Calor: Mirada", "Gaze_Heatmap.png", "Gaze_Heatmap.gif"),
            ("Pupilometría (Tiempo)", "Eye_Pupilometry_OverTime.png", "Eye_Pupilometry_OverTime.gif"),
            ("Mapa de Calor: Manos", "Hand_Heatmap.png", "Hand_Heatmap.gif"),
            ("Mapa de Calor: Pies", "Foot_Heatmap.png", "Foot_Heatmap.gif")
        ]
        
        # Create tabs dynamically
        tabs = st.tabs([v[0] for v in visualizations])
        
        for tab, (title, static_file, gif_file) in zip(tabs, visualizations):
            with tab:
                # Default to static
                img_path = figures_dir / static_file
                gif_path = figures_dir / gif_file if gif_file else None
                
                # Check availability
                has_static = img_path.exists()
                has_gif = gif_path and gif_path.exists()
                
                if has_gif:
                    st.markdown(f"**🎬 Animación: {title}**")
                    st.image(str(gif_path), caption=f"{title} (Animación)", use_column_width=True)
                    st.markdown("---")
                
                if has_static:
                    st.image(str(img_path), caption=f"{title} (Estático)", use_column_width=True)
                
                if not has_static and not has_gif:
                     st.info(f"Visualización no disponible: {title}")
    else:
        st.info("No se han encontrado figuras espaciales generadas. Ejecuta vr_analysis.py para crearlas.")

    # ============================================================
    # 🔹 Tabla completa
    # ============================================================
    st.header("📋 Tabla completa de métricas")
    st.dataframe(df)
    csv = df.to_csv(index=False).encode("utf-8")
    st.download_button("💾 Descargar CSV filtrado", data=csv, file_name="vr_user_metrics.csv", mime="text/csv")


if __name__ == "__main__":
    main()
