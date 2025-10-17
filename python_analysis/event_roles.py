EVENT_ROLE_MAP = {
    # Éxitos y fallos en tareas (disparos, colocación, movimiento)
    "target_hit": "action_success",
    "target_miss": "action_fail",
    "goal_reached": "action_success",
    "fall_detected": "action_fail",
    "object_placed_correctly": "action_success",
    "object_dropped": "action_fail",

    # Control general de tareas
    "task_start": "task_start",
    "task_end": "task_end",
    "task_restart": "task_restart",
    "session_start": "session_start",
    "session_end": "session_end",

    # Errores e interacción
    "collision": "navigation_error",
    "navigation_error": "navigation_error",
    "controller_error": "interface_error",
    "wrong_button": "interface_error",

    # Ayuda o guía
    "help_requested": "help_event",
    "guide_used": "help_event",
    "hint_used": "help_event",
}
