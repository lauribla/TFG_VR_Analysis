# 👨‍💻 VR LOGGER – MANUAL DEL DESARROLLADOR

## 📘 Introducción

Este documento sirve como **manual técnico para desarrolladores** que integren el sistema de logging VR en sus proyectos Unity.
Explica **cómo importar, usar y extender los logs**, además de cómo preparar los datos para el análisis de indicadores automáticos (eficiencia, efectividad, satisfacción y presencia).

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

Es el núcleo de comunicación con MongoDB. Permite enviar eventos mediante `LogEvent()` y gestiona la conexión.

### 🔹 2. **UserSessionManager.cs**

Controla la sesión del usuario y el `session_id`. También sirve de helper para agregar `group_id` o `user_id` automáticamente a cada evento.

### 🔹 3. **Loggers especializados**

* `CollisionLogger.cs`: registra colisiones físicas.
* `RaycastLogger.cs`: registra impactos de raycasts (mirada o puntero).
* `UserSessionLogger.cs`: marca inicio y fin de sesión.
* `HandTracker`, `GazeTracker`, `FootTracker`, `MovementTracker`: capturan movimiento físico para correlación de presencia.

---

## 🚀 3️⃣ Inicialización del sistema

### Opción A – Inicialización automática (recomendada)

Añade el componente `UserSessionManager` a un objeto de la escena (por ejemplo `VRManager`).

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

### Opción B – Inicialización manual

Si necesitas enviar logs desde scripts sin un `UserSessionManager`:

```csharp
if (!LoggerService.IsInitialized)
{
    LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");
}
```

Esto asegura que el sistema siempre esté listo antes de registrar eventos.

---

## ✍️ 4️⃣ Creación y envío de logs

### A. Estructura de un log

Cada evento sigue este modelo:

```json
{
  "timestamp": "2025-10-06T10:25:00Z",
  "user_id": "U001",
  "event_type": "collision",
  "event_name": "bullet_hit",
  "event_value": 1,
  "event_context": {
    "object_name": "Target_01",
    "speed": 3.2
  },
  "session_id": "GUID",
  "group_id": "control"
}
```

### B. Ejemplo simple de envío

```csharp
await LoggerService.LogEvent(
    eventType: "interaction",
    eventName: "button_press",
    eventValue: 1,
    eventContext: new {
        object_name = "PlayButton",
        pressed = true,
        time = Time.time
    }
);
```

### C. Ejemplo con sesión

Usando `UserSessionManager.Instance` para añadir metadatos automáticamente:

```csharp
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "collision",
    eventName: "bullet_hit",
    eventValue: 1,
    eventContext: new {
        object_hit = collision.gameObject.name,
        velocity = collision.relativeVelocity.magnitude
    }
);
```

### D. Ejemplo de logger automático (colisiones)

```csharp
void OnCollisionEnter(Collision collision)
{
    if (!LoggerService.IsInitialized)
        LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");

    var context = new {
        this_object = gameObject.name,
        other_object = collision.gameObject.name,
        velocity = collision.relativeVelocity.magnitude
    };

    await LoggerService.LogEvent("collision", "collision_enter", 1, context);
}
```

---

## 📈 5️⃣ Indicadores medibles (análisis automático)

Los eventos registrados son analizados por el módulo Python (`python_analysis/vr_analysis.py`), que calcula automáticamente indicadores clave:

| Categoría        | Indicador                      | Fuente de datos (log)                         |
| ---------------- | ------------------------------ | --------------------------------------------- |
| **Efectividad**  | Porcentaje de aciertos         | `event_name`: `target_hit` vs `target_miss`   |
| **Eficiencia**   | Tiempo medio de reacción       | Diferencia entre spawn y hit                  |
| **Satisfacción** | Adaptación y confianza         | Frecuencia y tipo de errores (ej. retries)    |
| **Presencia**    | Nivel de actividad / inmersión | Eventos de movimiento (`Trackers`) y latencia |

Para garantizar que tus logs alimentan correctamente el análisis, usa **nombres estándar de eventos**:

```text
- target_hit / target_miss
- task_start / task_end
- collision_enter / collision_exit
- teleport / movement
- gaze_focus / gaze_lost
```

---

## 🧪 6️⃣ Prueba y validación

### A. Probar conexión con MongoDB

En consola, verifica:

```
[LoggerService] Connected to MongoDB: test/tfg
```

Y revisa en **MongoDB Compass** que aparecen los documentos en la colección `tfg`.

### B. Forzar prueba manual

```csharp
void Start()
{
    if (!LoggerService.IsInitialized)
        LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");

    LoggerService.LogEvent("test", "manual_log", 1, new { msg = "Test log OK" });
}
```

---

## 📊 7️⃣ Análisis de datos (Python)

1. Ejecuta el análisis completo:

   ```bash
   python python_analysis/vr_analysis.py
   ```

   Generará los resultados en `python_analysis/pruebas/exports_*/`.

2. Para visualizar los indicadores:

   ```bash
   streamlit run python_visualization/visual_dashboard.py
   ```

3. El dashboard mostrará métricas por usuario y grupo en tiempo real.

---

## 🛡️ 8️⃣ Buenas prácticas de desarrollo

✅ Inicializa siempre el logger en `Awake()` o `Start()`.
✅ Usa logs estructurados con objetos anónimos para el contexto.
✅ No hagas `await` dentro de `Update()`; usa corrutinas o colas de envío.
✅ Evita enviar logs excesivos por frame (controla frecuencia).
✅ Usa nombres coherentes para eventos según las categorías oficiales.
✅ Comprueba en consola que se conecta correctamente antes de analizar.

---

## 🧩 9️⃣ Extender el sistema

Puedes crear nuevos tipos de loggers personalizados:

```csharp
using UnityEngine;
using VRLogger;

public class CustomEventLogger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var context = new { key = "Space", state = "pressed" };
            LoggerService.LogEvent("input", "key_press", 1, context);
        }
    }
}
```

Añade tus propios `event_type` y `event_name` según tus necesidades.

---

## 🧾 10️⃣ Resumen

| Módulo             | Función principal                | Uso                     |
| ------------------ | -------------------------------- | ----------------------- |
| LoggerService      | Conexión MongoDB + envío de logs | `LogEvent()`            |
| UserSessionManager | Control de sesión + helpers      | `LogEventWithSession()` |
| CollisionLogger    | Captura colisiones               | Automático              |
| RaycastLogger      | Captura raycasts (mirada)        | Automático              |
| Trackers           | Captura movimiento físico        | Configurable            |

---

## 📚 Créditos

**VR LOGGER – Módulo Unity para MongoDB Logging**
Desarrollado dentro del proyecto *VR User Evaluation*.
Tecnologías: Unity, MongoDB, C#, .NET 4.x.
Autoría: Lauribla / Proyecto académico ETSIINF.
