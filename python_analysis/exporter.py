import json
import pandas as pd
from pathlib import Path

class MetricsExporter:
    def __init__(self, results, output_dir="exports"):
        """
        results: dict generado por MetricsCalculator.compute_all()
        output_dir: carpeta donde guardar los archivos
        """
        self.results = results
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

    # ============================================================
    #  Exportaciones básicas (globales)
    # ============================================================
    def to_json(self, filename="results.json"):
        """Exporta resultados a JSON"""
        filepath = self.output_dir / filename
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(self.results, f, indent=4, ensure_ascii=False)
        print(f"[Exporter] ✅ JSON exportado en {filepath}")

    def to_csv(self, filename="results.csv"):
        """Exporta resultados globales a CSV plano"""
        flat_results = {}
        for cat, metrics in self.results.items():
            # Evitar que la lista 'agrupado_por_usuario_y_sesion' entre aquí
            if cat == "agrupado_por_usuario_y_sesion":
                continue
            for key, value in metrics.items():
                flat_results[f"{cat}_{key}"] = value

        df = pd.DataFrame([flat_results])
        filepath = self.output_dir / filename
        df.to_csv(filepath, index=False)
        print(f"[Exporter] ✅ CSV exportado en {filepath}")

    # ============================================================
    #  Exportación automática del bloque agrupado
    # ============================================================
    def export_grouped(self, filename="grouped_metrics.csv"):
        """
        Exporta la lista de métricas agrupadas (por usuario/sesión) en CSV.
        Solo se ejecuta si existe 'agrupado_por_usuario_y_sesion' en results.
        """
        grouped_data = self.results.get("agrupado_por_usuario_y_sesion", [])
        if not grouped_data:
            print("[Exporter] ⚠️ No hay datos agrupados para exportar.")
            return None

        df = pd.DataFrame(grouped_data)
        filepath = self.output_dir / filename
        df.to_csv(filepath, index=False)
        print(f"[Exporter] ✅ CSV agrupado exportado en {filepath}")
        return df

    # ============================================================
    #  Exportación múltiple (varios usuarios o grupos)
    # ============================================================
    @staticmethod
    def export_multiple(results_list, ids, mode="csv", output_dir="exports", filename="results_all"):
        """
        Exporta resultados de varios usuarios/sesiones/grupos en un solo archivo.
        - results_list: lista de dicts de resultados
        - ids: lista de IDs asociados (ej. user_id o group_id)
        - mode: "csv" o "json"
        """
        output_dir = Path(output_dir)
        output_dir.mkdir(parents=True, exist_ok=True)

        if mode == "json":
            combined = {id_: res for id_, res in zip(ids, results_list)}
            filepath = output_dir / f"{filename}.json"
            with open(filepath, "w", encoding="utf-8") as f:
                json.dump(combined, f, indent=4, ensure_ascii=False)
            print(f"[Exporter] ✅ JSON múltiple exportado en {filepath}")

        elif mode == "csv":
            rows = []
            for id_, res in zip(ids, results_list):
                flat = {"id": id_}
                for cat, metrics in res.items():
                    # Ignorar bloque agrupado dentro de cada resultado
                    if cat == "agrupado_por_usuario_y_sesion":
                        continue
                    for key, value in metrics.items():
                        flat[f"{cat}_{key}"] = value
                rows.append(flat)

            df = pd.DataFrame(rows)
            filepath = output_dir / f"{filename}.csv"
            df.to_csv(filepath, index=False)
            print(f"[Exporter] ✅ CSV múltiple exportado en {filepath}")
