import json
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path

class Visualizer:
    X_LABEL = "Grupo / Usuario"

    def __init__(self, input_file, output_dir="figures"):
        """
        input_file: archivo JSON o CSV exportado por MetricsExporter (global o agrupado)
        output_dir: carpeta donde guardar los gráficos
        """
        self.input_file = Path(input_file)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # --- Detectar y cargar el tipo de archivo ---
        if input_file.endswith(".json"):
            with open(input_file, "r", encoding="utf-8") as f:
                self.results = json.load(f)
            self.df = self._json_to_dataframe(self.results)
        elif input_file.endswith(".csv"):
            self.df = pd.read_csv(input_file)
        else:
            raise ValueError("Formato de archivo no soportado (usa .json o .csv)")

        # --- Detectar automáticamente columnas de agrupación ---
        self.mode = self._detect_mode()

        print(f"[Visualizer] ✅ Datos cargados desde {input_file}")
        print(f"[Visualizer] Modo detectado: {self.mode}")

    # ============================================================
    # UTILIDADES INTERNAS
    # ============================================================
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

    def _detect_mode(self):
        """Determina si los datos provienen del bloque global o agrupado"""
        cols = self.df.columns
        if {"user_id", "group_id", "session_id"}.issubset(cols):
            return "agrupado"  # DataFrame de MetricsCalculator.compute_grouped_metrics()
        return "global"

    def _get_x_col(self):
        """Devuelve la columna que se usará como eje X según el tipo de datos"""
        if self.mode == "agrupado":
            if "group_id" in self.df.columns:
                return "group_id"
            elif "user_id" in self.df.columns:
                return "user_id"
        return "id"  # caso global

    # ============================================================
    # VISUALIZACIONES DE INDICADORES OFICIALES
    # ============================================================
    def plot_hit_ratio(self):
        plt.figure(figsize=(8, 5))
        x_col = self._get_x_col()
        sns.barplot(data=self.df, x=x_col, y="hit_ratio", hue=x_col,
                    palette="Blues_d", legend=False)
        plt.title("Efectividad: Ratio de aciertos")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Hit Ratio")
        plt.ylim(0, 1)
        plt.tight_layout()
        plt.savefig(self.output_dir / "hit_ratio.png")
        plt.close()

    def plot_reaction_time(self):
        plt.figure(figsize=(8, 5))
        x_col = self._get_x_col()
        y_col = "avg_reaction_time_ms" if self.mode == "agrupado" else "eficiencia_avg_reaction_time_ms"
        sns.barplot(data=self.df, x=x_col, y=y_col, hue=x_col,
                    palette="Greens_d", legend=False)
        plt.title("Eficiencia: Tiempo medio de reacción")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Tiempo medio (ms)")
        plt.tight_layout()
        plt.savefig(self.output_dir / "reaction_time.png")
        plt.close()

    def plot_task_success(self):
        plt.figure(figsize=(8, 5))
        x_col = self._get_x_col()
        y_col = "success_rate" if self.mode == "agrupado" else "efectividad_success_rate"
        sns.barplot(data=self.df, x=x_col, y=y_col, hue=x_col,
                    palette="Purples_d", legend=False)
        plt.title("Efectividad: Porcentaje de tareas completadas")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Success Rate")
        plt.ylim(0, 1)
        plt.tight_layout()
        plt.savefig(self.output_dir / "success_rate.png")
        plt.close()

    def plot_activity_level(self):
        plt.figure(figsize=(8, 5))
        x_col = self._get_x_col()
        y_col = "activity_level" if self.mode == "agrupado" else "presencia_activity_level_per_min"
        sns.barplot(data=self.df, x=x_col, y=y_col, hue=x_col,
                    palette="Oranges_d", legend=False)
        plt.title("Presencia: Nivel de actividad en el entorno")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Eventos por minuto")
        plt.tight_layout()
        plt.savefig(self.output_dir / "activity_level.png")
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

        id_col = self._get_x_col()
        melted = self.df.melt(id_vars=id_col, value_vars=custom_cols,
                              var_name="custom_event", value_name="count")
        melted["custom_event"] = melted["custom_event"].str.replace("custom_events_", "")

        plt.figure(figsize=(10, 6))
        sns.barplot(data=melted, x=id_col, y="count", hue="custom_event", palette="Set2")
        plt.title("Eventos personalizados registrados")
        plt.xlabel(self.X_LABEL)
        plt.ylabel("Frecuencia")
        plt.legend(title="Evento personalizado", bbox_to_anchor=(1.05, 1), loc="upper left")
        plt.tight_layout()
        plt.savefig(self.output_dir / "custom_events.png", bbox_inches="tight")
        plt.close()

    # ============================================================
    # EJECUCIÓN COMPLETA
    # ============================================================
    def generate_all(self):
        """
        Genera todos los gráficos estándar.
        """
        try:
            self.plot_hit_ratio()
            self.plot_reaction_time()
            self.plot_task_success()
            self.plot_activity_level()
            self.plot_custom_events()
            print(f"[Visualizer] ✅ Todas las figuras generadas en {self.output_dir}")
        except Exception as e:
            print(f"[Visualizer] ⚠️ Error al generar figuras: {e}")
