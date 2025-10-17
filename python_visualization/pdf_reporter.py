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
    def __init__(self, results_file, figures_dir="figures", base_dir="pruebas"):
        self.results_file = Path(results_file)
        self.figures_dir = Path(figures_dir)
        self.base_dir = Path(base_dir)

        # Crear carpeta con timestamp para guardar el PDF
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        self.export_dir = self.base_dir / f"exports_{timestamp}"
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
        category_blocks = {
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
            ],
        }

        # ============================================================
        # 📊 Resultados detallados (modo agrupado)
        # ============================================================
        if mode == "agrupado" and df is not None:
            elements.append(Paragraph("Resultados Detallados por Usuario / Sesión", styles["Heading1"]))
            elements.append(Spacer(1, 10))

            for _, row in df.iterrows():
                uid = row.get("user_id", "N/A")
                gid = row.get("group_id", "N/A")
                sid = row.get("session_id", "N/A")

                elements.append(Paragraph(f"<b>Usuario:</b> {uid} — <b>Grupo:</b> {gid} — <b>Sesión:</b> {sid}", styles["Heading3"]))
                elements.append(Spacer(1, 8))

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

                    # 🖼️ Añadir imágenes de la categoría
                    category_key = cat.split(" ")[1].lower()
                    cat_dir = self.figures_dir / "agrupado"
                    if not cat_dir.exists():
                        cat_dir = self.figures_dir
                    for img in cat_dir.glob("*.png"):
                        if category_key in img.name.lower():
                            elements.append(Image(str(img), width=400, height=250))
                            elements.append(Spacer(1, 10))
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

                # 🔹 Para cada categoría (efectividad, eficiencia, etc.)
                for cat, metrics in res.items():
                    elements.append(Paragraph(cat.title(), styles["Heading3"]))
                    data = [["Métrica", "Valor"]]
                    # 🔹 Manejo seguro de métricas, incluso si hay listas o estructuras anidadas
                    if isinstance(metrics, dict):
                        for key, value in metrics.items():
                            if isinstance(value, (list, dict)):
                                # Convertir listas o dicts en texto legible (truncado si son largos)
                                value = str(value)[:300] + ("..." if len(str(value)) > 300 else "")
                            data.append([key.replace("_", " ").title(), str(value)])
                    elif isinstance(metrics, list):
                        # Si metrics es una lista (p. ej. 'learning_curve'), mostrar la longitud y primeros valores
                        preview = str(metrics[:5]) + ("..." if len(metrics) > 5 else "")
                        data.append(["Lista de valores", preview])
                    else:
                        # Si es un valor simple
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

                    # 🖼️ Añadir gráficos globales debajo de cada categoría
                    cat_dir = self.figures_dir / "global"
                    if not cat_dir.exists():
                        cat_dir = self.figures_dir
                    for img in cat_dir.glob("*.png"):
                        if cat.lower() in img.name.lower():
                            elements.append(Image(str(img), width=400, height=250))
                            elements.append(Spacer(1, 10))
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
