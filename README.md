# 🧠 VR USER EVALUATION – README ACTUALIZADO (Mapeo Semántico + Modos Global/Agrupado)

## 📘 Descripción general

Sistema modular para **monitorizar, almacenar y analizar el comportamiento de usuarios en entornos VR**, combinando **Unity + MongoDB + Python**.

Incluye:

* SDK de **logging universal para Unity** (eventos, sesiones, trackers, roles semánticos).
* **Base de datos MongoDB** (local o remota) para registro estructurado.
* **Pipeline de análisis automático** en Python (efectividad, eficiencia, satisfacción, presencia, custom events).
* **Informes PDF** y **dashboard interactivo** (Streamlit/Plotly).

---

## 📂 Estructura del repositorio

```
TFG_VR_Analysis/
│
├─ python_analysis/                     # Análisis y métricas
│  ├─ metrics.py                        # Cálculo de indicadores + mapeo de eventos
│  ├─ log_parser.py                     # Lectura de logs desde MongoDB
│  ├─ exporter.py                       # Exportación JSON/CSV
│  ├─ vr_analysis.py                    # Orquestador principal
│  └─ __init__.py
│
├─ python_visualization/                # Visualización e informes
│  ├─ visualize_groups.py               # Gráficas automáticas
│  ├─ visual_dashboard.py               # Dashboard Streamlit
│  ├─ pdf_reporter.py                   # Informe PDF (global + agrupado)
│  └─ __init__.py
│
├─ vr_logger/                           # SDK Unity (logging runtime)
│  ├─ Runtime/
│  │              
│  │  ├─ Manager/          
│  │  ├─ Trackers/                      # Gaze, Hand, Movement, Foot trackers
│  │  └─ Logs/                          # Loggers específicos (collision, raycast...)
│  └─ README.md                         # Manual Unity SDK
│
├─ requirements.txt                     # Dependencias Python
├─ DLLS_MONGO_Unity.zip                 # Librerías MongoDB para Unity
└─ README.md (este archivo)
```

> 📦 Exportaciones automáticas: `python_analysis/pruebas/exports_YYYYMMDD_HHMMSS/`
>
> Figuras y PDF: `python_analysis/pruebas/figures_YYYYMMDD_HHMMSS/`

---

## ⚙️ Novedades principales

### 🔗 Sistema de Mapeo Semántico de Eventos (`event_role`)

Para garantizar compatibilidad entre distintos tipos de experimentos (disparos, parkour, manipulación de objetos, etc.), el sistema introduce **roles de evento estandarizados**.

Cada evento puede etiquetarse con un rol genérico, independientemente de su nombre:

| Rol semántico (`event_role`) | Ejemplos detectados automáticamente           |
| ---------------------------- | --------------------------------------------- |
| `action_success`             | `target_hit`, `goal_reached`, `object_placed` |
| `action_fail`                | `target_miss`, `fall_detected`, `drop_error`  |
| `task_start`                 | `task_start`, `mission_begin`                 |
| `task_end`                   | `task_end`, `mission_complete`                |
| `navigation_error`           | `collision`, `path_error`                     |
| `interaction_help`           | `help_requested`, `hint_used`, `guide_used`   |

Esto permite calcular indicadores (efectividad, eficiencia, etc.) **sin depender de nombres concretos**.
El mapeo se gestiona automáticamente en `metrics.py`, pero también puede ampliarse manualmente según el dominio del experimento.

---

### 🔄 Modos de Análisis: *Global* y *Agrupado*

El pipeline Python ahora permite dos modos complementarios:

| Modo         | Descripción                                                                                                                   | Salida                                   |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------- |
| **Global**   | Agrega todas las sesiones y usuarios. Ideal para comparar condiciones experimentales o grupos.                                | `group_results.json`, `final_report.pdf` |
| **Agrupado** | Calcula todas las métricas por `user_id`, `group_id` y `session_id`. Permite ver el rendimiento individual y la variabilidad. | `grouped_metrics.csv`, `PDF agrupado`    |

Los modos pueden activarse/desactivarse desde `vr_analysis.py`:

```python
GENERAR_GLOBAL = True
GENERAR_AGRUPADO = True
```

