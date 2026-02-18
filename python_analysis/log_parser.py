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
                raw_context = log["event_context"]
                for k, v in raw_context.items():
                    if k in ["session_id", "group_id"]:
                        continue
                    
                    # Flatten Vector3-like dicts (x, y, z)
                    if isinstance(v, dict) and all(key in v for key in ["x", "y", "z"]):
                        context[f"{k}_x"] = v["x"]
                        context[f"{k}_y"] = v["y"]
                        context[f"{k}_z"] = v["z"]
                        # Also keep original for reference if needed? No, flat is better for dataframe
                    else:
                        context[k] = v

            if expand_context:
                # Expandir el contexto en columnas separadas
                row = {**base, **context}
            else:
                # Guardar contexto como JSON string (útil para exportación o debugging)
                row = {**base, "context": json.dumps(context)}

            parsed.append(row)

        df = pd.DataFrame(parsed)

        # Asegurar orden de columnas más útil (pero manteniendo las demás)
        priority_cols = [
            "timestamp", "user_id", "group_id", "session_id",
            "event_type", "event_name", "event_value", "context"
        ]
        # Columns that exist in df and are in priority list
        existing_priority = [c for c in priority_cols if c in df.columns]
        # Columns that exist in df but are NOT in priority list
        other_cols = [c for c in df.columns if c not in priority_cols]

        # Combine
        df = df[existing_priority + other_cols]

        return df

    def close(self):
        self.client.close()
