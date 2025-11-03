import json
from pathlib import Path


class ConfigSystem:
    """
    Config System para VR USER EVALUATION.
    Carga y gestiona la configuración de:
        - Mapeo de eventos → roles semánticos
        - Pesos y rangos de normalización de métricas
        - Definiciones de eventos personalizados
    """

    def __init__(self, config_path="python_analysis/config_system.json"):
        config_file = Path(config_path)
        if not config_file.exists():
            raise FileNotFoundError(f"No se encontró el archivo de configuración: {config_file}")

        with open(config_file, "r", encoding="utf-8") as f:
            self.data = json.load(f)

        self.event_roles = self.data.get("event_roles", {})
        self.metrics = self.data.get("metrics", {})
        self.custom_events = self.data.get("custom_events", {})

    # ============================================================
    # EVENTOS
    # ============================================================
    def get_event_role(self, event_name: str):
        """Devuelve el rol semántico asociado a un evento."""
        return self.event_roles.get(event_name, "other")

    # ============================================================
    # MÉTRICAS
    # ============================================================
    def get_metric_config(self, category: str):
        """Devuelve el bloque completo de configuración de una categoría (efectividad, eficiencia, etc.)"""
        return self.metrics.get(category, {})

    def get_all_metric_configs(self):
        """Devuelve todos los bloques de configuración de métricas"""
        return self.metrics

    # ============================================================
    # CUSTOM EVENTS
    # ============================================================
    def get_custom_event_type(self, event_name: str):
        """Clasifica un evento personalizado."""
        return self.custom_events.get(event_name, "custom_event")

    # ============================================================
    # METADATOS
    # ============================================================
    def get_version_info(self):
        """Devuelve metadatos de la configuración actual."""
        return {
            "version": self.data.get("version", "unknown"),
            "description": self.data.get("description", ""),
            "last_updated": self.data.get("last_updated", "")
        }
