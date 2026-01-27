import pandas as pd
from pymongo import MongoClient
import json
import os
from dotenv import load_dotenv

# Cargar variables de entorno desde el archivo .env (si existe)
load_dotenv()


class LogParser:
    def __init__(self, mongo_uri=None, db_name=None, collection_name=None):
        # Prioridad: Argumento > Variable de Entorno > Default Hardcoded
        self.mongo_uri = mongo_uri or os.getenv("MONGO_URI", "mongodb://localhost:27017")
        self.db_name = db_name or os.getenv("DB_NAME", "test")
        self.collection_name = collection_name or os.getenv("COLLECTION_NAME", "tfg")

        self.client = MongoClient(self.mongo_uri)
        self.collection = self.client[self.db_name][self.collection_name]

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
