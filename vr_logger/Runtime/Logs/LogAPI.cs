using System.Threading.Tasks;
using UnityEngine;

namespace VRLogger
{
    public static class LogAPI
    {
        // -----------------------
        // Sesiones
        // -----------------------
        public static Task LogSessionStart(string sessionId, string independentVariable = null) =>
            LoggerService.LogEvent("system", "session_start", null, new { session_id = sessionId, independent_variable = independentVariable });

        public static Task LogSessionEnd(string sessionId) =>
            LoggerService.LogEvent("system", "session_end", null, new { session_id = sessionId });

        public static Task LogUserExit(string reason = "early_leave") =>
            LoggerService.LogEvent("system", "user_exit", reason);

        // -----------------------
        // Tareas
        // -----------------------
        public static Task LogTaskStart(string taskId) =>
            LoggerService.LogEvent("task", "task_start", null, new { task_id = taskId });

        public static Task LogTaskEnd(string taskId, string result, float durationMs, int errors = 0) =>
            LoggerService.LogEvent("task", "task_end", result, new { task_id = taskId, duration_ms = durationMs, errors });

        public static Task LogTaskTimeout(string taskId) =>
            LoggerService.LogEvent("task", "task_timeout", null, new { task_id = taskId });

        public static Task LogTaskAbandoned(string taskId, string reason = "manual_exit") =>
            LoggerService.LogEvent("task", "task_abandoned", null, new { task_id = taskId, reason });

        public static Task LogTaskRestart(string taskId) =>
            LoggerService.LogEvent("task", "task_restart", null, new { task_id = taskId });

        // -----------------------
        // Objetivos / Disparo
        // -----------------------
        public static Task LogTargetAppeared(string targetId) =>
            LoggerService.LogEvent("task", "target_appeared", null, new { target_id = targetId });

        public static Task LogAimStart(string targetId) =>
            LoggerService.LogEvent("task", "aim_start", null, new { target_id = targetId });

        public static Task LogTriggerPull(string targetId) =>
            LoggerService.LogEvent("task", "trigger_pull", null, new { target_id = targetId });

        public static Task LogTargetHit(string targetId, int shots, float reactionTimeMs) =>
            LoggerService.LogEvent("task", "target_hit", 1, new { target_id = targetId, shots_fired = shots, reaction_time_ms = reactionTimeMs });

        public static Task LogTargetMiss(string targetId, float reactionTimeMs) =>
            LoggerService.LogEvent("task", "target_miss", 0, new { target_id = targetId, reaction_time_ms = reactionTimeMs });

        // -----------------------
        // Interacciones
        // -----------------------
        public static Task LogObjectInteraction(string objectId, string type) =>
            LoggerService.LogEvent("interaction", "object_interaction", null, new { object_id = objectId, action = type });

        public static Task LogUIInteraction(string element, string action) =>
            LoggerService.LogEvent("interaction", "ui_interaction", null, new { element, action });

        public static Task LogControllerAction(string objectId, string type) =>
            LoggerService.LogEvent("interaction", "controller_action", null, new { type, object_id = objectId });

        // -----------------------
        // Errores
        // -----------------------
        public static Task LogNavigationError(string errorType) =>
            LoggerService.LogEvent("error", "navigation_error", null, new { type = errorType });

        public static Task LogControllerError(string button) =>
            LoggerService.LogEvent("error", "controller_error", null, new { button });

        public static Task LogWrongButton(string button) =>
            LoggerService.LogEvent("error", "wrong_button", null, new { button });

        // -----------------------
        // Colisiones
        // -----------------------
        public static Task LogCollision(string objectId, float intensity) =>
            LoggerService.LogEvent("navigation", "collision", null, new { object_id = objectId, intensity });

        // -----------------------
        // Gaze / atención
        // -----------------------
        public static Task LogGazeSustained(string target, float durationMs) =>
            LoggerService.LogEvent("gaze", "gaze_sustained", null, new { target, duration_ms = durationMs });

        public static Task LogGazeFrequencyChange(float prevHz, float currentHz) =>
            LoggerService.LogEvent("gaze", "gaze_frequency_change", null, new { previous_hz = prevHz, current_hz = currentHz });

        public static Task LogActionWithGazeCheck(string button, bool gazeOnTarget) =>
            LoggerService.LogEvent("gaze", "action_with_gaze_check", null, new { button, gaze_on_target = gazeOnTarget });

        // -----------------------
        // Trayectoria / movimiento
        // -----------------------
        public static Task LogMovementFrame(Vector3 pos, Vector3 velocity, float dirChangeDeg) =>
            LoggerService.LogEvent("navigation", "movement_frame", null, new { pos, velocity, dir_change_deg = dirChangeDeg });

        public static Task LogSharpTurn(float angleDeg, float timeMs) =>
            LoggerService.LogEvent("navigation", "sharp_turn", null, new { angle_deg = angleDeg, time_ms = timeMs });

        // -----------------------
        // Ayudas
        // -----------------------
        public static Task LogHelpRequested(string helpType, string taskId) =>
            LoggerService.LogEvent("interaction", "help_requested", null, new { help_type = helpType, task_id = taskId });

        public static Task LogGuideUsed(string guideId) =>
            LoggerService.LogEvent("interaction", "guide_used", null, new { guide_id = guideId });

        public static Task LogHintUsed(string hintId) =>
            LoggerService.LogEvent("interaction", "hint_used", null, new { hint_id = hintId });

        // -----------------------
        // Audio / sonido
        // -----------------------
        public static Task LogAudioTriggered(string sourceId) =>
            LoggerService.LogEvent("system", "audio_triggered", null, new { source_id = sourceId });

        public static Task LogHeadTurn(Vector3 dir, float timeMs) =>
            LoggerService.LogEvent("system", "head_turn", null, new { dir, reaction_time_ms = timeMs });
    }
}
