import streamlit as st
import pandas as pd
from pymongo import MongoClient
import json
import os
from datetime import datetime, timezone
from dotenv import load_dotenv
from pathlib import Path

# Configuración de página principal
st.set_page_config(page_title="VR Logger Configurator", page_icon="⚙️", layout="wide")

# Cargar variables de entorno
load_dotenv()

# --- Funciones de base de datos ---
@st.cache_resource
def get_db_client(mongo_uri):
    return MongoClient(mongo_uri)

def load_profiles_from_disk(directory="python_analysis/profiles"):
    os.makedirs(directory, exist_ok=True)
    profiles = {}
    for filename in os.listdir(directory):
        if filename.endswith(".json"):
            with open(os.path.join(directory, filename), 'r', encoding='utf-8') as f:
                try:
                    data = json.load(f)
                    profiles[filename] = data
                except Exception as e:
                    st.error(f"Error loading {filename}: {e}")
    return profiles

def save_profile_to_disk(profile_name, data, directory="python_analysis/profiles"):
    os.makedirs(directory, exist_ok=True)
    safe_name = profile_name.replace(" ", "_").lower()
    if not safe_name.endswith(".json"):
        safe_name += ".json"
    path = os.path.join(directory, safe_name)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4)
    return path

# --- Interfaz Principal ---
st.title("VR Logger & Experiment Configurator")

# Configuración de conexión DB en la barra lateral
st.sidebar.header("Conexión MongoDB")
default_uri = os.getenv("MONGO_URI", "mongodb://localhost:27017")
default_db = os.getenv("DB_NAME", "test")
default_col = os.getenv("COLLECTION_NAME", "tfg")

mongo_uri = st.sidebar.text_input("Mongo URI", value=default_uri)
db_name = st.sidebar.text_input("Database Name", value=default_db)
collection_name = st.sidebar.text_input("Logs Collection", value=default_col)
participants_col_name = st.sidebar.text_input("Participants Collection", value="participants")

try:
    client = get_db_client(mongo_uri)
    db = client[db_name]
    logs_col = db[collection_name]
    parts_col = db[participants_col_name]
    client.admin.command('ping')
    st.sidebar.success("Conectado a MongoDB ✅")
except Exception as e:
    st.sidebar.error(f"Error de conexión: {e}")

quests_col_name = st.sidebar.text_input("Questionnaires Collection", value="questionnaires")
try:
    quests_col = db[quests_col_name]
except Exception:
    pass

tabs = st.tabs(["⚙️ Experimento: Config", "👥 Participantes", "📝 Cuestionarios"])

# Fetch existing participants for dropdowns
available_participants = []
try:
    parts_cursor = parts_col.find({}, {"participant_id": 1}).sort("created_at", -1)
    available_participants = [p.get("participant_id") for p in parts_cursor if "participant_id" in p]
except Exception:
    pass

