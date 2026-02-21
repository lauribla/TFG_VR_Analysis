import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
from pathlib import Path
import numpy as np
import io
from PIL import Image

class SpatialVisualizer:
    def __init__(self, df, output_dir, play_area_width=None, play_area_depth=None):
        """
        df: DataFrame RAW con eventos (debe tener event_name, timestamp, y columnas de posición expandidas)
        output_dir: ruta donde guardar las imágenes
        """
        self.df = df
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.play_area_width = play_area_width
        self.play_area_depth = play_area_depth

    def _draw_play_area(self, ax=None):
        if self.play_area_width is not None and self.play_area_depth is not None:
            if self.play_area_width > 0 and self.play_area_depth > 0:
                ax = ax or plt.gca()
                import matplotlib.patches as patches
                w = self.play_area_width
                d = self.play_area_depth
                # Centro en 0,0, así que la esquina inferior izq es -w/2, -d/2
                rect = patches.Rectangle((-w/2, -d/2), w, d, linewidth=2, edgecolor='red', facecolor='none', linestyle='--', label="Límites Zona VR", zorder=4)
                ax.add_patch(rect)

    def generate_all(self):
        print("[SpatialVisualizer] 🗺️ Generando gráficos espaciales...")
        try:
            self.plot_trajectories()
            self.plot_position_heatmap()
            self.plot_gaze_heatmap()
            self.plot_pupilometry()
            
            self.plot_hand_heatmap()
            self.plot_foot_heatmap()
            
            # Generar GIFs
            print("[SpatialVisualizer] 🎬 Generando animaciones (GIF)...")
            self.plot_trajectory_gif()
            self.plot_gaze_heatmap_gif()
            self.plot_pupilometry_gif()
            
            self.plot_hand_heatmap_gif()
            self.plot_foot_heatmap_gif()
            
            print(f"[SpatialVisualizer] ✅ Gráficos espaciales guardados en {self.output_dir}")
        except Exception as e:
            print(f"[SpatialVisualizer] ⚠️ Error generando gráficos espaciales: {e}")
            import traceback
            traceback.print_exc()

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
        last_points = moves.groupby("session_id").last().reset_index()
        sns.scatterplot(data=last_points, x="position_x", y="position_z", color="red", marker="X", s=100, label="End", zorder=5)

        plt.title("Trayectorias de Jugadores (Vista Superior - XZ)")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        self._draw_play_area()
        plt.axis("equal")
        plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
        plt.grid(True, linestyle="--", alpha=0.5)
        
        plt.savefig(self.output_dir / "Spatial_Trajectories.png", bbox_inches="tight")
        plt.close()

    def plot_trajectory_gif(self, max_frames=60):
        """Genera un GIF animado de la trayectoria."""
        moves = self.df[self.df["event_name"] == "movement_frame"].copy()
        if "position_x" not in moves.columns or "position_z" not in moves.columns:
            return

        # Ordenar por tiempo
        if not pd.api.types.is_datetime64_any_dtype(moves["timestamp"]):
            moves["timestamp"] = pd.to_datetime(moves["timestamp"])
        moves = moves.sort_values("timestamp")

        sessions = moves["session_id"].unique()
        
        # Crear frames
        frames = []
        
        # Vamos a samplear el tiempo globalmente para sincronizar
        # Tomamos el tiempo relativo min y max
        # Para simplificar, iteraremos por porcentaje del total de puntos (0% a 100%)
        # Esto asume que los puntos estan distribuidos uniformemente en el tiempo, lo cual es aprox cierto si el framerate es constante
        
        step_size = max(1, len(moves) // max_frames)
        indices = list(range(step_size, len(moves), step_size))
        if len(moves) - 1 not in indices:
            indices.append(len(moves) - 1)

        print(f"[SpatialVisualizer] Generando {len(indices)} frames para Trajectory GIF...")

        # Pre-calcular limites para mantener la escala fija
        x_min, x_max = moves["position_x"].min(), moves["position_x"].max()
        z_min, z_max = moves["position_z"].min(), moves["position_z"].max()
        margin = 1.0

        for idx in indices:
            current_data = moves.iloc[:idx]
            
            fig, ax = plt.subplots(figsize=(8, 8))
            
            # Dibujar trayectoria acumulada
            sns.lineplot(
                data=current_data, 
                x="position_x", 
                y="position_z", 
                hue="user_id", 
                alpha=0.8, 
                sort=False,
                lw=2,
                ax=ax,
                legend=False
            )
            
            # Dibujar punto actual (cabeza de la serptiente)
            # El ultimo punto de cada sesion en current_data
            heads = current_data.groupby("session_id").last().reset_index()
            sns.scatterplot(
                data=heads,
                x="position_x", 
                y="position_z", 
                hue="user_id",
                s=100,
                marker="o",
                ax=ax,
                legend=False
            )

            ax.set_xlim(x_min - margin, x_max + margin)
            ax.set_ylim(z_min - margin, z_max + margin)
            ax.set_title("Evolución de Trayectorias")
            ax.set_xlabel("X (m)")
            ax.set_ylabel("Z (m)")
            ax.grid(True, linestyle="--", alpha=0.3)
            self._draw_play_area(ax)
            ax.axis("equal")

            # Guardar en buffer
            buf = io.BytesIO()
            plt.savefig(buf, format='png', bbox_inches='tight')
            buf.seek(0)
            frames.append(Image.open(buf))
            plt.close(fig)

        if frames:
            frames[0].save(
                self.output_dir / "Spatial_Trajectories.gif",
                save_all=True,
                append_images=frames[1:],
                optimize=True,
                duration=200, # ms por frame
                loop=0
            )

    def plot_position_heatmap(self):
        """Mapa de calor de densidad de ocupación del espacio (X vs Z)."""
        moves = self.df[self.df["event_name"] == "movement_frame"]
        
        if "position_x" not in moves.columns or "position_z" not in moves.columns:
            return

        plt.figure(figsize=(10, 8))
        
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
             sns.scatterplot(data=moves, x="position_x", y="position_z", alpha=0.3, color="orange")
        
        plt.title("Mapa de Calor: Ocupación del Espacio (Global)")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        self._draw_play_area()
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        
        plt.savefig(self.output_dir / "Spatial_Heatmap_Global.png", bbox_inches="tight")
        plt.close()

    def plot_gaze_heatmap(self):
        """Mapa de calor de la MIRADA (Gaze)."""
        gazes = self.df[self.df["event_name"] == "gaze_frame"].copy()
        
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
        except:
             sns.scatterplot(data=gazes, x=bx, y=bz, alpha=0.5, color="purple")

        plt.title("Mapa de Calor: Atención Visual (Gaze Fixations)")
        plt.xlabel("World X (m)")
        plt.ylabel("World Z (m)")
        self._draw_play_area()
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        
        plt.savefig(self.output_dir / "Gaze_Heatmap.png", bbox_inches="tight")
        plt.close()

    def plot_gaze_heatmap_gif(self, max_frames=60):
        """Genera GIF de la evolución de la mirada."""
        gazes = self.df[self.df["event_name"] == "gaze_frame"].copy()
        
        bx = "hit_position_x" if "hit_position_x" in gazes.columns else "hit_point_x"
        bz = "hit_position_z" if "hit_position_z" in gazes.columns else "hit_point_z"
        
        if bx not in gazes.columns or bz not in gazes.columns:
            return

        if not pd.api.types.is_datetime64_any_dtype(gazes["timestamp"]):
            gazes["timestamp"] = pd.to_datetime(gazes["timestamp"])
        gazes = gazes.sort_values("timestamp")

        # Limitar rango si hay outliers extremos (opcional, pero buena practica en gaze)
        # x_min, x_max = gazes[bx].quantile(0.01), gazes[bx].quantile(0.99)
        x_min, x_max = gazes[bx].min(), gazes[bx].max()
        z_min, z_max = gazes[bz].min(), gazes[bz].max()

        step_size = max(1, len(gazes) // max_frames)
        indices = list(range(step_size, len(gazes), step_size))
        if len(gazes) - 1 not in indices:
            indices.append(len(gazes) - 1)

        print(f"[SpatialVisualizer] Generando {len(indices)} frames para Gaze GIF...")
        frames = []

        for idx in indices:
            current_data = gazes.iloc[:idx]
            
            fig, ax = plt.subplots(figsize=(8, 8))
            
            # Usaremos scatter acumulativo para simular "heatmap" construyéndose
            # alpha bajo para que la superposición cree densidad
            sns.scatterplot(
                data=current_data, 
                x=bx, 
                y=bz, 
                alpha=0.1, 
                color="purple",
                s=50,
                edgecolor=None,
                ax=ax
            )
            
            ax.set_xlim(x_min, x_max)
            ax.set_ylim(z_min, z_max)
            ax.set_title("Atención Visual Acumulada")
            ax.set_xlabel("World X (m)")
            ax.set_ylabel("World Z (m)")
            self._draw_play_area(ax)
            ax.axis("equal")
            ax.grid(True, linestyle="--", alpha=0.3)
            ax.axis("equal")
            ax.grid(True, alpha=0.3)
            
            buf = io.BytesIO()
            plt.savefig(buf, format='png', bbox_inches='tight')
            buf.seek(0)
            frames.append(Image.open(buf))
            plt.close(fig)

        if frames:
            frames[0].save(
                self.output_dir / "Gaze_Heatmap.gif",
                save_all=True,
                append_images=frames[1:],
                optimize=True,
                duration=200,
                loop=0
            )

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

    def plot_pupilometry_gif(self, max_frames=60):
        """Genera GIF de la evolución del diámetro pupilar."""
        eyes = self.df[self.df["event_name"] == "eye_frame"].copy()
        
        if eyes.empty: return

        cols_to_avg = []
        if "pupil_diameter_left" in eyes.columns: cols_to_avg.append("pupil_diameter_left")
        if "pupil_diameter_right" in eyes.columns: cols_to_avg.append("pupil_diameter_right")
        
        if not cols_to_avg: return

        eyes["avg_pupil"] = eyes[cols_to_avg].mean(axis=1)
        
        if not pd.api.types.is_datetime64_any_dtype(eyes["timestamp"]):
             eyes["timestamp"] = pd.to_datetime(eyes["timestamp"])
             
        eyes["time_norm"] = eyes.groupby("session_id")["timestamp"].transform(lambda x: (x - x.min()).dt.total_seconds())
        eyes = eyes.sort_values("time_norm")

        step_size = max(1, len(eyes) // max_frames)
        indices = list(range(step_size, len(eyes), step_size))
        if len(eyes) - 1 not in indices: indices.append(len(eyes) - 1)

        print(f"[SpatialVisualizer] Generando {len(indices)} frames para Pupilometry GIF...")
        
        # Pre-calc limites
        y_min, y_max = eyes["avg_pupil"].min(), eyes["avg_pupil"].max()
        x_max = eyes["time_norm"].max()
        
        frames = []
        for idx in indices:
            current_data = eyes.iloc[:idx]
            
            fig, ax = plt.subplots(figsize=(10, 5))
            sns.lineplot(data=current_data, x="time_norm", y="avg_pupil", hue="user_id", alpha=0.8, ax=ax)
            
            ax.set_ylim(y_min * 0.9, y_max * 1.1)
            ax.set_xlim(0, x_max)
            ax.set_title("Evolución del Diámetro Pupilar (Tiempo Real)")
            ax.set_xlabel("Tiempo (s)")
            ax.set_ylabel("Diámetro (mm)")
            ax.grid(True, linestyle="--", alpha=0.3)
            
            buf = io.BytesIO()
            plt.savefig(buf, format='png', bbox_inches='tight')
            buf.seek(0)
            frames.append(Image.open(buf))
            plt.close(fig)

        if frames:
            frames[0].save(
                self.output_dir / "Eye_Pupilometry_OverTime.gif",
                save_all=True,
                append_images=frames[1:],
                optimize=True,
                duration=200,
                loop=0
            )

    def plot_hand_heatmap(self):
        """Mapa de calor de posición de manos (X vs Z)."""
        hands = self.df[self.df["event_name"] == "hand_movement"].copy()
        
        if "position_x" not in hands.columns or "position_z" not in hands.columns or hands.empty:
            return

        plt.figure(figsize=(10, 8))
        
        try:
            sns.kdeplot(
                data=hands, 
                x="position_x", 
                y="position_z", 
                fill=True, 
                cmap="YlGnBu", 
                thresh=0.05, 
                alpha=0.8
            )
        except:
             sns.scatterplot(data=hands, x="position_x", y="position_z", alpha=0.3, hue="hand" if "hand" in hands.columns else None)
        
        plt.title("Mapa de Calor: Ocupación de Manos")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        self._draw_play_area()
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        if "hand" in hands.columns and plt.gca().get_legend() is not None:
            plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
            
        plt.savefig(self.output_dir / "Hand_Heatmap.png", bbox_inches="tight")
        plt.close()

    def plot_foot_heatmap(self):
        """Mapa de calor de posición de pies (X vs Z)."""
        feet = self.df[self.df["event_name"] == "foot_movement"].copy()
        
        if "position_x" not in feet.columns or "position_z" not in feet.columns or feet.empty:
            return

        plt.figure(figsize=(10, 8))
        
        try:
            sns.kdeplot(
                data=feet, 
                x="position_x", 
                y="position_z", 
                fill=True, 
                cmap="YlOrRd", 
                thresh=0.05, 
                alpha=0.8
            )
        except:
             sns.scatterplot(data=feet, x="position_x", y="position_z", alpha=0.3, hue="foot" if "foot" in feet.columns else None)
        
        plt.title("Mapa de Calor: Ocupación de Pies")
        plt.xlabel("X (m)")
        plt.ylabel("Z (m)")
        self._draw_play_area()
        plt.axis("equal")
        plt.grid(True, alpha=0.3)
        if "foot" in feet.columns and plt.gca().get_legend() is not None:
            plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
            
        plt.savefig(self.output_dir / "Foot_Heatmap.png", bbox_inches="tight")
        plt.close()

    def plot_hand_heatmap_gif(self, max_frames=60):
        self._plot_tracker_gif("hand_movement", "Hand_Heatmap.gif", "hand", "Evolución de Manos", max_frames)
        
    def plot_foot_heatmap_gif(self, max_frames=60):
        self._plot_tracker_gif("foot_movement", "Foot_Heatmap.gif", "foot", "Evolución de Pies", max_frames)
        
    def _plot_tracker_gif(self, event_name, filename, hue_col, title, max_frames):
        df_track = self.df[self.df["event_name"] == event_name].copy()
        if "position_x" not in df_track.columns or "position_z" not in df_track.columns or df_track.empty:
            return

        if not pd.api.types.is_datetime64_any_dtype(df_track["timestamp"]):
            df_track["timestamp"] = pd.to_datetime(df_track["timestamp"])
        df_track = df_track.sort_values("timestamp")

        x_min, x_max = df_track["position_x"].min(), df_track["position_x"].max()
        z_min, z_max = df_track["position_z"].min(), df_track["position_z"].max()

        step_size = max(1, len(df_track) // max_frames)
        indices = list(range(step_size, len(df_track), step_size))
        if len(df_track) - 1 not in indices:
            indices.append(len(df_track) - 1)

        print(f"[SpatialVisualizer] Generando {len(indices)} frames para {filename}...")
        frames = []

        for idx in indices:
            current_data = df_track.iloc[:idx]
            
            fig, ax = plt.subplots(figsize=(8, 8))
            
            sns.scatterplot(
                data=current_data, 
                x="position_x", 
                y="position_z", 
                hue=hue_col if hue_col in current_data.columns else None,
                alpha=0.3, 
                s=50,
                edgecolor=None,
                ax=ax,
                palette=None if hue_col not in current_data.columns else "Set1"
            )
            
            ax.set_xlim(x_min, x_max)
            ax.set_ylim(z_min, z_max)
            ax.set_title(title)
            ax.set_xlabel("X (m)")
            ax.set_ylabel("Z (m)")
            self._draw_play_area(ax)
            ax.axis("equal")
            ax.grid(True, alpha=0.3)
            
            buf = io.BytesIO()
            plt.savefig(buf, format='png', bbox_inches='tight')
            buf.seek(0)
            frames.append(Image.open(buf))
            plt.close(fig)

        if frames:
            frames[0].save(
                self.output_dir / filename,
                save_all=True,
                append_images=frames[1:],
                optimize=True,
                duration=200,
                loop=0
            )
