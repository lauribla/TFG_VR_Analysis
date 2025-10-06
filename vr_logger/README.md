# 🎮 VR Logger – Paquete Unity

## 📘 Descripción general

El **VR Logger** es un paquete para Unity que permite registrar automáticamente eventos del usuario en entornos VR y almacenarlos en **MongoDB**.
Forma parte del proyecto completo *VR User Evaluation*, pero este módulo puede utilizarse **de forma independiente** en cualquier aplicación Unity.

Incluye:

* Sistema de logging estructurado en MongoDB.
* Gestión de sesión por usuario (`UserSessionManager`).
* Loggers específicos para colisiones, raycasts y seguimiento (trackers).
* Compatibilidad total con **MongoDB.Driver** para .NET 4.x.

---

## ⚙️ Requisitos

* Unity 2021.3 o superior (modo .NET 4.x Equivalent).
* MongoDB ejecutándose localmente o de forma remota.
* Librerías incluidas en `DLLS_MONGO_Unity.zip`:

  * `MongoDB.Driver.dll`
  * `MongoDB.Driver.Core.dll`
  * `MongoDB.Bson.dll`
  * `DnsClient.dll`
  * `System.Buffers.dll`

Coloca todas las DLL dentro de:

```
Assets/Plugins/
```

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
    │   ├── LogAPI.cs
    │   ├── LogEventModel.cs
    │   ├── LoggerService.cs
    │   ├── MongoLogger.cs
    │   ├── RaycastLogger.cs
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

### 1️⃣ Inicialización del sistema de logs

Agrega el componente **`UserSessionManager`** a un objeto vacío en la escena (por ejemplo, `VRManager`).

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

📘 **Consejo:** ve a *Edit → Project Settings → Script Execution Order* y pon `UserSessionManager` al inicio (valor negativo) para que se inicialice antes que otros scripts.

---

### 2️⃣ Envío manual de logs

Puedes registrar cualquier evento personalizado:

```csharp
using VRLogger;

await LoggerService.LogEvent(
    eventType: "interaction",
    eventName: "button_press",
    eventValue: 1,
    eventContext: new {
        object_name = "StartButton",
        position = transform.position,
        timestamp = System.DateTime.UtcNow.ToString("o")
    }
);
```

🟡 **Buena práctica:** si `LoggerService` no está inicializado, asegúrate de hacerlo manualmente:

```csharp
if (!LoggerService.IsInitialized)
{
    LoggerService.Init("mongodb://localhost:27017", "test", "tfg", "U001");
}
```

---

### 3️⃣ Uso con `UserSessionManager`

El `UserSessionManager` gestiona automáticamente el `session_id` y el `group_id`.
Puedes enviar eventos asociados a la sesión actual:

```csharp
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "collision",
    eventName: "bullet_hit",
    eventValue: 1,
    eventContext: new {
        object_hit = collision.gameObject.name,
        speed = collision.relativeVelocity.magnitude
    }
);
```

Esto añadirá automáticamente:

```json
{
  "session_id": "<GUID>",
  "group_id": "control"
}
```

---

## 🧠 Loggers incluidos

### 🔹 `CollisionLogger`

Detecta colisiones (`OnCollisionEnter` / `OnCollisionExit`) y guarda:

* Nombre de los objetos.
* Velocidad relativa.
* Puntos de contacto.

### 🔹 `RaycastLogger`

Registra impactos de raycast:

* Objeto golpeado.
* Distancia y punto de impacto.
* Origen y dirección del rayo.

### 🔹 `UserSessionLogger`

Crea automáticamente eventos de inicio y fin de sesión al ejecutar la escena o salir de la aplicación.

### 🔹 `Trackers`

Registra la posición o rotación de partes del cuerpo (manos, cabeza, pies...) a intervalos regulares.
Permite correlacionar comportamiento físico con métricas cognitivas.

---

## 🧾 Estructura de los documentos en MongoDB

Cada evento registrado tiene la siguiente estructura:

```json
{
  "timestamp": ISODate("2025-10-06T10:00:00Z"),
  "user_id": "U001",
  "event_type": "collision",
  "event_name": "bullet_hit",
  "event_value": 1,
  "event_context": {
    "object_name": "TargetCube",
    "position": {"x": 1.2, "y": 1.0, "z": -0.3},
    "velocity": 4.5
  },
  "session_id": "0a3d...",
  "group_id": "control"
}
```

---

## 🧩 Integración con Python (Análisis)

Los datos generados se almacenan en MongoDB y se analizan mediante el script `python_analysis/vr_analysis.py` (del proyecto principal).
Desde ahí se generan:

* Estadísticas agregadas (eficiencia, efectividad, satisfacción, presencia).
* Archivos exportados (`.json`, `.csv`).
* Informe PDF.
* Dashboard web con Streamlit.

---

## 🛡️ Buenas prácticas

* Asegura la inicialización antes del primer `LogEvent`.
* Usa `UserSessionManager` para mantener la coherencia entre sesiones.
* Controla los eventos en `Update()` solo si son necesarios (optimización).
* Verifica en consola que se conecta correctamente a MongoDB.
* Si se pierde conexión, puedes hacer un `re-Init` automático.

---

## 🧰 Solución de problemas

**Error:** `Not initialized! Llama primero a LoggerService.Init()`
➡ Añade comprobación con `LoggerService.IsInitialized` antes de enviar.

**Error:** `MongoDB.Driver` no se carga
➡ Asegúrate de que todas las DLL del ZIP están en `Assets/Plugins/`.

**No se insertan datos en MongoDB**
➡ Comprueba que `mongod` está en ejecución y la URL `mongodb://localhost:27017` es accesible.

**Unity no compila (SharpCompress / DiagnosticSource)**
➡ Usa las versiones de DLL proporcionadas. Son compatibles con Unity y no requieren dependencias extra.

---

## 📚 Créditos

**VR Logger – Paquete Unity**
Desarrollado para el proyecto *VR User Evaluation*.
Tecnologías: Unity, MongoDB, C#, .NET 4.x.
Licencia académica – Uso educativo y de investigación.
