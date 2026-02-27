import json
import pandas as pd
import matplotlib

matplotlib.use('Agg')  # backend no interactivo para evitar errores en servidores/headless
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path


class Visualizer:
    X_LABEL = "Grupo"

    # Definición de categorías y métricas (sincronizado con dashboard y reporte)
    METRIC_CATEGORIES = {
        "Efectividad": [
            "hit_ratio", "precision", "success_rate", "learning_curve_mean",
            "progression", "success_after_restart", "attempts_per_target",
            "efectividad_score"
        ],
        "Eficiencia": [
            "avg_reaction_time_ms", "avg_task_duration_ms", "time_per_success_s",
            "navigation_errors", "aim_errors", "task_duration_success", "task_duration_fail",
            "eficiencia_score"
        ],
        "Satisfacción": [
            "retries_after_end", "voluntary_play_time_s", "aid_usage",
            "interface_errors", "learning_stability", "error_reduction_rate",
            "satisfaccion_score"
        ],
        "Presencia": [
            "inactivity_time_s", "first_success_time_s", "sound_localization_time_s",
            "activity_level_per_min", "audio_performance_gain",
            "presencia_score"
        ],
        "Global": [
            "global_score", "total_score", "sus_score"
        ],
        "Cuestionarios": [
            "sus_score", "subj_efectividad", "subj_eficiencia", "subj_satisfaccion", "subj_presencia"
        ]
    }

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

        # Normalizar scores de 0-1 a 0-100 (para que guarden proporción con el SUS score en las gráficas)
        for col in ["efectividad_score", "eficiencia_score", "satisfaccion_score", "presencia_score", "global_score", "total_score"]:
            if col in self.df.columns:
                self.df[col] = pd.to_numeric(self.df[col], errors="coerce") * 100

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
            # Silencioso para no saturar logs si no existe la métrica
            return False

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
        # print(f"[Visualizer] ✅ Figura generada: {filepath.name}")
        return True

    def _plot_metric_custom_name(self, y_col, title, ylabel, filename, palette="Blues_d"):
        """Versión de graficado con control directo del nombre de archivo output."""
        if y_col not in self.df.columns: return False
        
        x_col = self._get_x_col()
        
        plt.figure(figsize=(8, 5))
        sns.barplot(data=self.df, x=x_col, y=y_col, hue=x_col, palette=palette, legend=False)
        plt.title(title)
        plt.xlabel(self.X_LABEL)
        plt.ylabel(ylabel)
        plt.tight_layout()

        filepath = self.output_dir / filename
        plt.savefig(filepath)
        plt.close()
        return True

    # ============================================================
    # GRÁFICAS ESTÁNDAR (GENERACIÓN MASIVA)
    # ============================================================
    def generate_standard_figures(self):
        """Genera gráficos para TODAS las métricas definidas en METRIC_CATEGORIES que existan en el DF."""
        print(f"[Visualizer] 📊 Generando gráficas estándar para {self.mode}...")

        count = 0
        for category, metrics in self.METRIC_CATEGORIES.items():
            for metric in metrics:
                # Buscar columnas candidatas (con o sin prefijo de categoría si fuera necesario,
                # aunque aquí asumimos nombres planos o 'categoria_metrica')
                candidates = [metric, f"{category.lower()}_{metric}"]

                # Configurar paleta según categoría
                palette = "Blues_d"
                if category == "Eficiencia":
                    palette = "Greens_d"
                elif category == "Satisfacción":
                    palette = "Purples_d"
                elif category == "Presencia":
                    palette = "Oranges_d"
                elif category == "Global":
                    palette = "Reds_d"
                elif category == "Cuestionarios":
                    palette = "YlOrBr_d"

                # Intentar graficar
                if self._plot_metric(
                        candidates,
                        title=f"{category}: {metric.replace('_', ' ').title()}",
                        ylabel=metric.replace('_', ' ').title(),
                        palette=palette
                ):
                    count += 1

        print(f"[Visualizer] ✅ {count} gráficas estándar generadas.")
        
        # --------------------------------------------------------
        # GENERACIÓN DINÁMICA DE MÉTRICAS PERSONALIZADAS
        # --------------------------------------------------------
        print("[Visualizer] 🔍 Buscando métricas personalizadas dinámicas...")
        custom_count = 0
        
        # 1. Construir conjunto de todas las columnas "estándar" posibles
        # Esto incluye el nombre de la métrica tal cual Y con el prefijo de la categoría
        known_standard_cols = set()
        for cat, metrics in self.METRIC_CATEGORIES.items():
            for m in metrics:
                known_standard_cols.add(m)
                known_standard_cols.add(f"{cat.lower()}_{m}")

        # 2. Recorrer columnas del DataFrame
        for col in self.df.columns:
            # Si es una columna estándar conocida, ignorar
            if col in known_standard_cols:
                continue

            # Detectar si pertenece a una categoría (empieza por "efectividad_", etc.)
            matched_cat = None
            for cat in self.METRIC_CATEGORIES.keys():
                prefix = cat.lower() + "_"
                if col.startswith(prefix):
                    matched_cat = cat
                    break
            
            # Si tiene prefijo de categoría pero NO estaba en la lista de conocidas -> Es Custom
            if matched_cat:
                metric_name = col[len(matched_cat)+1:] # quitar prefix + underscore
                
                # Graficar como Custom
                filename = f"Custom_{matched_cat}_{metric_name}.png"
                title = f"{matched_cat}: {metric_name.replace('_', ' ').title()} (Custom)"
                
                if self._plot_metric_custom_name(
                    col, 
                    title=title, 
                    ylabel=metric_name.replace('_', ' '), 
                    filename=filename,
                    palette="Set2"
                ):
                    custom_count += 1

        if custom_count > 0:
            print(f"[Visualizer] ✅ {custom_count} gráficas custom dinámicas generadas.")

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
        plt.close()
        print(f"[Visualizer] ✅ Figura generada: {filepath.name}")

    # ============================================================
    # COMPARACIÓN POR VARIABLE INDEPENDIENTE
    # ============================================================
    def generate_independent_variable_comparison(self):
        """Genera gráficos comparativos agrupados por 'independent_variable'."""
        if "independent_variable" not in self.df.columns:
            return

        print("[Visualizer] ⚖️ Generando comparación por Variable Independiente...")

        iv_col = "independent_variable"
        # Asegurar tipos numéricos para agrupar
        numeric_cols = self.df.select_dtypes(include=['float64', 'int64']).columns

        # Agrupar por IV y calcular media
        grouped = self.df.groupby(iv_col)[numeric_cols].mean().reset_index()

        # Para cada score principal, generar un gráfico comparativo
        main_scores = ["efectividad_score", "eficiencia_score", "satisfaccion_score", "presencia_score", "global_score", "sus_score"]

        for score in main_scores:
            if score in grouped.columns:
                plt.figure(figsize=(8, 6))
                sns.barplot(data=grouped, x=iv_col, y=score, hue=iv_col, palette="viridis", legend=False)
                plt.title(f"Promedio de {score} por {iv_col}")
                plt.xlabel(iv_col)
                plt.ylabel("Score Promedio")
                plt.tight_layout()

                filename = f"Iv_Comparison_{score}.png"
                plt.savefig(self.output_dir / filename)
                plt.close()

        print(f"[Visualizer] ✅ Gráficos de comparación por IV generados.")

    # ============================================================
    # EJECUCIÓN COMPLETA
    # ============================================================
    def generate_all(self):
        """Genera todas las figuras posibles basándose en las columnas detectadas"""
        try:
            self.generate_standard_figures()
            self.generate_custom_events()
            self.generate_independent_variable_comparison()
            print(f"[Visualizer] ✅ Todas las figuras generadas en {self.output_dir}")
        except Exception as e:
            print(f"[Visualizer] ⚠️ Error al generar figuras: {e}")
