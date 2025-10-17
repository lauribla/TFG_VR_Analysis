# 👨‍💻 VR LOGGER – MANUAL DEL DESARROLLADOR (Actualizado: Mapeo Semántico + Modos Global/Agrupado)

## 📘 Introducción

Este documento sirve como **manual técnico para desarrolladores** que integren el sistema de logging VR en sus proyectos Unity.

Explica **cómo importar, usar y extender los logs**, además de cómo preparar los datos para el análisis de indicadores automáticos (eficiencia, efectividad, satisfacción y presencia), incluyendo el **nuevo sistema de mapeo semántico (`event_role`)** y los **modos de análisis global y agrupado** implementados en el pipeline Python.

---

## ⚙️ 1️⃣ Instalación e Importación

### Paso 1. Instalar dependencias

Descomprime `DLLS_MONGO_Unity.zip` (incluido en el repositorio principal) y copia los siguientes archivos dentro de:

```
Assets/Plugins/
```

**DLLs requeridas:**

* MongoDB.Driver.dll
* MongoDB.Driver.Core.dll
* MongoDB.Bson.dll
* DnsClient.dll
* System.Buffers.dll

Unity los detectará automáticamente como plugins de .NET 4.x.

### Paso 2. Importar el paquete `vr_logger`

Copia la carpeta completa `vr_logger/` (con su subcarpeta `Runtime/`) dentro de tu proyecto Unity.

Verás en el explorador algo así:

```
Assets/
 ├─ Plugins/
 ├─ vr_logger/
 │   ├─ Runtime/
 │   │   ├─ Logs/
 │   │   ├─ Manager/
 │   │   └─ Trackers/
```

---

## 🧠 2️⃣ Arquitectura general

El sistema se divide en tres módulos:

### 🔹 1. **LoggerService.cs**

Núcleo de comunicación con MongoDB. Permite enviar eventos mediante `LogEvent()` y gestiona la conexión, serialización y almacenamiento BSON.

### 🔹 2. **UserSessionManager.cs**

Controla el `user_id`, `group_id` y `session_id` activo. Proporciona métodos helper para enviar eventos automáticamente asociados a la sesión actual (`LogEventWithSession`).

### 🔹 3. **Loggers especializados**

* `CollisionLogger.cs` – registra colisiones físicas.
* `RaycastLogger.cs` – registra impactos de raycasts (mirada o puntero).
* `UserSessionLogger.cs` – marca inicio y fin de sesión.
* `HandTracker`, `GazeTracker`, `FootTracker`, `MovementTracker` – capturan movimiento físico (presencia e interacción).

---

## 🚀 3️⃣ Inicialización del sistema

### A. Inicialización automática (recomendada)

Agrega el componente `UserSessionManager` a un objeto de la escena (por ejemplo `VRManager`).

```csharp
using UnityEngine;
using VRLogger;

public class VRManager : MonoBehaviour
{
    void Awake()
    {
        LoggerService.Init(
            connectionString: "mongodb://localhost:27017",
            dbName: "test",
            collectionName: "tfg",
            userId: "U001"
        );
        Debug.Log("[VRLogger] Conectado a MongoDB.");
    }
}
```

### B. Inicialización manual

Si necesitas enviar logs sin `UserSessionManager`:

```csharp
if (!LoggerService.IsInitialized)
{
    LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");
}
```

Esto garantiza que el sistema esté operativo antes de registrar eventos.

---

## ✍️ 4️⃣ Creación y envío de logs

### A. Estructura del log

```json
{
  "timestamp": "2025-10-06T10:25:00Z",
  "user_id": "U001",
  "event_type": "collision",
  "event_name": "bullet_hit",
  "event_value": 1,
  "event_role": "action_success",
  "event_context": { "object_name": "Target_01", "speed": 3.2 },
  "session_id": "GUID",
  "group_id": "control"
}
```

### B. Ejemplo simple

```csharp
await LoggerService.LogEvent(
    eventType: "interaction",
    eventName: "button_press",
    eventValue: 1,
    eventContext: new { object_name = "PlayButton", pressed = true }
);
```

### C. Ejemplo con sesión y mapeo semántico

```csharp
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "task",
    eventName: "target_hit",
    eventValue: 1,
    eventContext: new { event_role = "action_success", target = "Balloon_01" }
);
```

