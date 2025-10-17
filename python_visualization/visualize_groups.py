import json
import pandas as pd
import matplotlib
matplotlib.use('Agg')  # backend no interactivo para evitar errores en servidores/headless
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path


class Visualizer:
    X_LABEL = "Grupo / Usuario"

    def __init__(self, input_file, output_dir="figures"):
        """
        input_file: archivo JSON o CSV exportado por MetricsExporter
        output_dir: carpeta donde guardar los gráficos
        """
        self.input_file = Path(input_file)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # --- Cargar archivo ---
        if input_file.endswith(".json"):
            with open(input_file, "r", encoding="utf-8") as f:
                self.results = json.load(f)
            self.df = self._json_to_dataframe(self.results)
        elif input_file.endswith(".csv"):
            self.df = pd.read_csv(input_file)
        else:
            raise ValueError("Formato de archivo no soportado (usa .json o .csv)")

        # --- Detectar modo ---
        self.mode = self._detect_mode()
        print(f"[Visualizer] ✅ Datos cargados desde {input_file}")
        print(f"[Visualizer] Modo detectado: {self.mode}")

    # ============================================================
    # UTILIDADES
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
        cols = set(self.df.columns)
        if {"user_id", "group_id", "session_id"}.issubset(cols):
            return "agrupado"
        return "global"

    def _get_x_col(self):
        """Devuelve la columna más adecuada para el eje X"""
        if self.mode == "agrupado":
            if "group_id" in self.df.columns:
                return "group_id"
            elif "user_id" in self.df.columns:
                return "user_id"
        return "id"

    def _find_col(self, *candidates):
        """Busca la primera columna existente en el DataFrame"""
        for c in candidates:
            if c in self.df.columns:
                return c
        return None

    # ============================================================
    # FUNCIÓN GENÉRICA DE GRAFICADO
    # ============================================================
    def _plot_metric(self, y_candidates, title, ylabel, palette="Blues_d", ylim=None):
        y_col = self._find_col(*y_candidates)
        if y_col is None:
            print(f"[Visualizer] ⚠️ Métrica no encontrada: {y_candidates}")
            return

        x_col = self._get_x_col()
        if x_col not in self.df.columns:
            print(f"[Visualizer] ⚠️ No se encontró columna de eje X ({x_col}).")
            return

        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x=x_col, y=y_col, hue=x_col, palette=palette, legend=False)
        plt.title(title)
        plt.xlabel(self.X_LABEL)
        plt.ylabel(ylabel)
        if ylim:
            plt.ylim(ylim)
        plt.tight_layout()

        safe_name = y_col.replace("/", "_").replace("\\", "_").replace(" ", "_")
        filepath = self.output_dir / f"{safe_name}.png"
        plt.savefig(filepath)
        plt.close()
        print(f"[Visualizer] ✅ Figura generada: {filepath.name}")

    # ============================================================
    # GRÁFICAS ESTÁNDAR
    # ============================================================
    def generate_standard_figures(self):
        """Genera los gráficos de los indicadores más comunes, si existen"""
        self._plot_metric(
            ["hit_ratio", "efectividad_hit_ratio"],
            "Efectividad: Ratio de aciertos",
            "Hit Ratio",
            palette="Blues_d",
            ylim=(0, 1)
        )

        self._plot_metric(
            ["success_rate", "efectividad_success_rate"],
            "Efectividad: Porcentaje de tareas completadas",
            "Success Rate",
            palette="Purples_d",
            ylim=(0, 1)
        )

        self._plot_metric(
            ["avg_reaction_time_ms", "eficiencia_avg_reaction_time_ms"],
            "Eficiencia: Tiempo medio de reacción",
            "Tiempo medio (ms)",
            palette="Greens_d"
        )

        self._plot_metric(
            ["avg_task_duration_ms", "eficiencia_avg_task_duration_ms"],
            "Eficiencia: Duración media de tarea",
            "Duración (ms)",
            palette="Greens_d"
        )

        self._plot_metric(
            ["activity_level", "presencia_activity_level_per_min"],
            "Presencia: Nivel de actividad en el entorno",
            "Eventos por minuto",
            palette="Oranges_d"
        )

    # ============================================================
    # CUSTOM EVENTS DINÁMICOS
    # ============================================================
    def generate_custom_events(self):
        """Genera gráficos de todos los eventos personalizados presentes"""
        custom_cols = [c for c in self.df.columns if c.startswith("custom_events_")]
        if not custom_cols:
            print("[Visualizer] ℹ️ No hay eventos personalizados en los datos.")
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
        filepath = self.output_dir / "custom_events.png"
        plt.savefig(filepath, bbox_inches="tight")
        plt.close()
        print(f"[Visualizer] ✅ Figura generada: {filepath.name}")

    # ============================================================
    # EJECUCIÓN COMPLETA
    # ============================================================
    def generate_all(self):
        """Genera todas las figuras posibles basándose en las columnas detectadas"""
        try:
            self.generate_standard_figures()
            self.generate_custom_events()
            print(f"[Visualizer] ✅ Todas las figuras generadas en {self.output_dir}")
        except Exception as e:
            print(f"[Visualizer] ⚠️ Error al generar figuras: {e}")