# -------------------------------------------------------------
# TAB 1: EXPERIMENT CONFIGURATION
# -------------------------------------------------------------
with tabs[0]:
    st.header("Configuración del Experimento (Experiment Profile)")
    
    # Profile Management
    profiles = load_profiles_from_disk()
    col1, col2 = st.columns([3, 1])
    with col1:
        selected_profile_name = st.selectbox("Cargar Perfil Guardado", ["-- Nuevo --"] + list(profiles.keys()))
    with col2:
        if st.button("↻ Refrescar Perfiles", use_container_width=True):
            st.rerun()

    # Pre-cargar datos del perfil seleccionado
    p_data = profiles.get(selected_profile_name, {}) if selected_profile_name != "-- Nuevo --" else {}

    def get_val(path, default):
        # Helper para navegar por el dict seguro
        current = p_data
        for key in path:
            if isinstance(current, dict) and key in current:
                current = current[key]
            else:
                return default
        return current

    # 1. Session Configuration
    st.subheader("Session Configuration")
    colA, colB, colC = st.columns(3)
    with colA:
        session_name = st.text_input("Session Name", value=get_val(["session", "session_name"], "Dia_1"))
        turn_duration = st.number_input("Turn Duration (Seconds)", value=float(get_val(["session", "turn_duration_seconds"], 60.0)))
    with colB:
        group_name = st.text_input("Group Name", value=get_val(["session", "group_name"], "Grupo_A"))
        play_area_w = float(get_val(["session", "play_area_width"], 0.0))
    with colC:
        indep_var = st.text_input("Independent Variable", value=get_val(["session", "independent_variable"], ""))
        play_area_d = float(get_val(["session", "play_area_depth"], 0.0))

    # 2. Participants
    st.subheader("Participants")
    colA, colB = st.columns(2)
    with colA:
        p_count = st.number_input("Participant Count", min_value=1, value=int(get_val(["participants", "count"], 4)))
        manual_user = st.text_input("Manual Participant Name (Override)", value=get_val(["_ui", "manual_participant"], ""))
    with colB:
        default_order = get_val(["participants", "order"], ["GrupoA_U001", "GrupoA_U002"])
        all_options = list(dict.fromkeys(available_participants + default_order))
        safe_defaults = [p for p in default_order if p in all_options]
        p_order_list = st.multiselect("Participant Order", options=all_options, default=safe_defaults)

    # 3. Experiment Info
    st.subheader("Experiment Info")
    colA, colB = st.columns(2)
    with colA:
        exp_id = st.text_input("Experiment ID", value=get_val(["experiment_selection", "experiment_id"], "shooting_basic"))
        formula_prof = st.text_input("Formula Profile", value=get_val(["experiment_selection", "formula_profile"], "default"))
    with colB:
        exp_desc = st.text_area("Description", value=get_val(["experiment_selection", "description"], "Basic shooting experiment"))

    # 4. Modules
    st.subheader("Modules")
    colA, colB, colC = st.columns(3)
    with colA:
        use_gaze = st.checkbox("Use Gaze Tracker", value=bool(get_val(["modules", "useGazeTracker"], True)))
        use_foot = st.checkbox("Use Foot Tracker", value=bool(get_val(["modules", "useFootTracker"], False)))
    with colB:
        use_eye = st.checkbox("Use Eye Tracker", value=bool(get_val(["modules", "useEyeTracker"], False)))
    with colC:
        use_move = st.checkbox("Use Movement Tracker", value=bool(get_val(["modules", "useMovementTracker"], True)))
        use_col = st.checkbox("Use Collision Logger", value=bool(get_val(["modules", "useCollisionLogger"], True)))
        use_hand = st.checkbox("Use Hand Tracker", value=bool(get_val(["modules", "useHandTracker"], False)))

    # 5. Flow
    st.subheader("Flow & GM Controls")
    colA, colB, colC = st.columns(3)
    with colA:
        flow_mode = st.selectbox("Flow Mode", ["turns", "manual"], index=0 if get_val(["participant_flow", "mode"], "turns") == "turns" else 1)
    with colB:
        end_cond = st.selectbox("End Condition", ["timer", "gm"], index=0 if get_val(["participant_flow", "end_condition"], "timer") == "timer" else 1)
    with colC:
        gm_enable = st.checkbox("Enable GM Controls", value=bool(get_val(["participant_flow", "gm_controls", "enabled"], True)))

    # 6. Metrics
    st.subheader("Metrics Setting (Weightings)")
    st.markdown("Ajusta los pesos principales de todas las métricas. Corresponde a MetricsCategory de Unity.")

    # Helper function for metric configs
    def render_metric_config(cat_key, metric_key, default_weight, default_min, default_max, default_invert, title):
        col1, col2, col3, col4, col5 = st.columns([0.5, 3, 2, 2, 2])
        m_data = get_val(["metrics", cat_key, metric_key], {})
        enabled = col1.checkbox("", value=bool(m_data.get("enabled", True)), key=f"{cat_key}_{metric_key}_e")
        weight = col2.slider(title, 0.0, 1.0, float(m_data.get("weight", default_weight)), key=f"{cat_key}_{metric_key}_w")
        min_v = col3.number_input("Min", value=float(m_data.get("min", default_min)), key=f"{cat_key}_{metric_key}_min")
        max_v = col4.number_input("Max", value=float(m_data.get("max", default_max)), key=f"{cat_key}_{metric_key}_max")
        invert = col5.checkbox("Invert", value=bool(m_data.get("invert", default_invert)), key=f"{cat_key}_{metric_key}_inv")
        return {"enabled": enabled, "weight": weight, "min": min_v, "max": max_v, "invert": invert}

    metrics_final = {"efectividad": {}, "eficiencia": {}, "satisfaccion": {}, "presencia": {}}

    with st.expander("Efectividad", expanded=False):
        metrics_final["efectividad"]["hit_ratio"] = render_metric_config("efectividad", "hit_ratio", 0.35, 0, 1, False, "Hit Ratio")
        metrics_final["efectividad"]["success_rate"] = render_metric_config("efectividad", "success_rate", 0.30, 0, 1, False, "Success Rate")
        metrics_final["efectividad"]["learning_curve_mean"] = render_metric_config("efectividad", "learning_curve_mean", 0.15, 0, 1, False, "Learning Curve Mean")
        metrics_final["efectividad"]["progression"] = render_metric_config("efectividad", "progression", 0.10, 0, 10, False, "Progression")
        metrics_final["efectividad"]["success_after_restart"] = render_metric_config("efectividad", "success_after_restart", 0.10, 0, 1, False, "Success After Restart")

    with st.expander("Eficiencia", expanded=False):
        metrics_final["eficiencia"]["avg_reaction_time_ms"] = render_metric_config("eficiencia", "avg_reaction_time_ms", 0.40, 100, 5000, True, "Avg Reaction Time(ms)")
        metrics_final["eficiencia"]["avg_task_duration_ms"] = render_metric_config("eficiencia", "avg_task_duration_ms", 0.30, 1000, 30000, True, "Avg Task Duration(ms)")
        metrics_final["eficiencia"]["time_per_success_s"] = render_metric_config("eficiencia", "time_per_success_s", 0.20, 0, 60, True, "Time Per Success(s)")
        metrics_final["eficiencia"]["navigation_errors"] = render_metric_config("eficiencia", "navigation_errors", 0.10, 0, 10, True, "Navigation Errors")

    with st.expander("Satisfaccion", expanded=False):
        metrics_final["satisfaccion"]["learning_stability"] = render_metric_config("satisfaccion", "learning_stability", 0.30, 0, 1, False, "Learning Stability")
        metrics_final["satisfaccion"]["error_reduction_rate"] = render_metric_config("satisfaccion", "error_reduction_rate", 0.25, 0, 1, False, "Error Reduction Rate")
        metrics_final["satisfaccion"]["voluntary_play_time_s"] = render_metric_config("satisfaccion", "voluntary_play_time_s", 0.25, 0, 60, False, "Voluntary Play Time(s)")
        metrics_final["satisfaccion"]["aid_usage"] = render_metric_config("satisfaccion", "aid_usage", 0.10, 0, 5, True, "Aid Usage")
        metrics_final["satisfaccion"]["interface_errors"] = render_metric_config("satisfaccion", "interface_errors", 0.10, 0, 5, True, "Interface Errors")

    with st.expander("Presencia", expanded=False):
        metrics_final["presencia"]["activity_level_per_min"] = render_metric_config("presencia", "activity_level_per_min", 0.25, 0, 100, False, "Activity Level / Min")
        metrics_final["presencia"]["first_success_time_s"] = render_metric_config("presencia", "first_success_time_s", 0.25, 0, 30, True, "First Success Time(s)")
        metrics_final["presencia"]["inactivity_time_s"] = render_metric_config("presencia", "inactivity_time_s", 0.20, 0, 60, True, "Inactivity Time(s)")
        metrics_final["presencia"]["sound_localization_time_s"] = render_metric_config("presencia", "sound_localization_time_s", 0.15, 0, 10, True, "Sound Localiz. Time(s)")
        metrics_final["presencia"]["audio_performance_gain"] = render_metric_config("presencia", "audio_performance_gain", 0.15, -1, 1, False, "Audio Perf. Gain")

    # 7. Event Roles Mapping
    st.subheader("Event Roles Mapping")
    st.markdown("Mapeos adicionales a los que están harcodeados en Unity (ej. `MatarEnemigo` -> `action_success`).")
    existing_roles = get_val(["event_roles"], {})
    
    # We use session state for dynamic lists
    if "roles" not in st.session_state:
        # load from file
        st.session_state.roles = list(existing_roles.items())

    new_event = st.text_input("New Event Name (e.g. boss_killed)")
    new_role = st.selectbox("Role", [
        "action_success", "action_fail", "task_start", "task_end", "task_restart", "task_abandoned", 
        "session_start", "session_end", "navigation_error", "interface_error", "interface_action", 
        "help_event", "movement_update", "movement_action", "exploration_event", "gaze_event", 
        "gaze_action", "gaze_sample", "interaction_event", "audio_event", "audio_reaction", 
        "inactivity_event", "system_event", "performance_measure", "custom_event"
    ])
    
    if st.button("Add Role Mapping") and new_event:
        st.session_state.roles.append((new_event, new_role))
    
    final_roles = {}
    for i, (ev, rl) in enumerate(st.session_state.roles):
        colA, colB, colC = st.columns([3,3,1])
        colA.text(ev)
        colB.text(rl)
        if colC.button("🗑️", key=f"del_role_{i}"):
             st.session_state.roles.pop(i)
             st.rerun()
        else:
             final_roles[ev] = rl

    # -----------------------------------------------------------------
    # CUSTOM METRICS
    # -----------------------------------------------------------------
    st.subheader("Extra: Creación de Métricas Personalizadas")
    st.markdown("Añade métricas nuevas que el script de Python calculará dinámicamente.")
    
    if "custom_metrics" not in st.session_state:
        # Load any existing non-standard metrics from config
        std_keys = [
            "hit_ratio", "success_rate", "learning_curve_mean", "progression", "success_after_restart",
            "avg_reaction_time_ms", "avg_task_duration_ms", "time_per_success_s", "navigation_errors",
            "learning_stability", "error_reduction_rate", "voluntary_play_time_s", "aid_usage", "interface_errors",
            "activity_level_per_min", "first_success_time_s", "inactivity_time_s", "sound_localization_time_s", "audio_performance_gain"
        ]
        loaded_customs = []
        for cat_k, cat_v in get_val(["metrics"], {}).items():
            if isinstance(cat_v, dict):
                for m_k, m_v in cat_v.items():
                    if m_k not in std_keys and isinstance(m_v, dict) and "target_event" in m_v:
                        loaded_customs.append({
                            "category": cat_k,
                            "name": m_k,
                            "target_event": m_v.get("target_event", ""),
                            "aggregation": m_v.get("aggregation", "count"),
                            "weight": m_v.get("weight", 0.1),
                            "min": m_v.get("min", 0.0),
                            "max": m_v.get("max", 100.0),
                            "invert": m_v.get("invert", False)
                        })
        st.session_state.custom_metrics = loaded_customs

    with st.expander("Crear nueva Métrica Personalizada"):
        c_cat = st.selectbox("Categoría", ["efectividad", "eficiencia", "satisfaccion", "presencia"])
        c_name = st.text_input("Nombre de la Métrica (ej: items_recogidos)")
        c_target = st.text_input("Target Event (Nombre exacto del evento en Unity)")
        c_agg = st.selectbox("Aggregation", ["count", "sum", "average", "max", "min"])
        
        c_col1, c_col2, c_col3, c_col4 = st.columns(4)
        c_weight = c_col1.number_input("Weight", 0.0, 1.0, 0.1)
        c_min = c_col2.number_input("Min Val", value=0.0)
        c_max = c_col3.number_input("Max Val", value=100.0)
        c_inv = c_col4.checkbox("Invertir", value=False)
        
        if st.button("Añadir Métrica") and c_name and c_target:
            st.session_state.custom_metrics.append({
                "category": c_cat, "name": c_name.strip().lower().replace(" ", "_"),
                "target_event": c_target, "aggregation": c_agg,
                "weight": c_weight, "min": c_min, "max": c_max, "invert": c_inv
            })
            st.success(f"Añadida métrica: {c_name}")
            
    # Display and delete custom metrics, and apply to final dict
    for i, cm in enumerate(st.session_state.custom_metrics):
        cc1, cc2 = st.columns([5, 1])
        cc1.text(f"[{cm['category'].upper()}] {cm['name']} -> Busca: '{cm['target_event']}' ({cm['aggregation']}) | Peso: {cm['weight']}")
        if cc2.button("🗑️", key=f"del_cm_{i}"):
            st.session_state.custom_metrics.pop(i)
            st.rerun()
        else:
            # Apply to final dict
            metrics_final[cm['category']][cm['name']] = {
                "enabled": True,
                "weight": cm['weight'],
                "min": cm['min'],
                "max": cm['max'],
                "invert": cm['invert'],
                "target_event": cm['target_event'],
                "aggregation": cm['aggregation']
            }

    # Construir el JSON de Configuración Final

    if manual_user:
        p_count = 1
        p_order_list = [manual_user]

    # Diccionario final replicando la estructura de ExperimentConfig.cs
    final_config_json = {
        "session": {
            "session_name": session_name,
            "group_name": group_name,
            "independent_variable": indep_var,
            "turn_duration_seconds": turn_duration,
            "play_area_width": play_area_w,
            "play_area_depth": play_area_d
        },
        "participants": {
            "count": p_count,
            "order": p_order_list
        },
        "experiment_selection": {
            "experiment_id": exp_id,
            "formula_profile": formula_prof,
            "description": exp_desc
        },
        "modules": {
            "useGazeTracker": use_gaze,
            "useEyeTracker": use_eye,
            "useMovementTracker": use_move,
            "useHandTracker": use_hand,
            "useFootTracker": use_foot,
            "useRaycastLogger": True, # Hardcoded True
            "useCollisionLogger": use_col
        },
        "participant_flow": {
            "mode": flow_mode,
            "end_condition": end_cond,
            "gm_controls": {
                "enabled": gm_enable
            }
        },
        "metrics": metrics_final,
        "event_roles": final_roles,
        "custom_events": get_val(["custom_events"], {}),
        "_ui": { "manual_participant": manual_user } # Para el configurador web
    }

    
    st.divider()
    col1, col2 = st.columns(2)
    
    with col1:
        st.subheader("💾 Guardar Perfil Localmente")
        save_name = st.text_input("Profile Name", value=session_name)
        if st.button("Guardar Perfil (JSON)"):
            path = save_profile_to_disk(save_name, final_config_json)
            st.success(f"Perfil guardado en {path}")
            
    with col2:
        st.subheader("🚀 Comenzar Experimento (Enviar DB)")
        st.markdown("Esto enviará el config log a MongoDB como si lo mandase el Inspector de Unity, para que `python_analysis` lo lea.")
        if st.button("Push Configuration to MongoDB", type="primary"):
            try:
                # Construir el Documento a la manera de LoggerService.cs
                log_doc = {
                    "timestamp": datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z",
                    "user_id": manual_user if manual_user else "WEB_CONFIG",
                    "event_type": "config",
                    "event_name": "experiment_config",
                    "save": True,
                    "event_value": None,
                    "session_id": session_name,
                    "group_id": group_name,
                    "event_context": final_config_json
                }
                
                logs_col.insert_one(log_doc)
                st.success("✅ Configuración enviada a MongoDB con éxito.")
                with st.expander("Ver JSON enviado"):
                    st.json(final_config_json)
            except Exception as e:
                st.error(f"❌ Fallo al insertar en MongoDB: {e}")

