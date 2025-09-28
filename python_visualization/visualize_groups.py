import json
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path

class Visualizer:
    X_LABEL = "Grupo / Usuario"

    def __init__(self, input_file, output_dir="figures"):
        """
        input_file: archivo JSON o CSV exportado por MetricsExporter.export_multiple()
        output_dir: carpeta donde guardar los gráficos
        """
        self.input_file = Path(input_file)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        if input_file.endswith(".json"):
            with open(input_file, "r", encoding="utf-8") as f:
                self.results = json.load(f)
            self.df = self._json_to_dataframe(self.results)
        elif input_file.endswith(".csv"):
            self.df = pd.read_csv(input_file)
        else:
            raise ValueError("Formato de archivo no soportado (usa .json o .csv)")

    def _json_to_dataframe(self, results):
        """Convierte resultados JSON jerárquicos en DataFrame plano"""
        rows = []
        for id_, res in results.items():
            flat = {"id": id_}
            for cat, metrics in res.items():
                if isinstance(metrics, dict):
                    for key, value in metrics.items():
                        flat[f"{cat}_{key}"] = value
            rows.append(flat)
        return pd.DataFrame(rows)

    # ============================================================
    # VISUALIZACIONES DE INDICADORES OFICIALES
    # ============================================================
    def plot_hit_ratio(self):
        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x="id", y="efectividad_hit_ratio", hue="id",
                    palette="Blues_d", legend=False)
        plt.title("Efectividad: Ratio de aciertos")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Hit Ratio")
        plt.ylim(0, 1)
        filepath = self.output_dir / "hit_ratio.png"
        plt.savefig(filepath)
        plt.close()

    def plot_reaction_time(self):
        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x="id", y="eficiencia_avg_reaction_time_ms", hue="id",
                    palette="Greens_d", legend=False)
        plt.title("Eficiencia: Tiempo medio de reacción")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Tiempo medio (ms)")
        filepath = self.output_dir / "reaction_time.png"
        plt.savefig(filepath)
        plt.close()

    def plot_task_success(self):
        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x="id", y="efectividad_success_rate", hue="id",
                    palette="Purples_d", legend=False)
        plt.title("Efectividad: Porcentaje de tareas completadas")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Success Rate")
        plt.ylim(0, 1)
        filepath = self.output_dir / "success_rate.png"
        plt.savefig(filepath)
        plt.close()

    def plot_activity_level(self):
        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x="id", y="presencia_activity_level_per_min", hue="id",
                    palette="Oranges_d", legend=False)
        plt.title("Presencia: Nivel de actividad en el entorno")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Eventos por minuto")
        filepath = self.output_dir / "activity_level.png"
        plt.savefig(filepath)
        plt.close()

    # ============================================================
    # VISUALIZACIÓN DE CUSTOM EVENTS
    # ============================================================
    def plot_custom_events(self):
        """
        Genera un gráfico de barras apiladas con la frecuencia de custom events por grupo/usuario.
        """
        custom_cols = [c for c in self.df.columns if c.startswith("custom_events_")]
        if not custom_cols:
            print("[Visualizer] No hay eventos personalizados en los datos.")
            return

        melted = self.df.melt(id_vars="id", value_vars=custom_cols,
                              var_name="custom_event", value_name="count")
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")

        plt.figure(figsize=(10, 6))
        sns.barplot(data=melted, x="id", y="count", hue="custom_event", palette="Set2")
        plt.title("Eventos personalizados registrados")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Frecuencia")
        plt.legend(title="Evento personalizado", bbox_to_anchor=(1.05, 1), loc="upper left")
        filepath = self.output_dir / "custom_events.png"
        plt.savefig(filepath, bbox_inches="tight")
        plt.close()

    # ============================================================
    # EJECUCIÓN COMPLETA
    # ============================================================
    def generate_all(self):
        self.plot_hit_ratio()
        self.plot_reaction_time()
        self.plot_task_success()
        self.plot_activity_level()
        self.plot_custom_events()
