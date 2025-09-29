from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from pathlib import Path
import json
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

    def generate(self):
        # Cargar resultados
        with open(self.results_file, "r", encoding="utf-8") as f:
            results = json.load(f)

        doc = SimpleDocTemplate(str(self.output_file), pagesize=A4)
        styles = getSampleStyleSheet()
        elements = []

        # Título
        elements.append(Paragraph("Informe de Resultados VR", styles["Title"]))
        elements.append(Spacer(1, 20))

        # Resumen de métricas por grupo/usuario
        for id_, res in results.items():
            elements.append(Paragraph(f"Resultados para: {id_}", styles["Heading2"]))
            elements.append(Spacer(1, 10))

            # Tabla con métricas principales
            data = [["Categoría", "Métrica", "Valor"]]
            for cat, metrics in res.items():
                if isinstance(metrics, dict):
                    for key, value in metrics.items():
                        data.append([cat, key, str(value)])

            table = Table(data, repeatRows=1)
            table.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, 0), colors.grey),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.whitesmoke),
                ("ALIGN", (0, 0), (-1, -1), "CENTER"),
                ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                ("BOTTOMPADDING", (0, 0), (-1, 0), 6),
                ("GRID", (0, 0), (-1, -1), 0.5, colors.black),
            ]))
            elements.append(table)
            elements.append(Spacer(1, 20))

            # Insertar gráficos si existen
            for fig_name in [
                "hit_ratio.png",
                "reaction_time.png",
                "success_rate.png",
                "activity_level.png",
                "custom_events.png"
            ]:
                fig_path = self.figures_dir / fig_name
                if fig_path.exists():
                    elements.append(Image(str(fig_path), width=400, height=250))
                    elements.append(Spacer(1, 15))

        # Construir PDF
        doc.build(elements)
        print(f"[PDFReport] Informe generado en {self.output_file}")