# -------------------------------------------------------------
# TAB 2: PARTICIPANTS
# -------------------------------------------------------------
with tabs[1]:
    st.header("Gestión de Participantes")
    st.markdown(f"Colección de MongoDB: `{participants_col_name}`")
    
    col1, col2 = st.columns([1, 2])
    
    with col1:
        st.subheader("Añadir Nuevo Participante")
        with st.form("participant_form"):
            col_id1, col_id2 = st.columns(2)
            group_id = col_id1.text_input("Grupo (Ej: GrupoA)", value="GrupoA")
            user_id = col_id2.text_input("ID Usuario (Ej: U005)")
            age = st.number_input("Edad", min_value=10, max_value=99, value=25)
            gender = st.selectbox("Género", ["Masculino", "Femenino", "Otro", "Prefiero no decirlo"])
            vr_exp = st.selectbox("Experiencia Previa en VR", ["Ninguna", "Poca", "Media", "Alta", "Experto"])
            notes = st.text_area("Notas / Observaciones")
            
            submit_participant = st.form_submit_button("Guardar Participante", type="primary")
            
            if submit_participant:
                if group_id and user_id:
                    new_id = f"{group_id}_{user_id}"
                    part_doc = {
                        "participant_id": new_id,
                        "age": age,
                        "gender": gender,
                        "vr_experience": vr_exp,
                        "notes": notes,
                        "created_at": datetime.now(timezone.utc)
                    }
                    try:
                        # Upsert basándose en el ID
                        parts_col.update_one(
                            {"participant_id": new_id},
                            {"$set": part_doc},
                            upsert=True
                        )
                        st.success(f"Participante {new_id} guardado correctamente.")
                    except Exception as e:
                        st.error(f"Error guardando participante: {e}")
                else:
                    st.error("El Grupo y el ID del participante son obligatorios.")
                    
    with col2:
        st.subheader("Participantes Registrados")
        if st.button("Actualizar Tabla"):
            st.rerun()
            
        try:
            participants_cursor = parts_col.find().sort("created_at", -1)
            parts_list = list(participants_cursor)
            
            if len(parts_list) > 0:
                # Remove MongoDB _id for display
                for p in parts_list:
                    p.pop('_id', None)
                    if 'created_at' in p:
                        if isinstance(p['created_at'], str):
                            pass
                        else:
                            p['created_at'] = p['created_at'].strftime("%Y-%m-%d %H:%M")
                
                df_parts = pd.DataFrame(parts_list)
                st.dataframe(df_parts, use_container_width=True)
            else:
                st.info("No hay participantes registrados aún en esta colección.")
        except Exception as e:
            st.error(f"No se pudo cargar la lista: {e}")

