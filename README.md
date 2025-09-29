# 🎮 VR User Evaluation – TFG

Este proyecto implementa un sistema completo para **monitorizar, analizar y visualizar** el comportamiento de usuarios en entornos de Realidad Virtual.

- **vr-logger (Unity / C#)** → Scripts para recoger datos en tiempo real desde Unity (logs de usuario, trackers de mirada, movimiento, etc.) y almacenarlos en MongoDB.  
- **python-analysis (Python)** → Scripts para procesar, parsear y analizar los logs guardados en MongoDB, extrayendo estadísticas y métricas definidas en la tabla de indicadores.


## 📂 Estructura del repositorio

```
TFG_VR_Analysis/
├── vr-logger/                   # Unity (C#) – generación de logs en MongoDB
│   └── Runtime/
│       ├── Manager/              # Gestión de sesiones y trackers
│       │   ├── UserSessionManager.cs
│       │   ├── VRTrackingManager.cs
│       ├── Trackers/             # Sensores y entradas
│       │   ├── GazeTracker.cs
│       │   ├── MovementTracker.cs
│       │   ├── HandTracker.cs
│       │   ├── FootTracker.cs
│       └── Logs/                 # Conexión a MongoDB
│           ├── LoggerService.cs
│           ├── LogEventModel.cs
│           ├── LogAPI.cs
│
├── python_analysis/              # Procesamiento y análisis de logs
│   ├── __init__.py
│   ├── log_parser.py             # Carga desde Mongo/JSON/CSV → DataFrame
│   ├── metrics.py                # Indicadores (efectividad, eficiencia, satisfacción, presencia) + custom
│   ├── exporter.py               # Exporta resultados (JSON/CSV)
│   └── test_mongo.py             # Test de conexión con MongoDB
│
├── python_visualization/         # Visualización y reportes
│   ├── __init__.py
│   ├── visualize_groups.py       # Gráficos estáticos (PNG, Seaborn)
│   ├── pdf_reporter.py           # Informe PDF con métricas + gráficos
│   └── visual_dashboard.py       # Dashboard web interactivo (Streamlit + Plotly)
│
├── pruebas/                      # Tests end-to-end
│   ├── test_pipeline.py          # Simulación sin BD (logs falsos → análisis)
│   └── test_pipeline_db.py       # Pipeline completo con MongoDB real
│   ├── exports_YYYYMMDD_HHMMSS/  # Outputs exportados (JSON, CSV, PDF) – ignorados en Git
│   ├── figures_YYYYMMDD_HHMMSS/  # Gráficos generados – ignorados en Git
│
├── requirements.txt              # Dependencias Python
└── README.md                     # Este archivo
```

---

## ⚙️ Instalación de dependencias (Python)

1. Crea un entorno virtual (recomendado):

   ```bash
   python -m venv venv
   source venv/bin/activate      # Linux/Mac
   venv\Scripts\activate         # Windows
   ```

2. Instala las dependencias:

   ```bash
   pip install -r requirements.txt
   ```

📦 Dependencias principales:

* `pandas`, `numpy` → análisis de datos
* `pymongo` → conexión con MongoDB
* `matplotlib`, `seaborn` → gráficos estáticos
* `plotly`, `streamlit` → visualización interactiva (dashboard web)
* `reportlab` → informes PDF

### DLLs de Mongo Db

MongoDB no es un paquete oficial de Unity, por lo que se debe incluir el driver oficial de MongoDB para C# en el paquete:

 * Descarga desde MongoDB .NET Driver.

 * Copia al directorio vr_logger/Runtime/Plugins/ los siguientes DLLs:

     * MongoDB.Driver.dll

     * MongoDB.Bson.dll

     * MongoDB.Driver.Core.dll

Unity los compilará junto a tus scripts y permitirán conectar directamente con MongoDB desde C#.


---

## ⚙️ Requisitos (Unity – vr-logger)

La parte de **Unity** (`vr-logger/`) requiere:

* Unity 2021+ (recomendado)
* Paquetes XR:

  * `XR Interaction Toolkit`
  * `OpenXR Plugin`
* Dependiendo del hardware:

  * `SteamVR` (HTC Vive)
  * `Oculus XR Plugin` (Quest)

⚠️ Estas dependencias se instalan con el **Unity Package Manager**, no con `pip`.

---

## ▶️ Cómo ejecutar el pipeline completo

1. Asegúrate de que **MongoDB está corriendo** en tu máquina:

   ```bash
   mongod
   ```

2. Ejecuta el test completo con BD:

   ```bash
   python pruebas/test_pipeline_db.py
   ```

Esto hará:

* Insertar logs de prueba en Mongo (`test.tfg`)
* Analizarlos con `LogParser` y `MetricsCalculator`
* Exportar resultados (`exports_.../`)
* Generar gráficos (`figures_.../`)
* Crear un informe PDF final

---

## 🌐 Visualización interactiva (dashboard web)

Lanza el dashboard:

```bash
streamlit run python_visualization/visual_dashboard.py
```

Se abrirá en tu navegador en:

```
http://localhost:8501
```

Podrás ver:

* Indicadores de efectividad, eficiencia, satisfacción y presencia
* Eventos personalizados (custom events)
* Tabla completa con todas las métricas

---

## 📄 Notas

* Los directorios `exports_*/` y `figures_*/` **no se versionan en Git** (están en `.gitignore`).
* Solo se sube el **código fuente**, no los resultados generados.
* Para correr con datos reales, Unity (`vr-logger`) insertará los logs directamente en MongoDB.

---

## ✨ Estado actual

✅ Unity (C#) genera logs en MongoDB
✅ Python analiza los logs y calcula métricas (tabla de indicadores + eventos custom)
✅ Exportador genera JSON/CSV
✅ Visualización con gráficos estáticos y dashboard web
✅ Informe PDF con tablas y gráficos
✅ Tests (`pruebas/`) permiten probar todo el pipeline con y sin BD

## 📖 Notas de uso
- Todos los scripts de Unity deben estar bajo `Runtime/` para empaquetarlos fácilmente como **Unity Package**.  
- Los trackers se activan desde `VRTrackingManager.cs` o manualmente añadiéndolos como componentes en la escena.  
- MongoDB debe estar corriendo en local o en un servidor accesible antes de ejecutar el juego.  
- Los scripts Python son independientes y pueden ejecutarse desde consola/IDE (ej. PyCharm).  

---

