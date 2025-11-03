import pandas as pd
import numpy as np
from python_analysis.event_roles import EVENT_ROLE_MAP


class MetricsCalculator:
    def __init__(self, df: pd.DataFrame):
        """
        Calculadora universal de métricas para evaluación de usuarios en VR.
        Utiliza 'roles de evento' (event_role) para abstraerse del tipo de tarea.
        """
        self.df = df.copy()
        if "timestamp" in self.df.columns:
            self.df["timestamp"] = pd.to_datetime(self.df["timestamp"], utc=True, errors="coerce")

        # Normalizar campos críticos
        self.df["user_id"] = self.df.get("user_id", "UNKNOWN").fillna("UNKNOWN")
        self.df["group_id"] = self.df.get("group_id", "UNDEFINED").fillna("UNDEFINED")
        self.df["session_id"] = self.df.get("session_id", "NO_SESSION").fillna("NO_SESSION")

        # Asignar roles semánticos
        self.df["event_role"] = self.df["event_name"].map(EVENT_ROLE_MAP).fillna("other")

        # Eventos base conocidos
        self.official_roles = {
            "action_success", "action_fail", "task_start", "task_end",
            "task_restart", "navigation_error", "session_start", "session_end",
            "help_event", "interface_error"
        }

    # ============================================================
    # NORMALIZACIÓN
    # ============================================================
    @staticmethod
    def normalize(value, min_val, max_val, invert=False):
        if pd.isna(value):
            return 0
        v = np.clip((value - min_val) / (max_val - min_val), 0, 1)
        return 1 - v if invert else v

    # ============================================================
    # --- EFECTIVIDAD ---
    # ============================================================
    def hit_ratio(self, df=None):
        df = df or self.df
        hits = len(df[df["event_role"] == "action_success"])
        fails = len(df[df["event_role"] == "action_fail"])
        total = hits + fails
        return hits / total if total > 0 else np.nan

    def precision(self, df=None):
        df = df or self.df
        actions = df[df["event_role"].isin(["action_success", "action_fail"])]
        hits = len(actions[actions["event_role"] == "action_success"])
        return hits / len(actions) if len(actions) > 0 else np.nan

    def success_rate(self, df=None):
        df = df or self.df
        tasks = df[df["event_role"] == "task_end"]
        success = len(tasks[tasks["event_value"].astype(str).str.lower() == "success"])
        return success / len(tasks) if len(tasks) > 0 else np.nan

    def learning_curve(self, df=None, block_size=5):
        df = df or self.df
        actions = df[df["event_role"].isin(["action_success", "action_fail"])]
        results = []
        for i in range(0, len(actions), block_size):
            block = actions.iloc[i:i+block_size]
            hits = len(block[block["event_role"] == "action_success"])
            ratio = hits / len(block) if len(block) > 0 else np.nan
            results.append(ratio)
        return results

    def progression(self, df=None):
        df = df or self.df
        return len(df[(df["event_role"] == "task_end") & (df["event_value"].astype(str).str.lower() == "success")])

    # ============================================================
    # --- EFICIENCIA ---
    # ============================================================
    def avg_reaction_time(self, df=None):
        df = df or self.df
        return df["reaction_time_ms"].dropna().mean() if "reaction_time_ms" in df.columns else np.nan

    def avg_task_duration(self, df=None):
        df = df or self.df
        tasks = df[df["event_role"] == "task_end"]
        return tasks["duration_ms"].dropna().mean() if "duration_ms" in tasks.columns else np.nan

    def time_per_success(self, df=None):
        df = df or self.df
        hits = df[df["event_role"] == "action_success"]
        tasks = df[df["event_role"] == "task_end"]
        if not tasks.empty and not hits.empty:
            total_time = (tasks["timestamp"].max() - tasks["timestamp"].min()).total_seconds()
            return total_time / len(hits) if len(hits) > 0 else np.nan
        return np.nan

    def navigation_errors(self, df=None):
        df = df or self.df
        return len(df[df["event_role"] == "navigation_error"])

    def aim_errors(self, df=None):
        df = df or self.df
        attempts = df[df["event_role"].isin(["action_success", "action_fail"])]
        return len(attempts)

    # ============================================================
    # --- SATISFACCIÓN ---
    # ============================================================
    def retries_after_end(self, df=None):
        df = df or self.df
        return len(df[df["event_role"] == "task_restart"])

    def voluntary_play_time(self, df=None):
        df = df or self.df
        session_end = df[df["event_role"] == "session_end"]["timestamp"].min()
        task_end = df[df["event_role"] == "task_end"]["timestamp"].max()
        if pd.notna(session_end) and pd.notna(task_end):
            return (session_end - task_end).total_seconds()
        return np.nan

    def aid_usage(self, df=None):
        df = df or self.df
        return len(df[df["event_role"] == "help_event"])

    def interface_errors(self, df=None):
        df = df or self.df
        return len(df[df["event_role"] == "interface_error"])

    # ============================================================
    # --- PRESENCIA ---
    # ============================================================
    def inactivity_time(self, df=None, threshold=5):
        df = df or self.df
        ts = df["timestamp"].sort_values()
        diffs = ts.diff().dt.total_seconds()
        return diffs[diffs > threshold].sum()

    def first_success_time(self, df=None):
        df = df or self.df
        start = df[df["event_role"] == "session_start"]["timestamp"].min()
        first_success = df[df["event_role"] == "action_success"]["timestamp"].min()
        if pd.notna(start) and pd.notna(first_success):
            return (first_success - start).total_seconds()
        return np.nan

    def sound_localization_time(self, df=None):
        df = df or self.df
        audio = df[df["event_name"] == "audio_triggered"]["timestamp"].min()
        head_turn = df[df["event_name"] == "head_turn"]["timestamp"].min()
        if pd.notna(audio) and pd.notna(head_turn):
            return (head_turn - audio).total_seconds()
        return np.nan

    def activity_level(self, df=None):
        df = df or self.df
        if "timestamp" in df.columns:
            total_time = (df["timestamp"].max() - df["timestamp"].min()).total_seconds() / 60
            return len(df) / total_time if total_time > 0 else np.nan
        return np.nan

    # ============================================================
    # --- CUSTOM EVENTS ---
    # ============================================================
    def custom_events_summary(self, df=None):
        df = df or self.df
        custom = df[~df["event_role"].isin(self.official_roles)]
        if custom.empty:
            return {}
        return custom["event_name"].value_counts().to_dict()

    # ============================================================
    # --- INDICADORES DERIVADOS ---
    # ============================================================
    def _success_after_restart(self, df):
        restarts = df[df["event_role"] == "task_restart"]["timestamp"]
        if restarts.empty:
            return None
        tasks_after_restart = df[(df["event_role"] == "task_end") & (df["timestamp"] > restarts.min())]
        if tasks_after_restart.empty:
            return None
        success = len(tasks_after_restart[tasks_after_restart["event_value"].astype(str).str.lower() == "success"])
        return success / len(tasks_after_restart)

    def _task_duration_by_result(self, df, result):
        tasks = df[(df["event_role"] == "task_end") & (df["event_value"] == result)]
        if "duration_ms" in tasks.columns and not tasks.empty:
            return tasks["duration_ms"].dropna().mean()
        return None

    def _learning_stability(self, df):
        curve = self.learning_curve(df)
        if not curve:
            return None
        return 1 - np.nanstd(curve)

    def _error_reduction_rate(self, df):
        errors = df[df["event_role"].isin(["action_fail", "interface_error"])]
        if errors.empty:
            return None
        half = len(errors) // 2
        first_half, second_half = errors.iloc[:half], errors.iloc[half:]
        if len(first_half) == 0:
            return None
        return (len(first_half) - len(second_half)) / len(first_half)

    def _audio_performance_gain(self, df):
        if "condition_audio" not in df.columns:
            return None
        with_audio = df[df["condition_audio"] == True]
        without_audio = df[df["condition_audio"] == False]
        if with_audio.empty or without_audio.empty:
            return None
        return self.hit_ratio(with_audio) - self.hit_ratio(without_audio)

    # ============================================================
    # --- FÓRMULA PONDERADA ---
    # ============================================================
    def _weighted_scores(self, row):
        """Calcula las puntuaciones ponderadas por categoría."""
        n = self.normalize  # alias

        efectividad = (
            0.35 * (row.get("hit_ratio") or 0) +
            0.30 * (row.get("success_rate") or 0) +
            0.15 * (row.get("learning_curve_mean") or 0) +
            0.10 * n(row.get("progression") or 0, 0, 10) +
            0.10 * n(row.get("success_after_restart") or 0, 0, 1)
        )

        eficiencia = (
            0.4 * n(row.get("avg_reaction_time_ms") or 0, 100, 2000, invert=True) +
            0.3 * n(row.get("avg_task_duration_ms") or 0, 1000, 30000, invert=True) +
            0.2 * n(row.get("time_per_success_s") or 0, 0, 60, invert=True) +
            0.1 * n(row.get("navigation_errors") or 0, 0, 10, invert=True)
        )

        satisfaccion = (
            0.3 * (row.get("learning_stability") or 0) +
            0.25 * n(row.get("error_reduction_rate") or 0, 0, 1) +
            0.25 * n(row.get("voluntary_play_time_s") or 0, 0, 60) +
            0.1 * n(row.get("aid_usage") or 0, 0, 5, invert=True) +
            0.1 * n(row.get("interface_errors") or 0, 0, 5, invert=True)
        )

        presencia = (
            0.25 * n(row.get("activity_level_per_min") or 0, 0, 100) +
            0.25 * n(row.get("first_success_time_s") or 0, 0, 30, invert=True) +
            0.2 * n(row.get("inactivity_time_s") or 0, 0, 60, invert=True) +
            0.15 * n(row.get("sound_localization_time_s") or 0, 0, 10, invert=True) +
            0.15 * n(row.get("audio_performance_gain") or 0, -1, 1)
        )

        return {
            "efectividad_score": round(efectividad * 100, 2),
            "eficiencia_score": round(eficiencia * 100, 2),
            "satisfaccion_score": round(satisfaccion * 100, 2),
            "presencia_score": round(presencia * 100, 2),
            "total_score": round((efectividad + eficiencia + satisfaccion + presencia) / 4 * 100, 2)
        }

    # ============================================================
    # --- AGRUPADO POR USUARIO Y SESIÓN ---
    # ============================================================
    def compute_grouped_metrics(self):
        if not {"user_id", "group_id", "session_id"}.issubset(self.df.columns):
            print("[MetricsCalculator] ⚠️ Faltan columnas (user_id, group_id, session_id).")
            return pd.DataFrame()

        grouped = self.df.groupby(["user_id", "group_id", "session_id"])
        results = []

        for (user, group, session), subdf in grouped:
            calc = {
                "user_id": user,
                "group_id": group,
                "session_id": session,

                # 🟢 EFECTIVIDAD
                "hit_ratio": self.hit_ratio(subdf),
                "precision": self.precision(subdf),
                "success_rate": self.success_rate(subdf),
                "learning_curve_mean": np.nanmean(self.learning_curve(subdf)) if self.learning_curve(subdf) else None,
                "progression": self.progression(subdf),
                "success_after_restart": self._success_after_restart(subdf),
                "attempts_per_target": self.aim_errors(subdf),

                # 🟠 EFICIENCIA
                "avg_reaction_time_ms": self.avg_reaction_time(subdf),
                "avg_task_duration_ms": self.avg_task_duration(subdf),
                "time_per_success_s": self.time_per_success(subdf),
                "navigation_errors": self.navigation_errors(subdf),
                "aim_errors": self.aim_errors(subdf),
                "task_duration_success": self._task_duration_by_result(subdf, "success"),
                "task_duration_fail": self._task_duration_by_result(subdf, "fail"),

                # 🟣 SATISFACCIÓN
                "retries_after_end": self.retries_after_end(subdf),
                "voluntary_play_time_s": self.voluntary_play_time(subdf),
                "aid_usage": self.aid_usage(subdf),
                "interface_errors": self.interface_errors(subdf),
                "learning_stability": self._learning_stability(subdf),
                "error_reduction_rate": self._error_reduction_rate(subdf),

                # 🔵 PRESENCIA
                "inactivity_time_s": self.inactivity_time(subdf),
                "first_success_time_s": self.first_success_time(subdf),
                "sound_localization_time_s": self.sound_localization_time(subdf),
                "activity_level_per_min": self.activity_level(subdf),
                "audio_performance_gain": self._audio_performance_gain(subdf),
            }

            # Añadir scores ponderados
            calc.update(self._weighted_scores(calc))
            results.append(calc)

        return pd.DataFrame(results)

    # ============================================================
    # --- AGREGADO GENERAL (GLOBAL) ---
    # ============================================================
    def compute_all(self):
        base = {
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

        # Calcular score global promedio a partir de medias de todas las filas agrupadas
        grouped_df = pd.DataFrame(base["agrupado_por_usuario_y_sesion"])
        if not grouped_df.empty:
            mean_row = grouped_df.mean(numeric_only=True).to_dict()
            base["scores"] = self._weighted_scores(mean_row)

        return base
