# 🛠️ Manual del Desarrollador – VR User Evaluation

Este documento explica cómo funciona el proyecto a nivel técnico y cómo mantener o extender el código.
Está pensado para desarrolladores que quieran **modificar el sistema**, **añadir nuevos indicadores** o **usar el paquete Unity (`vr_logger`) en otros proyectos**.

---

## 📂 Estructura del proyecto

```
TFG_VR_Analysis/
├── vr_logger/                   # Paquete Unity (C#)
│   ├── package.json              # Metadatos y dependencias Unity
│   └── Runtime/                  # Código fuente del paquete
│       ├── Logs/                 # Logging en MongoDB
│       ├── Manager/              # Gestión de sesiones y trackers
│       ├── Trackers/             # Gaze, movimiento, manos, pies
│       └── src_bd_unity/         # Test de conexión Mongo
│
├── python_analysis/              # Análisis de datos (Python)
│   ├── log_parser.py
│   ├── metrics.py
│   ├── exporter.py
│   └── test_mongo.py
│
├── python_visualization/         # Visualización y reportes (Python)
│   ├── visualize_groups.py
│   ├── pdf_reporter.py
│   └── visual_dashboard.py
│
├── pruebas/                      # Pruebas de integración end-to-end
│   ├── test_pipeline.py
│   ├── test_pipeline_db.py
│   ├── exports_*/                # Resultados (ignorado en git)
│   └── figures_*/                # Gráficos (ignorado en git)
```

---

## ⚙️ Flujo general del sistema

1. **Unity (vr_logger)**

   * Genera eventos (posición, orientación, trayectorias, interacción, etc.).
   * Los serializa como JSON y los guarda en **MongoDB**.

2. **Python Analysis**

   * `log_parser.py`: carga logs desde Mongo o archivos JSON/CSV.
   * `metrics.py`: calcula indicadores de efectividad, eficiencia, satisfacción, presencia y custom.
   * `exporter.py`: exporta resultados (JSON/CSV).

3. **Python Visualization**

   * `visualize_groups.py`: gráficos estáticos (para PDF).
   * `pdf_reporter.py`: genera informes PDF.
   * `visual_dashboard.py`: dashboard web interactivo con Streamlit + Plotly.

4. **Pruebas (pipelines)**

   * `test_pipeline.py`: logs simulados → análisis completo.
   * `test_pipeline_db.py`: inserta logs en Mongo, los procesa y genera outputs completos.

---

## 📦 Paquete Unity (`vr_logger`)

Este proyecto está empaquetado como **Unity Package** para que pueda importarse en otros proyectos.

### `package.json`

```json
{
  "name": "com.github.lauribla.vr_logger",
  "version": "1.0.0",
  "displayName": "VR Logger",
  "description": "Sistema de logging para experimentos VR con Unity.",
  "unity": "2022.3",
  "author": {
    "name": "Laura Hernández",
    "email": "laura.hhernandez@alumnos.upm.es",
    "url": "https://github.com/lauribla/TFG_VR_Analysis"
  },
  "dependencies": {
    "com.unity.xr.management": "4.2.0",
    "com.unity.xr.openxr": "1.8.2",
    "com.unity.inputsystem": "1.5.1"
  }
}
```

### Importación en otro proyecto Unity

En el `manifest.json` del proyecto final añade:

```json
"dependencies": {
  "com.github.lauribla.vr_logger": "https://github.com/lauribla/TFG_VR_Analysis.git?path=/vr_logger#main"
}
```

⚠️ Unity descargará automáticamente las dependencias declaradas en `package.json`.

---

## 🔧 Desarrollo en Python

### Añadir nuevos indicadores

* Editar `metrics.py` → añadir tu función de cálculo en `compute_all()`.
* El valor aparecerá automáticamente en:

  * `exporter.py` (JSON/CSV)
  * `visualize_groups.py` (si añades gráfico)
  * `pdf_reporter.py` (tabla + gráfico)
  * `visual_dashboard.py` (dashboard web)

### Añadir nuevos eventos personalizados

* Basta con añadir logs con `"event_type": "custom"` en Unity.
* `metrics.py` los detecta y los pasa como `custom_events`.
* `visualize_groups.py` y el dashboard web los grafican automáticamente.

---

## 📄 Convenciones

* **C# (Unity)**: cada tracker (gaze, movimiento, manos, pies) va en `Runtime/Trackers/`.
* **Python**: todo sigue convención snake_case.
* **Pruebas**: cada pipeline genera carpetas con timestamp (`exports_2025...`, `figures_2025...`).

---

## 🚀 Checklist para contribuir

1. Clonar el repo.
2. Instalar dependencias Python:

   ```bash
   pip install -r requirements.txt
   ```
3. Tener MongoDB corriendo (`mongod`).
4. Probar el pipeline:

   ```bash
   python pruebas/test_pipeline_db.py
   ```
5. Si modificas el paquete Unity, revisa que el `package.json` esté actualizado.
6. Si añades indicadores nuevos, actualiza el `DEVELOPER_GUIDE.md` y el `README.md`.

---

## ✨ Estado actual

* VR Logger (Unity) funcional como paquete.
* Python Analysis/Visualization integrado.
* Exportación en JSON/CSV + gráficos + PDF + dashboard web.
* Pipelines para pruebas unitarias y end-to-end.