Ambos se generan en carpetas separadas dentro de `python_analysis/pruebas/figures_*`.

El dashboard y el PDF reconocen automáticamente el modo al cargar los datos.

---

## 🚀 Flujo completo de uso

1. **Unity (VR Logger)**

   * Envía eventos a MongoDB con `LoggerService.LogEvent()`.
   * `UserSessionManager` agrega automáticamente `user_id`, `group_id`, `session_id`.

2. **MongoDB**

   * Recibe y almacena eventos en JSON estructurado.

3. **Python Analysis**

   * `log_parser.py` convierte los eventos en un `DataFrame`.
   * `metrics.py` calcula todas las métricas oficiales y personalizadas.

4. **Resultados automáticos**

   * Exportación de resultados globales y agrupados.
   * Generación de gráficas y PDF final.

5. **Dashboard interactivo**

   * Permite filtrar por usuario, grupo o sesión.
   * Visualiza indicadores agrupados o globales.

---

## 📊 Indicadores calculados

El sistema genera automáticamente indicadores de las cuatro categorías principales:

| Categoría        | Ejemplos de indicadores                                                                            |
| ---------------- | -------------------------------------------------------------------------------------------------- |
| **Efectividad**  | `hit_ratio`, `precision`, `success_rate`, `progression`, `learning_curve_mean`                     |
| **Eficiencia**   | `avg_reaction_time_ms`, `avg_task_duration_ms`, `time_per_success_s`, `navigation_errors`          |
| **Satisfacción** | `aid_usage`, `interface_errors`, `retries_after_end`, `learning_stability`                         |
| **Presencia**    | `activity_level_per_min`, `first_success_time_s`, `inactivity_time_s`, `sound_localization_time_s` |

Además, se generan **eventos personalizados** detectados automáticamente (“custom_events”),
y pueden incorporarse nuevos roles de evento en el mapeo para expandir el sistema a diferentes tareas experimentales.

---

## 📊 Ejecución rápida

```bash
# 1. Recoger datos desde Unity (ya almacenados en MongoDB)
python python_analysis/vr_analysis.py

# 2. Ver resultados globales / agrupados
streamlit run python_visualization/visual_dashboard.py
```

Genera:

* `results.json` / `results.csv`
* `grouped_metrics.csv`
* `group_results.json`
* `figures_*/`
* `final_report.pdf`

---

## 🔨 Extensibilidad

### Ampliar el mapeo de roles

En `metrics.py`, modifica el diccionario de equivalencias para incluir nuevos tipos de tareas.

```python
self.event_role_map = {
    "object_grabbed": "action_success",
    "object_dropped": "action_fail",
    "door_opened": "task_end",
}
```

### Agregar nuevas métricas

Cada nueva función que opere sobre `self.df` o un `subdf` puede añadirse al diccionario de salida en `compute_grouped_metrics()`.

### Compatibilidad experimental

El sistema funciona con cualquier entorno VR siempre que los eventos sigan el formato MongoDB estándar:

```json
{
  "timestamp": ISODate(),
  "user_id": "U001",
  "event_name": "target_hit",
  "event_role": "action_success",
  "event_context": { "session_id": "...", "group_id": "control" }
}
```

---

## 📊 Resultados generados

| Tipo         | Archivo                     | Descripción                                  |
| ------------ | --------------------------- | -------------------------------------------- |
| Global JSON  | `group_results.json`        | Métricas globales por categoría              |
| Agrupado CSV | `grouped_metrics.csv`       | Una fila por usuario/sesión                  |
| PDF Global   | `final_report.pdf`          | Informe visual global                        |
| PDF Agrupado | `final_report_agrupado.pdf` | Informe detallado individual                 |
| Dashboard    | `visual_dashboard.py`       | Web interactiva (filtros por usuario/sesión) |

---

## 📅 Autoría y licencia

Proyecto **VR USER EVALUATION**
Autor: *[Nombre del autor o grupo de investigación]*
Licencia: Uso académico y experimental.

---

> Este README refleja la versión actualizada del sistema con:
>
> * **Mapeo semántico universal de eventos (event_role)**.
> * **Modos Global y Agrupado** para experimentos multiusuario.
> * **Pipeline de análisis automatizado** con exportación y visualización completa.
