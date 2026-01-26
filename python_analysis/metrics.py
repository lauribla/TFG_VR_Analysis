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

        if "user_id" not in self.df.columns:
            self.df["user_id"] = "UNKNOWN"
        else:
            self.df["user_id"] = self.df["user_id"].fillna("UNKNOWN")

        if "group_id" not in self.df.columns:
            self.df["group_id"] = "GROUP"
        else:
            self.df["group_id"] = self.df["group_id"].fillna("GROUP")

        if "session_id" not in self.df.columns:
            self.df["session_id"] = "SESSION"
        else:
            self.df["session_id"] = self.df["session_id"].fillna("SESSION")

        # ------------------------------------------------------------------
        # Asignación de roles desde config (corregido)
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
    # MÉTRICAS BÁSICAS
    # ----------------------------------------------------------------------

    def hit_ratio(self, df=None):
        if df is None: df = self.df
        if df.empty: return 0.0  # Safe default
        hits = len(df[df["event_role"] == "action_success"])
        fails = len(df[df["event_role"] == "action_fail"])
        tot = hits + fails
        return hits / tot if tot > 0 else 0.0

    def precision(self, df=None):
        if df is None: df = self.df
        actions = df[df["event_role"].isin(["action_success", "action_fail"])]
        if len(actions) == 0: return np.nan
        hits = len(actions[actions["event_role"] == "action_success"])
        return hits / len(actions)

    def success_rate(self, df=None):
        if df is None: df = self.df
        tasks = df[df["event_role"] == "task_end"]
        if len(tasks) == 0: return np.nan
        success = len(tasks[tasks["event_value"].astype(str).str.lower() == "success"])
        return success / len(tasks)

    def learning_curve(self, df=None, block_size=5):
        if df is None: df = self.df
        actions = df[df["event_role"].isin(["action_success", "action_fail"])]
        res = []
        for i in range(0, len(actions), block_size):
            block = actions.iloc[i:i + block_size]
            hits = len(block[block["event_role"] == "action_success"])
            res.append(hits / len(block) if len(block) > 0 else np.nan)
        return res

    def _derive_task_stats(self, df: pd.DataFrame):
        """
        Derive task_duration_ms and reaction_time_ms from raw event stream.
        Robust pairing: Uses session_id and task sequence if available, or strict time sorting.
        """
        if df is None or df.empty or "timestamp" not in df.columns:
            return pd.DataFrame(columns=["task_duration_ms", "reaction_time_ms"])

        tmp = df.copy()
        tmp["timestamp"] = pd.to_datetime(tmp["timestamp"], utc=True, errors="coerce")
        tmp = tmp.sort_values("timestamp")

        # Find all starts and ends
        starts = tmp[tmp["event_role"] == "task_start"].reset_index(drop=True)
        ends = tmp[tmp["event_role"] == "task_end"].reset_index(drop=True)

        task_durations = []
        reaction_times = []

        # Robust pairing: Simply zip and check timestamps.
        # If mismatched counts, we truncate to the shorter list (simple robust approach).
        # A more complex approach would be to match by an explicit ID if it existed.
        count = min(len(starts), len(ends))

        for i in range(count):
            t0 = starts.loc[i, "timestamp"]
            t1 = ends.loc[i, "timestamp"]

            # Sanity check: Start must be before End
            if t0 > t1:
                # Discard misaligned pair (or try to look ahead? For now, discard/NaN safe)
                continue

            dur_ms = (t1 - t0).total_seconds() * 1000.0
            task_durations.append(dur_ms)

            # Reaction time: First action between t0 and t1
            actions = tmp[
                (tmp["event_role"].isin(["action_success", "action_fail"])) &
                (tmp["timestamp"] >= t0) &
                (tmp["timestamp"] <= t1)
                ]

            if not actions.empty:
                rt_ms = (actions.iloc[0]["timestamp"] - t0).total_seconds() * 1000.0
                reaction_times.append(rt_ms)
            else:
                reaction_times.append(np.nan)

        return pd.DataFrame({
            "task_duration_ms": task_durations,
            "reaction_time_ms": reaction_times
        })

    def avg_reaction_time(self, df=None):
        if df is None:
            df = self.df

        # Preferred: explicit field (if parser extracted it)
        if "reaction_time_ms" in df.columns:
            vals = pd.to_numeric(df["reaction_time_ms"], errors="coerce").dropna()
            if len(vals) > 0:
                return float(vals.mean())

        # Fallback: derive from task_start -> first action
        stats = self._derive_task_stats(df)
        vals = pd.to_numeric(stats["reaction_time_ms"], errors="coerce").dropna()
        return float(vals.mean()) if len(vals) > 0 else np.nan

    def avg_task_duration(self, df=None):
        if df is None:
            df = self.df

        # Preferred: explicit field (if parser extracted it)
        if "duration_ms" in df.columns:
            vals = pd.to_numeric(df["duration_ms"], errors="coerce").dropna()
            if len(vals) > 0:
                return float(vals.mean())

        # Fallback: derive from task_start -> task_end
        stats = self._derive_task_stats(df)
        vals = pd.to_numeric(stats["task_duration_ms"], errors="coerce").dropna()
        return float(vals.mean()) if len(vals) > 0 else np.nan

    def time_per_success(self, df=None):
        if df is None: df = self.df
        hits = df[df["event_role"] == "action_success"]
        if len(hits) == 0:
            return np.nan
        total_time = (df["timestamp"].max() - df["timestamp"].min()).total_seconds()
        return total_time / len(hits)

    def retries_after_end(self, df=None):
        if df is None: df = self.df
        return len(df[df["event_role"] == "task_restart"])

    def voluntary_play_time(self, df=None):
        if df is None: df = self.df
        end = df[df["event_role"] == "session_end"]["timestamp"].max()
        task_end = df[df["event_role"] == "task_end"]["timestamp"].max()

        if pd.notna(end) and pd.notna(task_end):
            diff = (end - task_end).total_seconds()
            return max(0.0, diff)  # Clamp to 0.0 if negative (log artifact)
        return np.nan

    def aid_usage(self, df=None):
        if df is None: df = self.df
        return len(df[df["event_role"] == "help_event"])

    def inactivity_time(self, df=None, threshold=5):
        if df is None: df = self.df
        ts = df["timestamp"].sort_values()
        diffs = ts.diff().dt.total_seconds()
        return diffs[diffs > threshold].sum()

    def first_success_time(self, df=None):
        if df is None: df = self.df
        start = df[df["event_role"] == "session_start"]["timestamp"].min()
        succ = df[df["event_role"] == "action_success"]["timestamp"].min()
        if pd.notna(start) and pd.notna(succ):
            return (succ - start).total_seconds()
        return np.nan

    def sound_localization_time(self, df=None):
        if df is None: df = self.df

        # Check if required events exist
        has_audio = not df[df["event_name"] == "audio_triggered"].empty
        has_head = not df[df["event_name"] == "head_turn"].empty

        if not (has_audio and has_head):
            return None  # Optional metric: return None to skip calculation if events missing

        audio = df[df["event_name"] == "audio_triggered"]["timestamp"].min()
        head = df[df["event_name"] == "head_turn"]["timestamp"].min()

        if pd.notna(audio) and pd.notna(head) and head > audio:
            return (head - audio).total_seconds()
        return None

    def activity_level(self, df=None):
        if df is None: df = self.df
        dur = (df["timestamp"].max() - df["timestamp"].min()).total_seconds() / 60
        if dur <= 0: return np.nan
        return len(df) / dur

    def learning_curve_mean(self, df=None):
        vals = self.learning_curve(df)
        if not vals:
            return 0.0
        return float(np.nanmean(vals))

    # ----------------------------------------------------------------------
    # MAPEO dinámico de nombres → funciones internas
    # ----------------------------------------------------------------------
    def _available_metric_functions(self):
        return {
            "hit_ratio": self.hit_ratio,
            "precision": self.precision,
            "success_rate": self.success_rate,
            "learning_curve_mean": self.learning_curve_mean,

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

            # Logic for optional metrics (return None -> Skip)
            if raw_value is None:
                # Skip this metric entirely from the weighted sum
                continue

            # Logic for NaN (return 0.0 default if mandatory but failed)
            if pd.isna(raw_value):
                raw_value = 0.0

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

        eff_w = profile.get("efectividad_weight", 1)
        efi_w = profile.get("eficiencia_weight", 1)
        sat_w = profile.get("satisfaccion_weight", 1)
        pre_w = profile.get("presencia_weight", 1)

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
        eficiencia = self.compute_category("eficiencia")
        satisfaccion = self.compute_category("satisfaccion")
        presencia = self.compute_category("presencia")

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


