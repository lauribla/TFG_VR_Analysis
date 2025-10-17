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
            self.df["timestamp"] = pd.to_datetime(self.df["timestamp"], utc=True, errors="coerce")

        # Normalizar campos críticos
        self.df["user_id"] = self.df.get("user_id", "UNKNOWN").fillna("UNKNOWN")
        self.df["group_id"] = self.df.get("group_id", "UNDEFINED").fillna("UNDEFINED")
        self.df["session_id"] = self.df.get("session_id", "NO_SESSION").fillna("NO_SESSION")

        # Eventos que forman parte de la tabla oficial de indicadores
        self.official_events = {
            "target_hit", "target_miss", "task_start", "task_end",
            "task_timeout", "task_abandoned", "task_restart",
            "navigation_error", "collision", "controller_error", "wrong_button",
            "help_requested", "guide_used", "hint_used",
            "gaze_sustained", "gaze_target_change", "gaze_frame",
            "movement_frame", "sharp_turn", "audio_triggered", "head_turn",
            "session_start", "session_end"
        }

    # ============================================================
    # --- EFECTIVIDAD ---
    # ============================================================
    def hit_ratio(self, df=None):
        if df is None:
            df = self.df

        hits = len(df[df["event_name"] == "target_hit"])
        misses = len(df[df["event_name"] == "target_miss"])
        total = hits + misses
        return hits / total if total > 0 else np.nan

    def precision(self, df=None):
        if df is None:
            df = self.df

        hits = len(df[df["event_name"] == "target_hit"])
        shots = len(df[df["event_name"].isin(["trigger_pull", "target_hit", "target_miss"])])
        return hits / shots if shots > 0 else np.nan

    def success_rate(self, df=None):
        if df is None:
            df = self.df

        tasks = df[df["event_name"] == "task_end"]
        success = len(tasks[tasks["event_value"] == "success"])
        return success / len(tasks) if len(tasks) > 0 else np.nan

    def learning_curve(self, df=None, block_size=5):
        if df is None:
            df = self.df

        attempts = df[df["event_name"].isin(["target_hit", "target_miss"])]
        results = []
        for i in range(0, len(attempts), block_size):
            block = attempts.iloc[i:i+block_size]
            hits = len(block[block["event_name"] == "target_hit"])
            ratio = hits / len(block) if len(block) > 0 else np.nan
            results.append(ratio)
        return results

    def progression(self, df=None):
        if df is None:
            df = self.df

        return len(df[(df["event_name"] == "task_end") & (df["event_value"] == "success")])

    # ============================================================
    # --- EFICIENCIA ---
    # ============================================================
    def avg_reaction_time(self, df=None):
        if df is None:
            df = self.df

        if "reaction_time_ms" in df.columns:
            return df["reaction_time_ms"].dropna().mean()
        return np.nan

    def avg_task_duration(self, df=None):
        if df is None:
            df = self.df

        tasks = df[df["event_name"] == "task_end"]
        if "duration_ms" in tasks.columns:
            return tasks["duration_ms"].dropna().mean()
        return np.nan

    def time_per_success(self, df=None):
        if df is None:
            df = self.df

        hits = df[df["event_name"] == "target_hit"]
        tasks = df[df["event_name"] == "task_end"]
        if not tasks.empty and not hits.empty:
            total_time = (tasks["timestamp"].max() - tasks["timestamp"].min()).total_seconds()
            return total_time / len(hits) if len(hits) > 0 else np.nan
        return np.nan

    def navigation_errors(self, df=None):
        if df is None:
            df = self.df

        return len(df[df["event_name"].isin(["navigation_error", "collision"])])

    def aim_errors(self, df=None):
        if df is None:
            df = self.df

        attempts = df[df["event_name"].isin(["target_hit", "target_miss"])]
        return len(attempts)

    # ============================================================
    # --- SATISFACCIÓN ---
    # ============================================================
    def retries_after_end(self, df=None):
        if df is None:
            df = self.df

        return len(df[df["event_name"] == "task_restart"])

    def voluntary_play_time(self, df=None):
        if df is None:
            df = self.df

        session_end = df[df["event_name"] == "session_end"]["timestamp"].min()
        task_end = df[df["event_name"] == "task_end"]["timestamp"].max()
        if pd.notna(session_end) and pd.notna(task_end):
            return (session_end - task_end).total_seconds()
        return np.nan

    def aid_usage(self, df=None):
        if df is None:
            df = self.df

        return len(df[df["event_name"].isin(["help_requested", "guide_used", "hint_used"])])

    def interface_errors(self, df=None):
        if df is None:
            df = self.df

        return len(df[df["event_name"].isin(["controller_error", "wrong_button"])])

    # ============================================================
    # --- PRESENCIA ---
    # ============================================================
    def inactivity_time(self, df=None, threshold=5):
        if df is None:
            df = self.df

        ts = df["timestamp"].sort_values()
        diffs = ts.diff().dt.total_seconds()
        return diffs[diffs > threshold].sum()

    def first_success_time(self, df=None):
        if df is None:
            df = self.df

        session_start = df[df["event_name"] == "session_start"]["timestamp"].min()
        first_hit = df[df["event_name"] == "target_hit"]["timestamp"].min()
        if pd.notna(session_start) and pd.notna(first_hit):
            return (first_hit - session_start).total_seconds()
        return np.nan

    def sound_localization_time(self, df=None):
        if df is None:
            df = self.df

        audio = df[df["event_name"] == "audio_triggered"]["timestamp"].min()
        head_turn = df[df["event_name"] == "head_turn"]["timestamp"].min()
        if pd.notna(audio) and pd.notna(head_turn):
            return (head_turn - audio).total_seconds()
        return np.nan

    def activity_level(self, df=None):
        if df is None:
            df = self.df

        if "timestamp" in df.columns:
            total_time = (df["timestamp"].max() - df["timestamp"].min()).total_seconds() / 60
            return len(df) / total_time if total_time > 0 else np.nan
        return np.nan

    # ============================================================
    # --- CUSTOM EVENTS ---
    # ============================================================
    def custom_events_summary(self, df=None):
        if df is None:
            df = self.df

        custom = df[~df["event_name"].isin(self.official_events)]
        if custom.empty:
            return {}
        return custom["event_name"].value_counts().to_dict()

    # ============================================================
    # --- AGREGADOS MULTIUSUARIO / MULTISESIÓN ---
    # ============================================================
    def compute_grouped_metrics(self):
        """
        Devuelve un DataFrame con métricas calculadas por usuario y sesión.
        """
        grouped_results = []

        for (user, group, session), subdf in self.df.groupby(["user_id", "group_id", "session_id"]):
            calc = {
                "user_id": user,
                "group_id": group,
                "session_id": session,

                # --- EFECTIVIDAD ---
                "hit_ratio": self.hit_ratio(subdf),
                "precision": self.precision(subdf),
                "success_rate": self.success_rate(subdf),
                "progression": self.progression(subdf),

                # --- EFICIENCIA ---
                "avg_reaction_time_ms": self.avg_reaction_time(subdf),
                "avg_task_duration_ms": self.avg_task_duration(subdf),
                "time_per_success_s": self.time_per_success(subdf),
                "navigation_errors": self.navigation_errors(subdf),
                "aim_errors": self.aim_errors(subdf),

                # --- SATISFACCIÓN ---
                "voluntary_play_time_s": self.voluntary_play_time(subdf),
                "aid_usage": self.aid_usage(subdf),
                "interface_errors": self.interface_errors(subdf),
                "retries_after_end": self.retries_after_end(subdf),

                # --- PRESENCIA ---
                "inactivity_time_s": self.inactivity_time(subdf),
                "first_success_time_s": self.first_success_time(subdf),
                "sound_localization_time_s": self.sound_localization_time(subdf),
                "activity_level": self.activity_level(subdf),

                # --- CUSTOM EVENTS ---
                "custom_events_count": len(subdf[subdf["event_name"].str.startswith("custom_")])
            }

            grouped_results.append(calc)

        return pd.DataFrame(grouped_results)

    # ============================================================
    # --- AGREGADO GENERAL (individual o global) ---
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
            "custom_events": self.custom_events_summary(),
            "agrupado_por_usuario_y_sesion": self.compute_grouped_metrics().to_dict(orient="records")
        }
