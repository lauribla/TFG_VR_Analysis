# 🎮 VR Logger – README ACTUALIZADO (Mapeo Semántico + Modos Global/Agrupado)

## 📘 Descripción general

El **VR Logger** es un paquete modular para Unity que registra eventos de usuario en entornos VR y los envía a **MongoDB** con soporte completo para **roles semánticos de evento (`event_role`)** y compatibilidad con el sistema de análisis Python.

Forma parte del ecosistema **VR User Evaluation**, pero puede usarse **de forma independiente** en cualquier aplicación Unity que requiera trazabilidad de comportamiento.

Incluye:

* Sistema de logging estructurado y tipado (BSON nativo).
* Gestión automática de sesión y usuario (`UserSessionManager`).
* Envío seguro a MongoDB con `LoggerService`.
* Mapeo semántico universal de eventos (`event_role` → `action_success`, `action_fail`, etc.).
* Loggers y trackers para colisiones, raycasts, movimiento y mirada.

---

## ⚙️ Requisitos

* **Unity 2021.3+** (modo .NET 4.x Equivalent).
* **MongoDB** local o remoto.
* Copia las DLL del paquete `DLLS_MONGO_Unity.zip` a:

  ```
  Assets/Plugins/
  ```

  Incluye:

  * `MongoDB.Driver.dll`
  * `MongoDB.Driver.Core.dll`
  * `MongoDB.Bson.dll`
  * `DnsClient.dll`
  * `System.Buffers.dll`

---

## 📂 Estructura del paquete

```
vr_logger/
│
├── package.json
├── README.md (este archivo)
└── Runtime/
    ├── Logs/
    │   ├── CollisionLogger.cs
    │   ├── RaycastLogger.cs
    │   ├── LoggerService.cs
    │   └── UserSessionLogger.cs
    │
    ├── Manager/
    │   ├── UserSessionManager.cs
    │   └── VRTrackingManager.cs
    │
    └── Trackers/
        ├── HandTracker.cs
        ├── GazeTracker.cs
        ├── MovementTracker.cs
        └── FootTracker.cs
```

---

## 🚀 Uso básico

### 1️⃣ Inicialización

Agrega el componente `UserSessionManager` a un objeto vacío (por ejemplo, `VRManager`).

```csharp
using UnityEngine;
using VRLogger;

public class VRManager : MonoBehaviour
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
        LoggerService.Init(connectionString, dbName, collectionName, userId);
        Debug.Log($"[VRLogger] Conectado a {dbName}.{collectionName} como {userId}");
    }
}
```

🔸 Consejo: establece su orden de ejecución como prioridad alta (negativo) en *Project Settings → Script Execution Order*.

---

### 2️⃣ Envío de logs con sesión

El `UserSessionManager` añade automáticamente `session_id` y `group_id` a cada evento.

```csharp
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "interaction",
    eventName: "object_placed",
    eventValue: true,
    eventContext: new { object_name = "Cube_01", position = transform.position }
);
```

Genera en MongoDB:

```json
{
  "timestamp": ISODate("2025-10-06T10:00:00Z"),
  "user_id": "U001",
  "event_type": "interaction",
  "event_name": "object_placed",
  "event_value": true,
  "session_id": "guid-1234",
  "group_id": "control"
}
```

---

### 3️⃣ Envío directo (sin sesión)

```csharp
await LoggerService.LogEvent(
    eventType: "trigger",
    eventName: "button_press",
    eventValue: 1,
    eventContext: new { object_name = "StartButton", hand = "right" }
);
```

Si el servicio no está inicializado, el sistema genera un log defensivo:

```csharp
if (!LoggerService.IsInitialized)
    LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");
```

---

## 🧠 Mapeo semántico (`event_role`)

El Logger permite definir roles genéricos que describen la intención del evento.

Ejemplo:

```csharp
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "task",
    eventName: "target_hit",
    eventValue: 1,
    eventContext: new { event_role = "action_success", target = "Balloon_01" }
);
```

Este `event_role` será utilizado automáticamente por el módulo de análisis en Python (`metrics.py`) para calcular indicadores como `hit_ratio`, `success_rate`, `learning_curve`, etc.

👉 Esto hace que el sistema sea **agnóstico del tipo de experimento** (disparos, parkour, manipulación de objetos, etc.).

---

## 📊 Integración con los modos Global / Agrupado

El paquete Unity genera eventos compatibles con los dos modos de análisis:

| Modo         | Descripción                                                                           | Estructura de datos   |
| ------------ | ------------------------------------------------------------------------------------- | --------------------- |
| **Global**   | Todos los eventos se analizan de forma conjunta (sin distinguir sesión).              | `group_results.json`  |
| **Agrupado** | Cada usuario y sesión tiene métricas separadas (`user_id`, `group_id`, `session_id`). | `grouped_metrics.csv` |

Estos modos se activan desde el pipeline Python (`vr_analysis.py`):

```python
GENERAR_GLOBAL = True
GENERAR_AGRUPADO = True
```

Ambos análisis utilizan los eventos generados por este SDK sin requerir cambios adicionales.

---

## 🧩 Loggers incluidos

* **CollisionLogger** – registra colisiones con información física.
* **RaycastLogger** – rayos, impactos y distancias.
* **UserSessionLogger** – inicio/fin de sesión automática.
* **Trackers** – posición/rotación de manos, pies, cabeza y cuerpo.

---

## 🛠️ Estructura final de eventos (MongoDB)

```json
{
  "timestamp": ISODate(),
  "user_id": "U001",
  "event_name": "target_hit",
  "event_type": "interaction",
  "event_value": 1,
  "event_role": "action_success",
  "session_id": "a9f7-...",
  "group_id": "experimental",
  "event_context": {
    "object_name": "TargetCube",
    "velocity": 4.5
  }
}
```

---

## 🧰 Buenas prácticas

* Inicializa siempre el `LoggerService` antes de cualquier log.
* Usa `UserSessionManager` para mantener coherencia entre sesiones.
* Define `event_role` si el evento representa éxito, fallo o interacción.
* Reintenta `Init()` automáticamente si la conexión a MongoDB falla.
* Evita llamadas redundantes a `LogEvent` en `Update()` (usa triggers o callbacks).

---

## 📈 Compatibilidad con Python

Los logs generados por Unity son consumidos directamente por:

* `log_parser.py` – lectura y parseo desde MongoDB.
* `metrics.py` – cálculo de métricas (mapeo de roles incluido).
* `vr_analysis.py` – pipeline de análisis global y agrupado.
* `visual_dashboard.py` – visualización y filtrado.

No se requieren adaptaciones adicionales.

---

## 📚 Créditos

**VR Logger – Unity SDK**
Componente del proyecto **VR User Evaluation**.
Compatible con **MongoDB** y **Python Analysis Toolkit**.
Licencia académica – Uso educativo y de investigación.
