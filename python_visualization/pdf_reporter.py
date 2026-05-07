from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle, PageBreak
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from pathlib import Path
import json
import pandas as pd
from datetime import datetime
import os


class PDFReport:
    def __init__(self, results_file, figures_dir, output_dir):
        self.results_file = Path(results_file)
        self.figures_dir = Path(figures_dir)
        # Usamos directamente el directorio de salida proporcionado
        self.export_dir = Path(output_dir)
        self.export_dir.mkdir(parents=True, exist_ok=True)

        self.output_file = self.export_dir / "final_report.pdf"

    # ============================================================
    # 🔹 Generar PDF completo
    # ============================================================
    def generate(self):
        # Detectar formato del archivo
        suffix = self.results_file.suffix.lower()
        if suffix == ".json":
            with open(self.results_file, "r", encoding="utf-8") as f:
                results = json.load(f)
            if isinstance(results, list):
                df = pd.DataFrame(results)
                mode = "agrupado"
            else:
                df = None
                mode = "global"
        elif suffix == ".csv":
            df = pd.read_csv(self.results_file)
            results = None
            mode = "agrupado"
        else:
            raise ValueError("Formato de resultados no soportado para el PDF (usa .json o .csv)")

        # Crear documento base
        doc = SimpleDocTemplate(str(self.output_file), pagesize=A4)
        styles = getSampleStyleSheet()
        elements = []

        # ============================================================
        # 🧭 Portada del informe
        # ============================================================
        elements.append(Paragraph("VR USER EVALUATION - Informe de Resultados", styles["Title"]))
        elements.append(Spacer(1, 20))
        elements.append(Paragraph(f"Generado el {datetime.now().strftime('%d/%m/%Y %H:%M')}", styles["Normal"]))
        elements.append(Spacer(1, 10))
        elements.append(Paragraph(f"Archivo de resultados: {self.results_file.name}", styles["Normal"]))
        elements.append(Spacer(1, 20))

        # ============================================================
        # 📋 Resumen general
        # ============================================================
        if df is not None and not df.empty:
            n_users = df["user_id"].nunique() if "user_id" in df.columns else "-"
            n_groups = df["group_id"].nunique() if "group_id" in df.columns else "-"
            n_sessions = df["session_id"].nunique() if "session_id" in df.columns else "-"
            summary_data = [
                ["Usuarios únicos", str(n_users)],
                ["Grupos experimentales", str(n_groups)],
                ["Sesiones registradas", str(n_sessions)],
            ]
            table = Table(summary_data, colWidths=[200, 150])
            table.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, 0), colors.lightgrey),
                ("ALIGN", (0, 0), (-1, -1), "LEFT"),
                ("GRID", (0, 0), (-1, -1), 0.25, colors.grey),
            ]))
            elements.append(table)
        elements.append(PageBreak())

        # ============================================================
        # 🔸 CATEGORÍAS OFICIALES
        # ============================================================
        # ============================================================
        # 🔸 CATEGORÍAS (Dinámicas según datos)
        # ============================================================
        # Definimos las categorías base, pero las métricas se rellenarán según lo encontrado
        known_categories = ["efectividad", "eficiencia", "satisfaccion", "presencia"]
        category_blocks = {
            f"{'🟢' if c == 'efectividad' else '🟠' if c == 'eficiencia' else '🟣' if c == 'satisfaccion' else '🔵'} {c.capitalize()}": []
            for c in known_categories}

        # Lógica para detectar métricas disponibles en DF o Resultados
        available_metrics = set()
        if df is not None:
            available_metrics = set(df.columns)
        elif results is not None:
            # En modo global, buscamos en el primer resultado disponible
            first_res = next(iter(results.values()))
            for cat in known_categories:
                if cat in first_res and isinstance(first_res[cat], dict):
                    available_metrics.update(first_res[cat].keys())

        # Mapeo manual de métricas conocidas a sus categorías para mantener orden lógico
        # Si aparecen nuevas métricas en el futuro, se añadirán si coinciden con los datos,
        # pero este mapa ayuda a ordenarlas y asignarlas a la categoría correcta.
        metric_to_cat = {
            "hit_ratio": "efectividad", "precision": "efectividad", "success_rate": "efectividad",
            "learning_curve_mean": "efectividad", "progression": "efectividad", "success_after_restart": "efectividad",
            "path_efficiency": "efectividad", "gaze_on_path_ratio": "efectividad",

            "avg_reaction_time_ms": "eficiencia", "avg_task_duration_ms": "eficiencia",
            "time_per_success_s": "eficiencia",
            "navigation_errors": "eficiencia", "aim_errors": "eficiencia",

            "learning_stability": "satisfaccion", "error_reduction_rate": "satisfaccion",
            "voluntary_play_time_s": "satisfaccion", "aid_usage": "satisfaccion", "interface_errors": "satisfaccion",

            "activity_level_per_min": "presencia", "first_success_time_s": "presencia",
            "inactivity_time_s": "presencia",
            "sound_localization_time_s": "presencia", "audio_performance_gain": "presencia"
        }

        # Rellenar category_blocks solo con métricas presentes
        for m in available_metrics:
            if m in metric_to_cat:
                cat_key = metric_to_cat[m]
                # Buscar la clave con emoji correspondiente
                for k in category_blocks:
                    if cat_key.capitalize() in k:
                        category_blocks[k].append(m)

        # Limpiar categorías vacías
        category_blocks = {k: v for k, v in category_blocks.items() if v}

        # ============================================================
        # 📊 Resultados detallados (modo agrupado)
        # ============================================================
        if mode == "agrupado" and df is not None:
            elements.append(Paragraph("Resultados Detallados por Usuario / Sesión", styles["Heading1"]))
            elements.append(Spacer(1, 10))

            # 🔹 Resumen de Scores Ponderados (global promedio)
            if all(c in df.columns for c in ["efectividad", "eficiencia", "satisfaccion", "presencia", "global_score"]):
                elements.append(Paragraph("<b>Resumen de puntuaciones ponderadas</b>", styles["Heading2"]))

                mean_scores = {}
                for col in ["efectividad_score", "eficiencia_score", "satisfaccion_score", "presencia_score",
                            "total_score", "global_score", "sus_score"]:
                    if col in df.columns:
                        mean_scores[col] = round(df[col].mean(), 2)

                data = [["Categoría", "Puntuación promedio (%)"]] + [
                    [k.replace("_score", "").capitalize(), v] for k, v in mean_scores.items()
                ]

                table = Table(data, hAlign="LEFT")
                table.setStyle([
                    ("BACKGROUND", (0, 0), (-1, 0), colors.lightgrey),
                    ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                    ("ALIGN", (1, 1), (-1, -1), "CENTER")
                ])
                elements.append(table)
                elements.append(Spacer(1, 12))
                elements.append(PageBreak())

            for _, row in df.iterrows():
                uid = row.get("user_id", "N/A")
                gid = row.get("group_id", "N/A")
                sid = row.get("session_id", "N/A")

                elements.append(Paragraph(f"<b>Usuario:</b> {uid} — <b>Grupo:</b> {gid} — <b>Sesión:</b> {sid}",
                                          styles["Heading3"]))
                elements.append(Spacer(1, 8))

                # 🔹 Mostrar los scores del usuario (ponderados desde MetricsCalculator)
                elements.append(Paragraph("Puntuaciones Ponderadas", styles["Heading4"]))

                score_keys = [
                    ("Efectividad", "efectividad_score"),
                    ("Eficiencia", "eficiencia_score"),
                    ("Satisfacción", "satisfaccion_score"),
                    ("Presencia", "presencia_score"),
                    ("Total Global", "global_score"),
                    ("Cuestionario SUS (Subjetivo)", "sus_score"),
                    ("Cuestionario Presencia (Subjetivo)", "presence_score"),
                    ("Cuestionario Satisfacción (Subjetivo)", "satisfaction_score")
                ]

                # Respaldo: intentar sin _score si no existe (retrocompatibilidad)
                final_keys = []
                for label, k in score_keys:
                    if k in df.columns:
                        final_keys.append((label, k))
                    elif k.replace("_score", "") in df.columns:
                        final_keys.append((label, k.replace("_score", "")))

                score_keys = final_keys
                data = [["Categoría", "Score (%)"]]

                for label, key in score_keys:
                    if key in row and not pd.isna(row[key]):
                        if key in ["sus_score", "presence_score", "satisfaction_score"]:
                            val = round(float(row[key]), 2)
                            data.append([label, f"{val} / 100"])
                        elif key != "global_score":
                            val = round(float(row[key]) * 100, 2)
                            data.append([label, f"{val}%"])
                        else:
                            val = round(float(row[key]) * 100, 2)
                            data.append([label, f"{val}%"])

                score_table = Table(data, hAlign="LEFT")
                score_table.setStyle([
                    ("BACKGROUND", (0, 0), (-1, 0), colors.lightgrey),
                    ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                    ("ALIGN", (1, 1), (-1, -1), "CENTER")
                ])
                elements.append(score_table)
                elements.append(Spacer(1, 10))

                # 🔹 Para cada categoría principal
                for cat, keys in category_blocks.items():
                    elements.append(Paragraph(cat, styles["Heading2"]))
                    data = [["Métrica", "Valor"]]
                    for key in keys:
                        if key in row:
                            data.append([key.replace("_", " ").title(), str(row[key])])

                    table = Table(data, repeatRows=1, colWidths=[250, 150])
                    table.setStyle(TableStyle([
                        ("BACKGROUND", (0, 0), (-1, 0), colors.grey),
                        ("TEXTCOLOR", (0, 0), (-1, 0), colors.whitesmoke),
                        ("ALIGN", (0, 0), (-1, -1), "CENTER"),
                        ("GRID", (0, 0), (-1, -1), 0.25, colors.black),
                    ]))
                    elements.append(table)
                    elements.append(Spacer(1, 8))

                elements.append(PageBreak())

        # ============================================================
        # 📈 Resultados globales (modo global JSON)
        # ============================================================
        elif mode == "global" and results is not None:
            elements.append(Paragraph("Resultados Globales por Grupo / ID", styles["Heading1"]))
            elements.append(Spacer(1, 10))

            for id_, res in results.items():
                elements.append(Paragraph(f"Resultados para: {id_}", styles["Heading2"]))
                elements.append(Spacer(1, 10))

                # 🔹 Si hay bloque de scores globales
                if "scores" in res and isinstance(res["scores"], dict):
                    elements.append(Paragraph("Resumen de puntuaciones ponderadas", styles["Heading3"]))
                    data = [["Categoría", "Valor (%)"]]
                    for k, v in res["scores"].items():
                        data.append([k.replace("_score", "").capitalize(), f"{round(v, 2)}"])
                    table = Table(data, hAlign="LEFT")
                    table.setStyle([
                        ("BACKGROUND", (0, 0), (-1, 0), colors.lightgrey),
                        ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                        ("ALIGN", (1, 1), (-1, -1), "CENTER")
                    ])
                    elements.append(table)
                    elements.append(Spacer(1, 10))

                # 🔹 Para cada categoría (efectividad, eficiencia, etc.)
                for cat, metrics in res.items():
                    if cat == "scores":
                        continue
                    elements.append(Paragraph(cat.title(), styles["Heading3"]))
                    data = [["Métrica", "Valor"]]
                    if isinstance(metrics, dict):
                        for key, value in metrics.items():
                            if isinstance(value, (list, dict)):
                                value = str(value)[:300] + ("..." if len(str(value)) > 300 else "")
                            data.append([key.replace("_", " ").title(), str(value)])
                    elif isinstance(metrics, list):
                        preview = str(metrics[:5]) + ("..." if len(metrics) > 5 else "")
                        data.append(["Lista de valores", preview])
                    else:
                        data.append(["Valor", str(metrics)])

                    table = Table(data, repeatRows=1, colWidths=[250, 150])
                    table.setStyle(TableStyle([
                        ("BACKGROUND", (0, 0), (-1, 0), colors.grey),
                        ("TEXTCOLOR", (0, 0), (-1, 0), colors.whitesmoke),
                        ("ALIGN", (0, 0), (-1, -1), "CENTER"),
                        ("GRID", (0, 0), (-1, -1), 0.25, colors.black),
                    ]))
                    elements.append(table)
                    elements.append(Spacer(1, 8))

                    cat_dir = self.figures_dir / "global"
                    if not cat_dir.exists():
                        cat_dir = self.figures_dir
                    for img in cat_dir.glob("*.png"):
                        if cat.lower() in img.name.lower():
                            elements.append(Image(str(img), width=400, height=250))
                            elements.append(Spacer(1, 10))
                elements.append(PageBreak())

        if (self.figures_dir / "agrupado").exists():
            self.figures_dir = self.figures_dir / "agrupado"
        elif (self.figures_dir / "global").exists():
            self.figures_dir = self.figures_dir / "global"

        iv_charts = list(self.figures_dir.glob("Iv_Comparison_*.png"))
        if iv_charts:
            elements.append(Paragraph("Análisis de Variables Independientes", styles["Heading1"]))
            elements.append(Spacer(1, 10))
            elements.append(
                Paragraph("Comparación de puntuaciones promedio según la Variable Independiente.", styles["Normal"]))
            elements.append(Spacer(1, 10))

            for chart in iv_charts:
                # Extraer nombre limpio del gráfico
                chart_name = chart.stem.replace("Iv_Comparison_", "").replace("_", " ").title()
                elements.append(Paragraph(chart_name, styles["Heading3"]))
                elements.append(Image(str(chart), width=400, height=250))
                elements.append(Spacer(1, 10))

            elements.append(PageBreak())

        # ============================================================
        # 🗺️ Análisis Espacial y de Mirada (NUEVO)
        # ============================================================
        # Volver a la raíz de figures para buscar 'spatial'
        # self.figures_dir apuntaba a 'agrupado' o 'global', subimos un nivel si es necesario
        root_figs = self.figures_dir.parent if self.figures_dir.name in ["agrupado", "global"] else self.figures_dir
        spatial_dir = root_figs / "spatial"

        if spatial_dir.exists():
            spatial_charts = list(spatial_dir.rglob("*.png"))
            if spatial_charts:
                elements.append(Paragraph("Análisis Espacial y de Mirada", styles["Heading1"]))
                elements.append(Spacer(1, 10))

                titles = {
                    "Spatial_Trajectories.png": "Trayectorias de Jugadores",
                    "Spatial_Heatmap_Global.png": "Mapa de Calor: Ocupación del Espacio",
                    "Gaze_Heatmap.png": "Mapa de Calor: Atención Visual (Mirada)",
                    "Gaze_Targets_BarChart.png": "Objetos Más Mirados (Gaze Cabeza)",
                    "Eye_Targets_BarChart.png": "Objetos Más Mirados (Eye Tracking Real)",
                    "Eye_Pupilometry_OverTime.png": "Evolución del Diámetro Pupilar",
                    "Hand_Heatmap.png": "Mapa de Calor: Manos",
                    "Foot_Heatmap.png": "Mapa de Calor: Pies"
                }

                for chart in spatial_charts:
                    # Incluimos el nombre de la carpeta (ej: "Audio_MapaA") en el título para diferenciar
                    folder_name = chart.parent.name

                    # Títulos explícitos para archivos conocidos
                    base_title = titles.get(chart.name)

                    # Si no está en el dict, intentar detectar bar charts por usuario
                    # Patrón: Gaze_Targets_BarChart_<uid>.png / Eye_Targets_BarChart_<uid>.png
                    if base_title is None:
                        stem = chart.stem  # ej: "Gaze_Targets_BarChart_U001"
                        if stem.startswith("Gaze_Targets_BarChart_"):
                            uid = stem.replace("Gaze_Targets_BarChart_", "", 1)
                            base_title = f"Objetos Más Mirados (Gaze) — {uid}"
                        elif stem.startswith("Eye_Targets_BarChart_"):
                            uid = stem.replace("Eye_Targets_BarChart_", "", 1)
                            base_title = f"Objetos Más Mirados (Eye Tracking) — {uid}"
                        else:
                            base_title = stem.replace("_", " ").title()

                    title = f"{base_title} - {folder_name}" if folder_name != "spatial" else base_title

                    elements.append(Paragraph(title, styles["Heading2"]))
                    elements.append(Spacer(1, 5))
                    elements.append(Image(str(chart), width=450, height=350))
                    elements.append(Spacer(1, 15))

                elements.append(PageBreak())

        # ============================================================
        # 📊 Gráficos Comparativos (Todo al final)
        # ============================================================
        elements.append(Paragraph("Gráficos Comparativos por Métrica", styles["Heading1"]))
        elements.append(Spacer(1, 10))

        # Recorremos las categorías oficiales para mantener orden
        for cat, keys in category_blocks.items():
            elements.append(Paragraph(f"Gráficos de {cat}", styles["Heading2"]))
            elements.append(Spacer(1, 10))

            category_key = cat.split(" ")[1].lower()

            # Determinar carpeta correcta (agrupado o global)
            search_dir = self.figures_dir
            if (self.figures_dir / "agrupado").exists():
                search_dir = self.figures_dir / "agrupado"
            elif (self.figures_dir / "global").exists():
                search_dir = self.figures_dir / "global"

            # Buscar imágenes que contengan el nombre de la categoría (ej: 'efectividad')
            # Ojo: visualize_groups genera 'Efectividad_hit_ratio.png'

            found_images = []
            if search_dir.exists():
                for img in sorted(search_dir.glob("*.png")):
                    # Filtramos: debe ser de esta categoría Y NO ser un Iv_Comparison ni Custom
                    name_lower = img.name.lower()
                    # Normalización simple de acentos para matching (satisfacción -> satisfaccion)
                    cat_key_norm = category_key.replace("ó", "o").replace("á", "a").replace("é", "e").replace("í",
                                                                                                              "i").replace(
                        "ú", "u")
                    name_norm = name_lower.replace("ó", "o").replace("á", "a").replace("é", "e").replace("í",
                                                                                                         "i").replace(
                        "ú", "u")

                    if cat_key_norm in name_norm and "iv_comparison" not in name_lower and "custom" not in name_lower:
                        found_images.append(img)

            if found_images:
                for img in found_images:
                    # Título de la imagen basado en el nombre del archivo
                    clean_name = img.stem.replace(category_key, "").replace("_", " ").strip().title()
                    elements.append(Paragraph(clean_name, styles["Heading3"]))
                    elements.append(Image(str(img), width=450, height=300))
                    elements.append(Spacer(1, 15))
                elements.append(PageBreak())

        # ============================================================
        # 🎯 Eventos personalizados
        # ============================================================
        elements.append(Paragraph("Eventos Personalizados", styles["Heading1"]))
        elements.append(Spacer(1, 10))

        custom_paths = list(self.figures_dir.glob("*custom*.png"))
        if not custom_paths:
            elements.append(Paragraph("No se encontraron gráficos de eventos personalizados.", styles["Normal"]))
        else:
            for path in custom_paths:
                elements.append(Paragraph(path.name, styles["Heading3"]))
                elements.append(Image(str(path), width=400, height=250))
                elements.append(Spacer(1, 10))

        # ============================================================
        # 📦 Generar el PDF final
        # ============================================================
        doc.build(elements)
        print(f"[PDFReport] ✅ Informe PDF generado en {self.output_file}")