El campo `event_role` permite que el pipeline Python clasifique el evento automáticamente para calcular métricas.

---

## 🧩 5️⃣ Sistema de mapeo semántico (`event_role`)

### ¿Qué es?

Un mecanismo que permite describir la **intención del evento** sin depender del nombre (`event_name`).

Ejemplos de roles comunes:

| Rol (`event_role`) | Ejemplo de eventos                            |
| ------------------ | --------------------------------------------- |
| `action_success`   | `target_hit`, `goal_reached`, `object_placed` |
| `action_fail`      | `target_miss`, `fall_detected`                |
| `task_start`       | `task_start`, `mission_begin`                 |
| `task_end`         | `task_end`, `mission_complete`                |
| `interaction_help` | `help_requested`, `guide_used`, `hint_used`   |

Estos roles son interpretados por `metrics.py` para calcular indicadores como efectividad, eficiencia o presencia.

🔸 Si no se define `event_role`, el sistema intentará inferirlo automáticamente según el `event_name`.

---

## 📈 6️⃣ Compatibilidad con los modos Global y Agrupado

El sistema de logging Unity produce datos compatibles con los dos modos de análisis del pipeline Python:

| Modo         | Descripción                                                                  | Archivo generado      |
| ------------ | ---------------------------------------------------------------------------- | --------------------- |
| **Global**   | Analiza todos los eventos juntos (comparación de grupos).                    | `group_results.json`  |
| **Agrupado** | Calcula métricas por usuario y sesión (`user_id`, `group_id`, `session_id`). | `grouped_metrics.csv` |

Ambos modos se activan desde `python_analysis/vr_analysis.py`:

```python
GENERAR_GLOBAL = True
GENERAR_AGRUPADO = True
```

Los logs enviados desde Unity no requieren cambios adicionales; Python reconocerá automáticamente los campos `user_id`, `group_id` y `session_id`.

---

## 🧮 7️⃣ Indicadores analizados automáticamente

El módulo `metrics.py` analiza tus logs y genera indicadores de:

| Categoría        | Ejemplo de métrica                  | Derivado de                              |
| ---------------- | ----------------------------------- | ---------------------------------------- |
| **Efectividad**  | `hit_ratio`, `success_rate`         | Eventos `action_success` / `action_fail` |
| **Eficiencia**   | `avg_reaction_time_ms`              | Diferencia temporal entre eventos        |
| **Satisfacción** | `aid_usage`, `error_reduction_rate` | Eventos de ayuda o error                 |
| **Presencia**    | `activity_level_per_min`            | Eventos de movimiento y mirada           |

---

## 🧪 8️⃣ Prueba y validación

* Verifica en consola: `[LoggerService] ✅ Conectado a MongoDB`
* Comprueba en MongoDB Compass que se insertan eventos con `session_id` y `group_id`.
* Ejecuta en Python:

  ```bash
  python python_analysis/vr_analysis.py
  ```

  para generar métricas y PDF.

---

## 🛡️ 9️⃣ Buenas prácticas

* Inicializa el `LoggerService` en `Awake()`.
* Usa `UserSessionManager` para coherencia entre sesiones.
* Incluye `event_role` siempre que sea posible.
* Evita enviar eventos en cada frame; usa triggers.
* Si la conexión falla, reintenta `LoggerService.Init()`.
* Mantén consistencia en nombres de `event_name` y `event_role`.

---

## 🧩 🔟 Extensión del sistema

Puedes crear loggers personalizados que hereden de los existentes o creen nuevos tipos de eventos:

```csharp
using UnityEngine;
using VRLogger;

public class CustomLogger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var ctx = new { key = "Space", event_role = "interaction_generic" };
            LoggerService.LogEvent("input", "key_press", 1, ctx);
        }
    }
}
```

El nuevo evento será detectado automáticamente en MongoDB y analizado por Python si se añade su `event_role` correspondiente en el mapeo.

---

## 📚 Créditos

**VR LOGGER – Módulo Unity para MongoDB Logging**
Parte del ecosistema **VR User Evaluation**.
Incluye soporte para **mapeo semántico (`event_role`)** y **modos de análisis global/agrupado**.
Licencia académica – Uso educativo e investigativo.
