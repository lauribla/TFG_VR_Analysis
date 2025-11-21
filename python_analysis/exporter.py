import csv
import json
from pathlib import Path

class MetricsExporter:
    def __init__(self, metrics, output_dir):
        self.metrics = metrics
        self.output_dir = Path(output_dir)

    # -------------------------------------------------------------
    # JSON Export
    # -------------------------------------------------------------
    def to_json(self, filename):
        path = self.output_dir / filename
        with open(path, "w", encoding="utf-8") as f:
            json.dump(self.metrics, f, indent=4)
        print(f"[Exporter] ✅ JSON exportado en {path}")

    # -------------------------------------------------------------
    # CSV Export (Nueva versión compatible con estructura extendida)
    # -------------------------------------------------------------
    def to_csv(self, filename):
        path = self.output_dir / filename

        flat = self._flatten_dict(self.metrics)

        with open(path, "w", newline="", encoding="utf-8") as csvfile:
            writer = csv.writer(csvfile)
            writer.writerow(["metric", "value"])

            for key, value in flat.items():
                writer.writerow([key, value])

        print(f"[Exporter] ✅ CSV exportado en {path}")

    # -------------------------------------------------------------
    # Utilidad para aplanar un dict anidado
    # -------------------------------------------------------------
    def _flatten_dict(self, d, parent_key=""):
        """Convierte estructuras anidadas a:  categoria.metric : valor"""
        items = {}

        if isinstance(d, (int, float, str)):
            # Caso simple: valor suelto
            return {"value": d}

        if isinstance(d, list):
            # Convierte listas en strings o índices
            for i, v in enumerate(d):
                sub = self._flatten_dict(v, f"{parent_key}.{i}" if parent_key else str(i))
                items.update(sub)
            return items

        if isinstance(d, dict):
            for key, value in d.items():
                new_key = f"{parent_key}.{key}" if parent_key else key
                if isinstance(value, dict):
                    items.update(self._flatten_dict(value, new_key))
                elif isinstance(value, list):
                    items.update(self._flatten_dict(value, new_key))
                else:
                    items[new_key] = value
            return items

        # fallback
        return {parent_key: d}

    # -------------------------------------------------------------
    @staticmethod
    def export_multiple(results_list, names_list, mode, output_dir, filename="combined"):
        output_path = Path(output_dir) / filename

        if mode == "json":
            combined = {name: results for name, results in zip(names_list, results_list)}
            with open(output_path, "w", encoding="utf-8") as f:
                json.dump(combined, f, indent=4)
