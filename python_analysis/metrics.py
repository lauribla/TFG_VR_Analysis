import pandas as pd
import numpy as np

class MetricsCalculator:
    def __init__(self, df: pd.DataFrame, experiment_config=None, user_profile="novice"):
        """
        Calculadora avanzada de métricas basada 100% en experiment_config.
        """

        self.df = df.copy()
        self.config = experiment_config or {}
        self.user_profile = user_profile

        # ------------------------------------------------------------------
        # Normalización de timestamp
        # ------------------------------------------------------------------
        if "timestamp" in self.df.columns:
            self.df["timestamp"] = pd.to_datetime(self.df["timestamp"], utc=True, errors="coerce")

        self.df["user_id"] = self.df.get("user_id", "UNKNOWN").fillna("UNKNOWN")
        self.df["group_id"] = self.df.get("group_id", "GROUP").fillna("GROUP")
        self.df["session_id"] = self.df.get("session_id", "SESSION").fillna("SESSION")

        # ------------------------------------------------------------------
        # Asignación de roles desde config
        # ------------------------------------------------------------------
        roles_cfg = self.config.get("event_roles", {})

        def resolve_role(ev):
            return roles_cfg.get(ev, "custom_event")

        self.df["event_role"] = self.df["event_name"].apply(resolve_role)
        def resolve_role(ev):
            return roles_cfg.get(ev, "custom_event")
        self.df["event_role"] = self.df["event_name"].apply(resolve_role)

        # ------------------------------------------------------------------
        # Registro de métricas y perfiles desde config
        # ------------------------------------------------------------------
        self.metrics_cfg = self.config.get("metrics", {})
        self.profiles_cfg = self.config.get("profiles", {})

    # ----------------------------------------------------------------------
    # Normalización universal
    # ----------------------------------------------------------------------
    @staticmethod
    def normalize(value, min_val, max_val, invert=False):
        if pd.isna(value):
            return 0.0
        if min_val is None or max_val is None:
            return value  # sin normalización declarada en config
        v = np.clip((value - min_val) / (max_val - min_val), 0, 1)
        return 1 - v if invert else v

    # ----------------------------------------------------------------------
    # MÉTRICAS BÁSICAS – las mismas que antes
    # ----------------------------------------------------------------------

    def hit_ratio(self, df=None):
        if df is None: df = self.df
        hits = len(df[df["event_role"] == "action_success"])
        fails = len(df[df["event_role"] == "action_fail"])
        tot = hits + fails
        return hits / tot if tot > 0 else np.nan

    def precision(self, df=None):
        if df is None: df = self.df
        actions = df[df["event_role"].isin(["action_success", "action_fail"])]
        if len(actions)==0: return np.nan
        hits = len(actions[actions["event_role"] == "action_success"])
        return hits / len(actions)

    def success_rate(self, df=None):
        if df is None: df = self.df
        tasks = df[df["event_role"] == "task_end"]
        if len(tasks)==0: return np.nan
        success = len(tasks[tasks["event_value"].astype(str).str.lower()=="success"])
        return success / len(tasks)

    def learning_curve(self, df=None, block_size=5):
        if df is None: df = self.df
        actions = df[df["event_role"].isin(["action_success","action_fail"])]
        res=[]
        for i in range(0, len(actions), block_size):
            block = actions.iloc[i:i+block_size]
            hits = len(block[block["event_role"]=="action_success"])
            res.append(hits/len(block) if len(block)>0 else np.nan)
        return res

    def avg_reaction_time(self, df=None):
        if df is None: df = self.df
        if "reaction_time_ms" not in df.columns:
            return np.nan
        return df["reaction_time_ms"].dropna().mean()

    def avg_task_duration(self, df=None):
        if df is None: df = self.df
        tasks = df[df["event_role"]=="task_end"]
        if "duration_ms" not in tasks.columns:
            return np.nan
        return tasks["duration_ms"].dropna().mean()

    def time_per_success(self, df=None):
        if df is None: df = self.df
        hits = df[df["event_role"]=="action_success"]
        if len(hits)==0:
            return np.nan
        total_time = (df["timestamp"].max() - df["timestamp"].min()).total_seconds()
        return total_time / len(hits)

    def retries_after_end(self, df=None):
        if df is None: df = self.df
        return len(df[df["event_role"]=="task_restart"])

    def voluntary_play_time(self, df=None):
        if df is None: df = self.df
        end = df[df["event_role"]=="session_end"]["timestamp"].min()
        task_end = df[df["event_role"]=="task_end"]["timestamp"].max()
        if pd.notna(end) and pd.notna(task_end):
            return (end-task_end).total_seconds()
        return np.nan

    def aid_usage(self, df=None):
        if df is None: df = self.df
        return len(df[df["event_role"]=="help_event"])

    def inactivity_time(self, df=None, threshold=5):
        if df is None: df = self.df
        ts = df["timestamp"].sort_values()
        diffs = ts.diff().dt.total_seconds()
        return diffs[diffs > threshold].sum()

    def first_success_time(self, df=None):
        if df is None: df = self.df
        start = df[df["event_role"]=="session_start"]["timestamp"].min()
        succ = df[df["event_role"]=="action_success"]["timestamp"].min()
        if pd.notna(start) and pd.notna(succ):
            return (succ-start).total_seconds()
        return np.nan

    def sound_localization_time(self, df=None):
        if df is None: df = self.df
        audio = df[df["event_name"]=="audio_triggered"]["timestamp"].min()
        head = df[df["event_name"]=="head_turn"]["timestamp"].min()
        if pd.notna(audio) and pd.notna(head):
            return (head-audio).total_seconds()
        return np.nan

    def activity_level(self, df=None):
        if df is None: df = self.df
        dur = (df["timestamp"].max() - df["timestamp"].min()).total_seconds()/60
        if dur<=0: return np.nan
        return len(df)/dur

    # ----------------------------------------------------------------------
    # MAPEO dinámico de nombres → funciones internas
    # ----------------------------------------------------------------------
    def _available_metric_functions(self):
        return {
            "hit_ratio": self.hit_ratio,
            "precision": self.precision,
            "success_rate": self.success_rate,
            "learning_curve_mean": lambda df=None: np.nanmean(self.learning_curve(df)),

            "avg_reaction_time_ms": self.avg_reaction_time,
            "avg_task_duration_ms": self.avg_task_duration,
            "time_per_success_s": self.time_per_success,

            "retries_after_end": self.retries_after_end,
            "voluntary_play_time_s": self.voluntary_play_time,
            "aid_usage": self.aid_usage,

            "inactivity_time_s": self.inactivity_time,
            "first_success_time_s": self.first_success_time,
            "sound_localization_time_s": self.sound_localization_time,
            "activity_level_per_min": self.activity_level
        }

    # ----------------------------------------------------------------------
    # PROCESAR MÉTRICAS POR CATEGORÍA (efectividad, eficiencia…)
    # ----------------------------------------------------------------------
    def compute_category(self, category_name, df=None):
        if df is None:
            df = self.df

        cat_cfg = self.metrics_cfg.get(category_name, {})
        metric_funcs = self._available_metric_functions()

        results = {}
        weighted_sum = 0
        total_weight = 0

        for metric_name, params in cat_cfg.items():
            func = metric_funcs.get(metric_name)
            if func is None:
                continue

            raw_value = func(df)

            weight = params.get("weight", 1.0)
            min_val = params.get("min")
            max_val = params.get("max")
            invert = params.get("invert", False)

            normalized = self.normalize(raw_value, min_val, max_val, invert)

            results[metric_name] = {
                "raw": raw_value,
                "normalized": normalized,
                "weight": weight
            }

            weighted_sum += normalized * weight
            total_weight += weight

        # puntuación final de la categoría
        results["score"] = weighted_sum / total_weight if total_weight > 0 else 0.0

        return results

    # ----------------------------------------------------------------------
    # PUNTUACIÓN GLOBAL (usa perfiles)
    # ----------------------------------------------------------------------
    def compute_global_score(self, cat_scores):
        profile = self.profiles_cfg.get(self.user_profile, {})

        eff_w  = profile.get("efectividad_weight", 1)
        efi_w  = profile.get("eficiencia_weight", 1)
        sat_w  = profile.get("satisfaccion_weight", 1)
        pre_w  = profile.get("presencia_weight", 1)

        final = (
            cat_scores["efectividad"]["score"] * eff_w +
            cat_scores["eficiencia"]["score"] * efi_w +
            cat_scores["satisfaccion"]["score"] * sat_w +
            cat_scores["presencia"]["score"] * pre_w
        )

        return final / (eff_w + efi_w + sat_w + pre_w)

    # ----------------------------------------------------------------------
    # MÉTRICAS AGRUPADAS POR USUARIO Y SESIÓN
    # ----------------------------------------------------------------------
    def compute_grouped_metrics(self):
        metric_funcs = self._available_metric_functions()
        rows = []

        grouped = self.df.groupby(["user_id", "group_id", "session_id"])

        for (user, group, session), subdf in grouped:
            entry = {
                "user_id": user,
                "group_id": group,
                "session_id": session,
            }

            # Añadir cada métrica básica disponible
            for metric_name, func in metric_funcs.items():
                try:
                    entry[metric_name] = func(subdf)
                except:
                    entry[metric_name] = None

            # Añadir categorías normalizadas
            cat_scores = {
                "efectividad": self.compute_category("efectividad", subdf)["score"],
                "eficiencia": self.compute_category("eficiencia", subdf)["score"],
                "satisfaccion": self.compute_category("satisfaccion", subdf)["score"],
                "presencia": self.compute_category("presencia", subdf)["score"]
            }

            entry.update(cat_scores)

            # Puntuación global para este participante
            entry["global_score"] = self.compute_global_score({
                "efectividad": {"score": cat_scores["efectividad"]},
                "eficiencia": {"score": cat_scores["eficiencia"]},
                "satisfaccion": {"score": cat_scores["satisfaccion"]},
                "presencia": {"score": cat_scores["presencia"]},
            })

            rows.append(entry)

        return pd.DataFrame(rows)


    # ----------------------------------------------------------------------
    # MÉTRICAS GLOBALES
    # ----------------------------------------------------------------------
    def compute_all(self):
        efectividad = self.compute_category("efectividad")
        eficiencia  = self.compute_category("eficiencia")
        satisfaccion = self.compute_category("satisfaccion")
        presencia   = self.compute_category("presencia")

        cat_scores = {
            "efectividad": efectividad,
            "eficiencia": eficiencia,
            "satisfaccion": satisfaccion,
            "presencia": presencia
        }

        global_score = self.compute_global_score(cat_scores)

        return {
            "categorias": cat_scores,
            "global_score": global_score
        }
