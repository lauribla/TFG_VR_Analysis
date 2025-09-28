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
            base = {
                "timestamp": pd.to_datetime(log.get("timestamp")),
                "user_id": log.get("user_id"),
                "event_type": log.get("event_type"),
                "event_name": log.get("event_name"),
                "event_value": log.get("event_value"),
                "session_id": log.get("event_context", {}).get("session_id"),
                "group_id": log.get("event_context", {}).get("group_id"),
            }

            # Contexto dinámico
            context = {k: v for k, v in log.get("event_context", {}).items()
                       if k not in ["session_id", "group_id"]}

            if expand_context:
                # Expandir el contexto en columnas separadas
                row = {**base, **context}
            else:
                # Guardar contexto como JSON string
                row = {**base, "context": json.dumps(context)}

            parsed.append(row)

        return pd.DataFrame(parsed)

    def close(self):
        self.client.close()
