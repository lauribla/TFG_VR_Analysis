# 📦 VR Logger – Paquete Unity

Este paquete proporciona un sistema de **logging para experimentos en Realidad Virtual**.
Su objetivo es capturar eventos del usuario (trayectorias, mirada, manos, pies, sesiones, etc.) y almacenarlos en **MongoDB** para su posterior análisis.

---

## 🚀 Instalación

### 1. Importar desde GitHub

En el `manifest.json` de tu proyecto Unity, añade:

```json
"dependencies": {
  "com.github.lauribla.vr_logger": "https://github.com/lauribla/TFG_VR_Analysis.git?path=/vr_logger#main"
}
```

Unity descargará el paquete automáticamente junto con sus dependencias declaradas en `package.json`:

* `XR Management`
* `OpenXR Plugin`
* `Input System`

### 2. Importar manualmente

1. Descarga el repositorio como `.zip`.
2. Copia la carpeta `vr_logger/` en `Packages/` de tu proyecto Unity.

### DLLs de Mongo Db

MongoDB no es un paquete oficial de Unity, por lo que se debe incluir el driver oficial de MongoDB para C# en el paquete:

 * Descarga desde MongoDB .NET Driver.

 * Copia al directorio vr_logger/Runtime/Plugins/ los siguientes DLLs:

     * MongoDB.Driver.dll

     * MongoDB.Bson.dll

     * MongoDB.Driver.Core.dll

Unity los compilará junto a tus scripts y permitirán conectar directamente con MongoDB desde C#.

---

## 📂 Estructura del paquete

```
vr_logger/
├── package.json         # Metadatos y dependencias
└── Runtime/             # Código fuente
    ├── Logs/            # Logging en MongoDB
    ├── Manager/         # Gestión de sesiones y tracking
    ├── Trackers/        # Gaze, movimiento, manos, pies
    └── src_bd_unity/    # Tests de conexión Mongo
```

---

## ⚙️ Uso básico

1. Arrastra los componentes de `Runtime/Manager` a un GameObject vacío en tu escena VR.

   * `VRTrackingManager.cs`: activa/desactiva los distintos trackers.
   * `UserSessionManager.cs`: gestiona sesiones de usuario en MongoDB.

2. Configura en el inspector:

   * `Mongo URI` → dirección de tu base de datos MongoDB (ej: `mongodb://localhost:27017`).
   * `DB Name` y `Collection Name` → base y colección donde se guardarán los logs.

3. Durante la ejecución en VR, el sistema enviará automáticamente eventos de:

   * Mirada (gaze)
   * Movimiento
   * Manos
   * Pies
   * Eventos personalizados (si los añades en tus scripts)

---

## 🛠️ Extensión

* Para añadir un nuevo tracker, crea un script en `Runtime/Trackers/` que herede de `MonoBehaviour` y use el `LoggerService` para enviar eventos a MongoDB.
* Los eventos personalizados se definen con `event_type = "custom"` y aparecerán automáticamente en el análisis posterior.

---

## 📄 Notas

* Este paquete es **independiente** del análisis en Python, pero complementa el sistema general del TFG.
* Los datos guardados en MongoDB podrán ser procesados con las herramientas de `python_analysis` y `python_visualization` del repositorio principal.

---

## ✨ Autor

* **Laura Hernández** – [laura.hhernandez@alumnos.upm.es](mailto:laura.hhernandez@alumnos.upm.es)
  Universidad Politécnica de Madrid