# -------------------------------------------------------------
# TAB 3: QUESTIONNAIRES (SUS + Custom Metrics)
# -------------------------------------------------------------
with tabs[2]:
    st.header("📝 Cuestionarios Subjetivos")
    st.markdown("Selecciona un participante de la base de datos y entrégale la pantalla para que valore su experiencia.")
    
    if not available_participants:
        st.warning("⚠️ No hay participantes registrados en la base de datos. Ve a la pestaña 'Participantes' para añadir uno.")
    else:
        selected_q_user = st.selectbox("👤 Selecciona al Participante:", ["-- Seleccionar --"] + available_participants)
        
        if selected_q_user != "-- Seleccionar --":
            st.divider()
            st.subheader(f"System Usability Scale (SUS) para: {selected_q_user}")
            st.markdown("Por favor, valora de 1 (Totalmente en desacuerdo) a 5 (Totalmente de acuerdo).")
            
            sus_questions = [
                "1. Creo que me gustará usar con frecuencia este sistema.",
                "2. Encontré el sistema innecesariamente complejo.",
                "3. Me pareció que el sistema era fácil de usar.",
                "4. Creo que necesitaría el apoyo de una persona técnica para poder usar este sistema.",
                "5. Encontré que las diversas funciones en este sistema estaban bien integradas.",
                "6. Pensé que había demasiada inconsistencia en este sistema.",
                "7. Imaginaría que la mayoría de las personas aprenderían a usar este sistema muy rápidamente.",
                "8. Encontré el sistema muy engorroso de usar.",
                "9. Me sentí muy confiado usando el sistema.",
                "10. Necesité aprender muchas cosas antes de poder empezar con el sistema."
            ]
            
            with st.form("questionnaire_form"):
                sus_answers = []
                for q in sus_questions:
                    ans = st.radio(q, [1, 2, 3, 4, 5], index=2, horizontal=True)
                    sus_answers.append(ans)
                    
                st.divider()
                st.subheader("Valoración Específica del Experimento")
                st.markdown("Valora tu percepción sobre los siguientes aspectos de 1 (Muy baja) a 5 (Muy alta):")
                
                colE1, colE2 = st.columns(2)
                sub_efectividad = colE1.slider("🎯 Efectividad (¿Lograste tus metas de forma precisa?)", 1, 5, 3)
                sub_eficiencia = colE2.slider("⚡ Eficiencia (¿El esfuerzo y tiempo requerido fue adecuado?)", 1, 5, 3)
                sub_satisfaccion = colE1.slider("😊 Satisfacción (¿Disfrutaste la experiencia?)", 1, 5, 3)
                sub_presencia = colE2.slider("🌍 Presencia (¿Te sentiste inmerso en el entorno virtual?)", 1, 5, 3)
                
                submit_q = st.form_submit_button("Guardar y Enviar", type="primary")
                
                if submit_q:
                    sum_score = 0
                    for i, val in enumerate(sus_answers):
                        if (i + 1) % 2 != 0: # Impar
                            sum_score += (val - 1)
                        else: # Par
                            sum_score += (5 - val)
                    sus_final = sum_score * 2.5
                    
                    q_doc = {
                        "user_id": selected_q_user,
                        "sus_score": sus_final,
                        "sus_answers": sus_answers,
                        "subj_efectividad": sub_efectividad,
                        "subj_eficiencia": sub_eficiencia,
                        "subj_satisfaccion": sub_satisfaccion,
                        "subj_presencia": sub_presencia,
                        "timestamp": datetime.now(timezone.utc)
                    }
                    
                    try:
                        quests_col.update_one({"user_id": selected_q_user}, {"$set": q_doc}, upsert=True)
                        st.success(f"¡Cuestionario guardado con éxito! Puntuación SUS final: {sus_final}/100")
                    except Exception as e:
                        st.error(f"Error al guardar: {e}")

