# 🧠 VR USER EVALUATION – README COMPLETO (MongoDB + Unity + Python)

## 📘 Descripción general

Sistema completo para **monitorizar el comportamiento del usuario en entornos VR**, almacenar los eventos en **MongoDB**, y **analizarlos/visualizarlos** con Python (métricas + PDF + dashboard web).

Incluye:

* SDK de **logging para Unity** (eventos, sesión, trackers).
* **Almacenamiento en MongoDB** (local o remoto).
* **Pipeline de análisis** (métricas: efectividad, eficiencia, satisfacción, presencia).
* **Informe PDF** y **dashboard web interactivo** (Streamlit/Plotly).

---

## 📂 Estructura del repositorio

```
TFG_VR_Analysis/
│
├─ pruebas/                             # Scripts de prueba / orquestación
│  ├─ test_pipeline.py
│  └─ test_pipeline_db.py
│
├─ python_analysis/                     # Núcleo de análisis de datos
│  ├─ __init__.py
│  ├─ exporter.py
│  ├─ log_parser.py
│  ├─ metrics.py
│  └─ vr_analysis.py                    # 🔹 Script principal del pipeline
│  
├─ python_visualization/                # Visualización / informes
│  ├─ __init__.py
│  ├─ pdf_reporter.py                   # Generador de informe PDF
│  ├─ visual_dashboard.py               # 🔹 Dashboard web (Streamlit)
│  └─ visualize_groups.py               # Utilidades de gráficos
│
├─ vr_logger/                           # Paquete Unity (runtime)
│  ├─ README.md
│  ├─ package.json
│  └─ Runtime/
│     ├─ Logs/
│     │  ├─ CollisionLogger.cs
│     │  ├─ LogAPI.cs
│     │  ├─ LogEventModel.cs
│     │  ├─ LoggerService.cs           # Comunicación con MongoDB
│     │  ├─ MongoLogger.cs
│     │  ├─ RaycastLogger.cs
│     │  └─ UserSessionLogger.cs
│     ├─ Manager/
│     │  ├─ UserSessionManager.cs      # Gestión de usuario/sesión y helper de logs
│     │  └─ VRTrackingManager.cs
│     └─ Trackers/
│        ├─ FootTracker.cs
│        ├─ GazeTracker.cs
│        ├─ HandTracker.cs
│        └─ MovementTracker.cs
│
├─ DLLS_MONGO_Unity.zip                 # 🔹 Librerías .dll necesarias para Unity
├─ requirements.txt                     # 🔹 Dependencias Python
├─ VR_Analysis.sln
└─ README.md (este archivo)
```

> 📦 **Exportaciones y figuras** se crean automáticamente en:
>
> `python_analysis/pruebas/exports_YYYYMMDD_HHMMSS/`
>
> `python_analysis/pruebas/figures_YYYYMMDD_HHMMSS/`

---

## ⚙️ Requisitos

### Unity

* Unity 2021.3+ (scripting runtime .NET 4.x).
* Copia las DLL a `Assets/Plugins/` (vienen en `DLLS_MONGO_Unity.zip`):

  * `MongoDB.Driver.dll`
  * `MongoDB.Driver.Core.dll`
  * `MongoDB.Bson.dll`
  * `DnsClient.dll`
  * `System.Buffers.dll`

### MongoDB

* Instancia local (por defecto): `mongodb://localhost:27017`
* BD y colección por defecto (configurables):

  * **DB:** `test`
  * **Collection:** `tfg`

### Python

* Python 3.10+.
* Instala dependencias:

```bash
pip install -r requirements.txt
```

Dependencias principales: `pymongo`, `pandas`, `numpy`, `matplotlib`, `plotly`, `reportlab`, `streamlit`.

---

## 🚀 Guía rápida (de 0 a resultados)

1. **Ejecuta tu escena Unity** → los eventos se guardan en MongoDB (`test.tfg`).
2. **Analiza** con:

```bash
python python_analysis/vr_analysis.py
```

3. **Mira el PDF** en `python_analysis/pruebas/exports_*/final_report.pdf`.
4. **Abre el dashboard web**:

```bash
streamlit run python_visualization/visual_dashboard.py
```

---

## 🎯 Uso en Unity

### 1) Añade `UserSessionManager` a la escena

Asegura la inicialización de MongoDB al arrancar la escena.

```csharp
using UnityEngine;
using VRLogger;

public class Bootstrap : MonoBehaviour
{
    [Header("Mongo Config")]
    public string connectionString = "mongodb://localhost:27017";
    public string dbName = "test";
    public string collectionName = "tfg";

    [Header("User Config")]
    public string userId = "U001";
    public string groupId = "control";

    void Awake()
    {
        // Inicializa el Logger al inicio de la escena
        LoggerService.Init(connectionString, dbName, collectionName, userId);
        Debug.Log($"[UserSessionManager] Conectado a {dbName}.{collectionName} como {userId}");
    }
}
```

> ✅ **Orden de ejecución recomendado:** coloca este script arriba en *Project Settings → Script Execution Order* para garantizar que se inicializa antes de enviar eventos.

