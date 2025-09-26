# 📂 TFG_VR_Analysis – Project Structure

Este repositorio implementa un sistema de **monitorización de usuarios en entornos virtuales (VR)**, compuesto por dos partes principales:

- **vr-logger (Unity / C#)** → Scripts para recoger datos en tiempo real desde Unity (logs de usuario, trackers de mirada, movimiento, etc.) y almacenarlos en MongoDB.  
- **python-analysis (Python)** → Scripts para procesar, parsear y analizar los logs guardados en MongoDB, extrayendo estadísticas y métricas definidas en la tabla de indicadores.

---

## 📂 Estructura del repositorio

TFG_VR_Analysis/
├── .git/ # Configuración de Git
├── .idea/ # Configuración del proyecto JetBrains Rider
├── python-analysis/ # Scripts en Python para análisis de logs
│ └── test_mongo.py # Test de conexión y lectura desde MongoDB
│
├── vr-logger/ # Parte Unity/C# para logging y tracking
│ ├── Runtime/ # Scripts de ejecución en Unity (runtime)
│ │ ├── Logs/ # Módulo de logging
│ │ │ ├── LogAPI.cs # API con funciones para cada tipo de evento
│ │ │ ├── LogEventModel.cs # Modelo de datos de los logs (formato JSON)
│ │ │ ├── LoggerService.cs # Conexión e inserción de eventos en MongoDB
│ │ │ ├── MongoLogger.cs # Helper específico para Mongo (versión extendida)
│ │ │ └── UserSessionLogger.cs # Gestión de usuario/sesión
│ │ │
│ │ ├── Trackers/ # Trackers modulares
│ │ │ ├── GazeTracker.cs # Rastreo de mirada, trayectoria y fijaciones
│ │ │ ├── MovementTracker.cs # Rastreo de movimiento y giros bruscos
│ │ │ ├── FootTracker.cs # Rastreo de tobilleras (posición/velocidad pies)
│ │ │ └── HandTracker.cs # Rastreo de manos (posición/velocidad manos)
│ │ │
│ │ └── Manager/ # Gestión centralizada
│ │ └── VRTrackingManager.cs # Activa/desactiva trackers según configuración
│ │
│ └── src_bd_unity/ # Pruebas de conexión a base de datos
│ └── MongoSmokeTest/
│ ├── MongoSmokeTest.csproj # Proyecto de prueba en C#
│ └── Program.cs # Test de inserción/lectura en MongoDB
│
│
├── python-visualization/    # Generación de gráficos e informes (Matplotlib, Seaborn, etc.)
│   └── (scripts de plots, dashboards, reportes PDF)
│
└── README.md # (Este documento)


---

## 🧩 Descripción de componentes principales

### 📌 Unity / C# (`vr-logger`)
- **Logs/**
  - `LogAPI.cs`: funciones listas para cada tipo de evento (`LogTaskStart()`, `LogCollision()`, etc.).  
  - `LoggerService.cs`: maneja conexión a Mongo y escritura de eventos.  
  - `LogEventModel.cs`: define el formato JSON estándar de cada evento.  
  - `UserSessionLogger.cs`: inicializa sesión de usuario en Unity.  

- **Trackers/**
  - `GazeTracker.cs`: registra mirada, posición y orientación periódicamente.  
  - `MovementTracker.cs`: registra posición, velocidad y giros bruscos.  
  - `FootTracker.cs`: registra trayectoria de tobilleras.  
  - `HandTracker.cs`: registra movimiento de manos.  

- **Manager/**
  - `VRTrackingManager.cs`: panel central para activar/desactivar trackers según hardware disponible.  

- **src_bd_unity/**
  - Proyecto auxiliar con pruebas de conexión a MongoDB.  

---

### 📌 Python (`python-analysis`)
- `test_mongo.py`: script de ejemplo para leer logs desde MongoDB y verificar que se insertan correctamente.  
- (a futuro) scripts para:
  - Parsear logs.  
  - Filtrar por tipo/usuario.  
  - Calcular métricas (eficiencia, efectividad, satisfacción, presencia).  
  - Generar visualizaciones y estadísticas.  

---

### 📌 Python (python-visualization)
- Graficar métricas de efectividad, eficiencia, satisfacción y presencia.
- Generar mapas de calor de mirada y movimiento.
  - Comparar condiciones experimentales (ej. con/ sin audio, distintos modos de disparo).
  - Exportar resultados en informes PDF o dashboards interactivos.
  - Crear visualizaciones listas para presentaciones o artículos. 

---


## 🚀 Flujo de datos

1. **Unity (C#)** → Los trackers (`GazeTracker`, `MovementTracker`, etc.) recogen datos en runtime.  
2. **LoggerService** → Inserta cada evento en la colección de MongoDB en tiempo real.  
3. **MongoDB** → Actúa como almacenamiento central de logs.  
4. **Python** → Procesa los logs guardados, genera estadísticas y visualizaciones basadas en la **tabla de indicadores**.  

---

## 📖 Notas de uso
- Todos los scripts de Unity deben estar bajo `Runtime/` para empaquetarlos fácilmente como **Unity Package**.  
- Los trackers se activan desde `VRTrackingManager.cs` o manualmente añadiéndolos como componentes en la escena.  
- MongoDB debe estar corriendo en local o en un servidor accesible antes de ejecutar el juego.  
- Los scripts Python son independientes y pueden ejecutarse desde consola/IDE (ej. PyCharm).  

---

