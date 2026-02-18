import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
from pathlib import Path
import numpy as np

class SpatialVisualizer:
    def __init__(self, df, output_dir):
        """
        df: DataFrame RAW con eventos (debe tener event_name, timestamp, y columnas de posición expandidas)
        output_dir: ruta donde guardar las imágenes
        """
        self.df = df
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

    def generate_all(self):
        print("[SpatialVisualizer] 🗺️ Generando gráficos espaciales...")
        try:
            self.plot_trajectories()
            self.plot_position_heatmap()
            self.plot_gaze_heatmap()
            self.plot_pupilometry()
            print(f"[SpatialVisualizer] ✅ Gráficos espaciales guardados en {self.output_dir}")
        except Exception as e:
            print(f"[SpatialVisualizer] ⚠️ Error generando gráficos espaciales: {e}")

    def plot_trajectories(self):
        """Dibuja la ruta recorrida (X vs Z) por cada usuario."""
        moves = self.df[self.df["event_name"] == "movement_frame"].copy()
        
        if "position_x" not in moves.columns or "position_z" not in moves.columns:
            return

        plt.figure(figsize=(10, 10))
        
        # Graficar una línea por sesión/usuario
        sns.lineplot(
            data=moves, 
            x="position_x", 
            y="position_z", 
            hue="user_id", 
            alpha=0.7, 
            sort=False,
            lw=1.5
        )
        
        # Marcar inicio y fin (promedio de primeros y últimos puntos para no saturar)
        # O mejor, solo mostrar trayectoria limpia.
        # Simplificación: Scatter plot de puntos finales
        last_points = moves.groupby("session_id").last().reset_index()
        sns.scatterplot(data=last_points, x="position_x", y="position_z", color="red", marker="X", s=100, label="End", zorder=5)

        plt.title("Trayectorias de Jugadores (Vista Superior - XZ)")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        plt.axis("equal")
        plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
        plt.grid(True, linestyle="--", alpha=0.5)
        
        plt.savefig(self.output_dir / "Spatial_Trajectories.png", bbox_inches="tight")
        plt.close()

    def plot_position_heatmap(self):
        """Mapa de calor de densidad de ocupación del espacio (X vs Z)."""
        moves = self.df[self.df["event_name"] == "movement_frame"]
        
        if "position_x" not in moves.columns or "position_z" not in moves.columns:
            return

        plt.figure(figsize=(10, 8))
        
        # KDE Plot (Densidad)
        try:
            sns.kdeplot(
                data=moves, 
                x="position_x", 
                y="position_z", 
                fill=True, 
                cmap="inferno", 
                thresh=0.05, 
                alpha=0.8,
                gridsize=100
            )
        except:
             # Fallback a scatter si KDE falla (pocos datos)
             sns.scatterplot(data=moves, x="position_x", y="position_z", alpha=0.3, color="orange")
        
        plt.title("Mapa de Calor: Ocupación del Espacio (Global)")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        
        plt.savefig(self.output_dir / "Spatial_Heatmap_Global.png", bbox_inches="tight")
        plt.close()

    def plot_gaze_heatmap(self):
        """
        Mapa de calor de la MIRADA (Gaze).
        Requiere eventos 'gaze_sustained' con 'world_pos_x', 'world_pos_z' 
        (o coordenadas locales de un plano si se define así).
        Asumiremos 'world_pos' flattenado como 'hit_point_x', 'hit_point_z'.
        """
        gazes = self.df[self.df["event_name"] == "gaze_frame"].copy()
        
        # Verificar columnas (log_parser aplanará Vector3 hit_position → hit_position_x, hit_position_y, hit_position_z)
        # Adaptar si el nombre en Unity es 'hit_position' o 'hit_point'
        bx = "hit_position_x" if "hit_position_x" in gazes.columns else "hit_point_x"
        bz = "hit_position_z" if "hit_position_z" in gazes.columns else "hit_point_z"
        
        if bx not in gazes.columns or bz not in gazes.columns:
            return

        plt.figure(figsize=(10, 8))
        
        try:
            sns.kdeplot(
                data=gazes, 
                x=bx, 
                y=bz, 
                fill=True, 
                cmap="viridis", 
                thresh=0.05, 
                alpha=0.8
            )
            # Superponer puntos de interés si hubiera (opcional)
        except:
             sns.scatterplot(data=gazes, x=bx, y=bz, alpha=0.5, color="purple")

        plt.title("Mapa de Calor: Atención Visual (Gaze Fixations)")
        plt.xlabel("World X (m)")
        plt.ylabel("World Z (m)")
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        
        plt.savefig(self.output_dir / "Gaze_Heatmap.png", bbox_inches="tight")
        plt.close()

    def plot_pupilometry(self):
        """Gráfico de evolución temporal del diámetro pupilar promedio."""
        eyes = self.df[self.df["event_name"] == "eye_frame"].copy()
        
        if eyes.empty: return

        # Verificar si hay datos de pupilas
        cols_to_avg = []
        if "pupil_diameter_left" in eyes.columns: cols_to_avg.append("pupil_diameter_left")
        if "pupil_diameter_right" in eyes.columns: cols_to_avg.append("pupil_diameter_right")
        
        if not cols_to_avg:
            return

        # Calcular promedio
        eyes["avg_pupil"] = eyes[cols_to_avg].mean(axis=1)
        
        # Normalizar tiempo por sesión (empezar en 0)
        # Asegurarse de que timestamp es datetime
        if not pd.api.types.is_datetime64_any_dtype(eyes["timestamp"]):
             eyes["timestamp"] = pd.to_datetime(eyes["timestamp"])
             
        eyes["time_norm"] = eyes.groupby("session_id")["timestamp"].transform(lambda x: (x - x.min()).dt.total_seconds())

        plt.figure(figsize=(12, 6))
        sns.lineplot(data=eyes, x="time_norm", y="avg_pupil", hue="user_id", alpha=0.6)
        
        plt.title("Evolución del Diámetro Pupilar")
        plt.xlabel("Tiempo de sesión (s)")
        plt.ylabel("Diámetro (mm)")
        plt.tight_layout()
        
        plt.savefig(self.output_dir / "Eye_Pupilometry_OverTime.png")
        plt.close()