### 2) Enviar eventos (API del logger)

**Opción A – Con contexto de sesión (recomendado)**

```csharp
// Requiere que exista UserSessionManager con el helper LogEventWithSession
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "collision",
    eventName: "bullet_hit",
    eventValue: 1,
    eventContext: new { object_name = target.name, speed = 3.2f }
);
```

**Opción B – Envío directo** (funciona aunque no haya `UserSessionManager`)

```csharp
// 🔒 Buen práctica: inicialización de respaldo
if (!LoggerService.IsInitialized)
{
    LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");
}

await LoggerService.LogEvent(
    eventType: "spawn",
    eventName: "spawn_object",
    eventValue: 1,
    eventContext: new { object_name = obj.name, spawn_time = Time.time }
);
```

> ℹ️ En repos anteriores se usaba `SendLog(...)`. **Sustitúyelo por** `LoggerService.LogEvent(...)`.

### 3) Ejemplos listos (incluidos en `vr_logger/Runtime/Logs/`)

* `CollisionLogger.cs` → envía `collision_enter/exit` + contexto de colisión.
* `RaycastLogger.cs` → envía impactos de raycast (`raycast_hit`).
* `UserSessionLogger.cs` → resumen de inicio/fin de sesión.

### 4) Trackers (opcionales)

En `vr_logger/Runtime/Trackers/` hay capturas de `Gaze`, `Hands`, `Movement`, `Foot`. Puedes activarlos y adaptar la frecuencia de muestreo en tus escenas.

---

## 🧮 Pipeline de Análisis (Python)

### 1) `vr_analysis.py` – Orquestador

Ejecuta todo el flujo:

* carga desde MongoDB (`test.tfg`),
* calcula métricas (efectividad, eficiencia, satisfacción, presencia),
* exporta CSV/JSON,
* genera figuras,
* crea el informe PDF.

```bash
python python_analysis/vr_analysis.py
```

**Exporta en:**

```
python_analysis/pruebas/
  ├─ exports_YYYYMMDD_HHMMSS/
  │  ├─ results.json
  │  ├─ results.csv
  │  ├─ group_results.json
  │  └─ final_report.pdf
  └─ figures_YYYYMMDD_HHMMSS/
```

### 2) Dashboard web (Streamlit)

```bash
streamlit run python_visualization/visual_dashboard.py
```

El panel detecta por defecto el último `group_results.json` dentro de `python_analysis/pruebas/exports_*/` y muestra:

* Indicadores oficiales (hit ratio, success rate, reaction time, activity level...).
* Conteo de eventos personalizados.
* Tabla de métricas completa (por usuario/grupo).

### 3) Scripts de prueba

En `pruebas/` hay dos pipelines de ejemplo:

* `test_pipeline.py` → prueba local con ficheros.
* `test_pipeline_db.py` → prueba conectando a la base de datos.

---

## 🧩 Campos del documento (MongoDB)

Cada evento guardado incluye:

```json
{
  "timestamp": ISODate("2025-10-06T10:02:45Z"),
  "user_id": "U001",
  "event_type": "collision",
  "event_name": "bullet_hit",
  "event_value": 1,
  "event_context": {
    "session_id": "...",
    "group_id": "control",
    "context": { "object_name": "Target_01", "speed": 3.2 }
  }
}
```

> `UserSessionManager.LogEventWithSession(...)` añade automáticamente `session_id` y `group_id` al contexto.

---

## 🛡️ Buenas prácticas

* **Inicialización defensiva:** antes de cualquier `LogEvent`, comprueba `LoggerService.IsInitialized` y llama a `Init(...)` si es necesario.
* **Orden de ejecución:** inicializa el logger cuanto antes en la escena.
* **Variables por escena:** puedes configurar `userId`, `groupId`, `dbName`, `collectionName` desde el inspector del `UserSessionManager`.
* **Entornos separados:** usa `test.tfg` para desarrollo y otra BD/colección para datos reales.

---

## 🧰 Solución de problemas

**Unity – “Not initialized! Llama primero a LoggerService.Init()”**
→ Añade init defensivo donde envíes logs (ver ejemplos arriba).

**Unity – Errores de DLL (SharpCompress, DiagnosticSource, etc.)**
→ Asegúrate de copiar **todas** las DLL del ZIP a `Assets/Plugins/` (ver lista en requisitos). Vuelve a compilar.

**Python – Error `tz-naive vs tz-aware`**
→ Ya gestionado en `metrics.py` usando `pd.to_datetime(..., utc=True, errors='coerce')`.

**Streamlit – “File does not exist: …”**
→ Ejecuta desde la raíz del repo o pasa la ruta completa:
`streamlit run python_visualization/visual_dashboard.py`

---

## 🧾 Licencia y créditos

* Proyecto académico **VR USER EVALUATION**.
* Tecnologías: Unity, MongoDB, Python (pandas, plotly, reportlab, streamlit).
* Autoría: ver historial de commits.

¡Listo! Si necesitas un **README rápido para el subpaquete `vr_logger`** o una **plantilla de escenas de ejemplo**, dímelo y lo añadimos al repo.
