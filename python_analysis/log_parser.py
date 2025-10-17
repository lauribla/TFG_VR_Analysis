import pandas as pd
from pymongo import MongoClient
import json

class LogParser:
    def __init__(self, mongo_uri="mongodb://localhost:27017", db_name="test", collection_name="tfg"):
        self.client = MongoClient(mongo_uri)
        self.collection = self.client[db_name][collection_name]

    def fetch_logs(self, query=None, limit=0):
        """
        Carga logs desde MongoDB.
        """
        cursor = self.collection.find(query or {}).limit(limit)
        return list(cursor)

    def parse_logs(self, logs, expand_context=False):
        """
        Convierte los logs JSON en un DataFrame con campos relevantes.
        :param expand_context: si True, expande los campos de 'event_context' en columnas.
        """
        parsed = []
        for log in logs:
            # Intentar leer session_id y group_id tanto desde nivel raíz como desde event_context
            session_id = log.get("session_id") or log.get("event_context", {}).get("session_id")
            group_id = log.get("group_id") or log.get("event_context", {}).get("group_id")

            base = {
                "timestamp": pd.to_datetime(log.get("timestamp")),
                "user_id": log.get("user_id", "UNKNOWN"),
                "group_id": group_id,
                "session_id": session_id,
                "event_type": log.get("event_type", "undefined"),
                "event_name": log.get("event_name", "undefined"),
                "event_value": log.get("event_value", None),
            }

            # Intentar convertir event_value a número si es posible
            try:
                base["event_value"] = float(base["event_value"])
            except (ValueError, TypeError):
                pass  # dejarlo como está si no es convertible

            # Contexto dinámico
            context = {}
            if "event_context" in log and isinstance(log["event_context"], dict):
                context = {k: v for k, v in log["event_context"].items()
                           if k not in ["session_id", "group_id"]}

            if expand_context:
                # Expandir el contexto en columnas separadas
                row = {**base, **context}
            else:
                # Guardar contexto como JSON string (útil para exportación o debugging)
                row = {**base, "context": json.dumps(context)}

            parsed.append(row)

        df = pd.DataFrame(parsed)

        # Asegurar orden de columnas más útil
        column_order = [
            "timestamp", "user_id", "group_id", "session_id",
            "event_type", "event_name", "event_value", "context"
        ]
        df = df[[c for c in column_order if c in df.columns]]

        return df

    def close(self):
        self.client.close()
