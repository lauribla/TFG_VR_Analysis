# 🎮 VR Logger – Unity Package (v2.1)

## 📘 Descripción general

El **VR Logger** es un paquete para Unity que facilita la captura y análisis de comportamiento en VR.

Novedades v2.1:
* **ExperimentConfig**: Configuración centralizada sin código (Inspector).
* **ExperimentProfile**: Perfiles reutilizables (ScriptableObjects).
* **Streamlit Configurator**: Crea configuraciones de experimentos en una UI web y descárgalas en Unity con 1 clic ("Pull Config from Streamlit"). Incluye **Gestión de Participantes** y **Cuestionarios SUS** integrados directamente con el Dashboard de visualización y el Informe en PDF.
* **Dynamic Play Area**: El tamaño del área de juego para los mapas de seguimiento espaciales se extrae **automáticamente** en tiempo de ejecución de las gafas de RV.
* **Inspector Event Mapping**: Define tus eventos (ej: `bullet_hit` -> `action_success`) visualmente.
* **Plugins Modulares**: Activa/desactiva Gaze, Movement, Hand, etc.

---

## ⚙️ Requisitos

* **Unity 2021.3+**
* **MongoDB** (local o remoto)
* **Plugins incluidos**: `MongoDB.Driver`, `MongoDB.Bson`, etc. (en `Assets/Plugins/`)

---

## 🚀 Guía de### 3. Eye Tracking (Opcional)
Para usar las funcionalidades de **seguimiento ocular** (EyeTracker o GazeTracker), es **OBLIGATORIO**:
1.  Importar el SDK de **VIVESR** en el proyecto (`Assets/VIVESR`).
2.  Asegurar que la escena tiene el prefab **`SRanipal Eye Framework`** configurado y activo.

## 🚀 Uso Básico

### 1️⃣ Configuración de la Escena (OBLIGATORIO)

Para que el sistema funcione, debes tener estos 3 componentes en tu escena (pueden estar en un mismo GameObject vacío llamado `VRManager`):

1.  **`UserSessionManager`**: Gestiona la conexión a la base de datos y el ciclo de vida de la sesión.
2.  **`ExperimentConfig`**: Carga el perfil del experimento (`ExperimentProfile`) y la configuración JSON.
3.  **`VRTrackingManager`**: Gestiona las referencias a los objetos físicos.
    *   Debes asignarle manualmente:
        *   `Vr Camera` -> Tu Main Camera.
        *   `Player Transform` -> Tu XR Origin / XR Rig.
        *   `Left/Right Hand` -> Los objetos de tus manos.
    *   Si no lo configuras, el sistema intentará buscarlos automáticamente. Si no usas VR, no pasa nada, no dará error. 

> **Nota:** Si usas múltiples escenas, asegúrate de que el `VRTrackingManager` esté presente y configurado en CADA escena, ya que las referencias (cámara, manos) cambian al cambiar de escena. `UserSessionManager` sobrevive entre escenas.

### 2️⃣ Conexión a Base de Datos (UserSessionManager)

Antes de nada, define tu destino de log en el inspector de **UserSessionManager**:
* **Connection String**: `mongodb://localhost:27017` (Defecto para local) o tu URI de Mongo Atlas.
* **Database Name**: El nombre de tu base de datos (se creará si no existe), ej: `vr_experiment_db`.
* **Collection Name**: Nombre de la colección, ej: `logs`.

### 3️⃣ Crear un Perfil (`ExperimentProfile`)

En lugar de configurar todo en la escena cada vez, crea un activo de perfil:

1. En la ventana **Project**, haz clic derecho > **Create** > **VRLogger** > **Experiment Profile**.
2. Ponle nombre (ej: `Experimento_Disparos`).
3. Selecciónalo y configura en el inspector:
   * **Session Name**, **Group Name**, **Independent Variable**.
   * **Participantes**: Lista de IDs o activa **Manual Participant Name** para pruebas rápidas.
   * **Métricas**: Define pesos, minimos/máximos para cada métrica.
   * **Event Mapping**: Mapea tus eventos a roles estándar.

### 4️⃣ Asignar Perfil en Escena

1. Selecciona tu objeto `VRManager`.
2. En el componente **`ExperimentConfig`**, arrastra tu perfil al campo **Active Profile**.
3. (Opcional) Haz clic derecho en el componente y selecciona **"Load From Profile"** para ver los valores en el inspector, o **"Save To Profile"** si haces cambios locales que quieres guardar.

### 5️⃣ Configurar Event Mapping (¡Importante!)

Para que Python entienda tus eventos, defínelos en el perfil:

* **Event Name**: `platano_hit` (El string que envías desde el código).
* **Role**: Selecciona del Dropdown (ej: `Action_Success`).

Esto asegura que `platano_hit` cuente como un acierto en las gráficas de Eficacia.

---

## 💻 Envío de Eventos desde Código

Usa `LoggerService` o `UserSessionManager` para enviar eventos. El sistema usará la configuración cargada al inicio.

```csharp
// Envío con contexto automático de sesión
await UserSessionManager.Instance.LogEventWithSession(
    eventType: "interaction",
    eventName: "platano_hit", // Debe coincidir con tu Event Mapping
    eventValue: 1.0f,
    eventContext: new { distance = 5.5f, weapon = "hand" }
);
```

---

## 🧩 Plugins disponibles

Actívalos desde el **ExperimentProfile** (sección *Modules*):

| Plugin | Descripción | Log generado |
| :--- | :--- | :--- |
| **GazeTracker** | Registra qué objeto mira el usuario (Raycast desde cámara). | `gaze_sustained`, `gaze_frequency_change` |
| **MovementTracker** | Registra posición/rotación de HMD cada X segundos. | `movement_update` |
| **HandTracker** | Registra posición de manos (Controllers). | `hand_movement` |
| **CollisionLogger** | Detecta colisiones físicas con tags específicos. | `collision` (`Navigation_Error`) |
| **RaycastLogger** | Lanza rayos desde controladores para ver interacciones. | `ui_interaction` |

---

## 🛠️ Estructura final de eventos (MongoDB)

```json
{
  "timestamp": ISODate(),
  "user_id": "U001",
  "event_name": "platano_hit",
  "event_type": "interaction",
  "event_role": "action_success", // Inyectado automáticamente por ExperimentConfig
  "session_id": "guid-1234",
  "group_id": "Grupo_A",
  "event_context": { ... }
}
```

---

## 📚 Créditos

**VR Logger – Unity SDK v2.1**
Parte del proyecto **VR User Evaluation**.
