import pandas as pd
import numpy as np

class MetricsCalculator:
    def __init__(self, df: pd.DataFrame):
        """
        df: DataFrame parseado desde LogParser con expand_context=True.
        Debe contener al menos: timestamp, user_id, event_name, event_value, session_id, group_id
        y en columnas adicionales: reaction_time_ms, duration_ms, etc. si están disponibles.
        """
        self.df = df.copy()
        if "timestamp" in self.df.columns:
            self.df["timestamp"] = pd.to_datetime(self.df["timestamp"])

        # Eventos que forman parte de la tabla oficial de indicadores
        self.official_events = {
            "target_hit", "target_miss", "task_start", "task_end",
            "task_timeout", "task_abandoned", "task_restart",
            "navigation_error", "collision", "controller_error", "wrong_button",
            "help_requested", "guide_used", "hint_used",
            "gaze_sustained", "gaze_target_change", "gaze_frame",
            "movement_frame", "sharp_turn", "audio_triggered", "head_turn"
        }

    # ============================================================
    # EFECTIVIDAD
    # ============================================================
    def hit_ratio(self):
        hits = len(self.df[self.df["event_name"] == "target_hit"])
        misses = len(self.df[self.df["event_name"] == "target_miss"])
        total = hits + misses
        return hits / total if total > 0 else None

    def precision(self):
        hits = len(self.df[self.df["event_name"] == "target_hit"])
        shots = len(self.df[self.df["event_name"].isin(["trigger_pull", "target_hit", "target_miss"])])
        return hits / shots if shots > 0 else None

    def success_rate(self):
        tasks = self.df[self.df["event_name"] == "task_end"]
        success = len(tasks[tasks["event_value"] == "success"])
        return success / len(tasks) if len(tasks) > 0 else None

    def learning_curve(self, block_size=5):
        """Devuelve % aciertos por bloques de intentos"""
        attempts = self.df[self.df["event_name"].isin(["target_hit", "target_miss"])]
        results = []
        for i in range(0, len(attempts), block_size):
            block = attempts.iloc[i:i+block_size]
            hits = len(block[block["event_name"] == "target_hit"])
            ratio = hits / len(block) if len(block) > 0 else None
            results.append(ratio)
        return results

    def progression(self):
        """Número de tareas completadas con éxito"""
        return len(self.df[(self.df["event_name"] == "task_end") & (self.df["event_value"] == "success")])

    # ============================================================
    # EFICIENCIA
    # ============================================================
    def avg_reaction_time(self):
        if "reaction_time_ms" in self.df.columns:
            return self.df["reaction_time_ms"].dropna().mean()
        return None

    def avg_task_duration(self):
        tasks = self.df[self.df["event_name"] == "task_end"]
        if "duration_ms" in tasks.columns:
            return tasks["duration_ms"].dropna().mean()
        return None

    def time_per_success(self):
        hits = self.df[self.df["event_name"] == "target_hit"]
        tasks = self.df[self.df["event_name"] == "task_end"]
        if not tasks.empty and not hits.empty:
            total_time = (tasks["timestamp"].max() - tasks["timestamp"].min()).total_seconds()
            return total_time / len(hits) if len(hits) > 0 else None
        return None

    def navigation_errors(self):
        return len(self.df[self.df["event_name"].isin(["navigation_error", "collision"])])

    def aim_errors(self):
        """Nº intentos por objetivo"""
        attempts = self.df[self.df["event_name"].isin(["target_hit", "target_miss"])]
        return len(attempts)

    # ============================================================
    # SATISFACCIÓN (indicadores indirectos)
    # ============================================================
    def retries_after_end(self):
        return len(self.df[self.df["event_name"] == "task_restart"])

    def voluntary_play_time(self):
        session_end = self.df[self.df["event_name"] == "session_end"]["timestamp"].min()
        task_end = self.df[self.df["event_name"] == "task_end"]["timestamp"].max()
        if pd.notna(session_end) and pd.notna(task_end):
            return (session_end - task_end).total_seconds()
        return None

    def aid_usage(self):
        return len(self.df[self.df["event_name"].isin(["help_requested", "guide_used", "hint_used"])])

    def interface_errors(self):
        return len(self.df[self.df["event_name"].isin(["controller_error", "wrong_button"])])

    # ============================================================
    # PRESENCIA
    # ============================================================
    def inactivity_time(self, threshold=5):
        ts = self.df["timestamp"].sort_values()
        diffs = ts.diff().dt.total_seconds()
        return diffs[diffs > threshold].sum()

    def first_success_time(self):
        session_start = self.df[self.df["event_name"] == "session_start"]["timestamp"].min()
        first_hit = self.df[self.df["event_name"] == "target_hit"]["timestamp"].min()
        if pd.notna(session_start) and pd.notna(first_hit):
            return (first_hit - session_start).total_seconds()
        return None

    def sound_localization_time(self):
        audio = self.df[self.df["event_name"] == "audio_triggered"]["timestamp"].min()
        head_turn = self.df[self.df["event_name"] == "head_turn"]["timestamp"].min()
        if pd.notna(audio) and pd.notna(head_turn):
            return (head_turn - audio).total_seconds()
        return None

    def activity_level(self):
        """Actividad dentro del entorno (acciones por minuto)"""
        if "timestamp" in self.df.columns:
            total_time = (self.df["timestamp"].max() - self.df["timestamp"].min()).total_seconds() / 60
            return len(self.df) / total_time if total_time > 0 else None
        return None

    # ============================================================
    # CUSTOM EVENTS
    # ============================================================
    def custom_events_summary(self):
        """
        Devuelve un resumen de todos los eventos que no están en la lista oficial.
        Ejemplo: {'hand_gesture': 12, 'special_power': 5}
        """
        custom = self.df[~self.df["event_name"].isin(self.official_events)]
        if custom.empty:
            return {}
        return custom["event_name"].value_counts().to_dict()

    # ============================================================
    # AGREGADO GENERAL
    # ============================================================
    def compute_all(self):
        return {
            "efectividad": {
                "hit_ratio": self.hit_ratio(),
                "precision": self.precision(),
                "success_rate": self.success_rate(),
                "learning_curve": self.learning_curve(),
                "progression": self.progression(),
            },
            "eficiencia": {
                "avg_reaction_time_ms": self.avg_reaction_time(),
                "avg_task_duration_ms": self.avg_task_duration(),
                "time_per_success_s": self.time_per_success(),
                "navigation_errors": self.navigation_errors(),
                "aim_errors": self.aim_errors(),
            },
            "satisfaccion": {
                "retries_after_end": self.retries_after_end(),
                "voluntary_play_time_s": self.voluntary_play_time(),
                "aid_usage": self.aid_usage(),
                "interface_errors": self.interface_errors(),
            },
            "presencia": {
                "inactivity_time_s": self.inactivity_time(),
                "first_success_time_s": self.first_success_time(),
                "sound_localization_time_s": self.sound_localization_time(),
                "activity_level_per_min": self.activity_level(),
            },
            "custom_events": self.custom_events_summary()
        }
