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

    def to_json(self, filename="results.json"):
        """Exporta resultados a JSON"""
        filepath = self.output_dir / filename
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(self.results, f, indent=4, ensure_ascii=False)
        print(f"[Exporter] JSON exportado en {filepath}")

    def to_csv(self, filename="results.csv"):
        """Exporta resultados a CSV plano"""
        # Aplanamos el dict anidado en un solo nivel
        flat_results = {}
        for cat, metrics in self.results.items():
            for key, value in metrics.items():
                flat_results[f"{cat}_{key}"] = value

        df = pd.DataFrame([flat_results])
        filepath = self.output_dir / filename
        df.to_csv(filepath, index=False)
        print(f"[Exporter] CSV exportado en {filepath}")

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
            print(f"[Exporter] JSON múltiple exportado en {filepath}")

        elif mode == "csv":
            rows = []
            for id_, res in zip(ids, results_list):
                flat = {"id": id_}
                for cat, metrics in res.items():
                    for key, value in metrics.items():
                        flat[f"{cat}_{key}"] = value
                rows.append(flat)

            df = pd.DataFrame(rows)
            filepath = output_dir / f"{filename}.csv"
            df.to_csv(filepath, index=False)
            print(f"[Exporter] CSV múltiple exportado en {filepath}")
