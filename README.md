# 🧠 VR USER EVALUATION – README ACTUALIZADO (v2.1)

## 📘 Descripción general

Sistema modular para **monitorizar, almacenar y analizar el comportamiento de usuarios en entornos VR**, combinando **Unity + MongoDB + Python**.

Incluye:
* SDK de **logging universal para Unity**.
* **Base de datos MongoDB** (local o remota).
* **Pipeline de análisis automático** en Python (Efectividad, Eficiencia, Satisfacción, Presencia).
* **Informes PDF** y **dashboard interactivo**.

---

## 🛠️ Instalación y Configuración

### 1. Requisitos Previos
* **Unity 2021.3+**
* **MongoDB Community Server** (o Atlas Cloud)
* **Python 3.9+**

### 2. Configuración del Entorno Python
Para ejecutar el análisis y visualizar el dashboard, instala las dependencias:

```bash
pip install -r requirements.txt
```

Este comando instalará librerías clave como `pandas`, `pymongo`, `streamlit`, `plotly`, `reportlab`, etc.

---

## 🚀 Flujo de Trabajo (Unity -> Mongo -> Python)

### Paso 1: Configurar en Unity
1. Usa el componente **`UserSessionManager`** en tu escena para definir la conexión a la Base de Datos:
   * **Connection String**: `mongodb://localhost:27017`
   * **Database Name**: `vr_experiment_db`
   * **Collection Name**: `logs`
2. Configura tu experimento con **`ExperimentConfig`** y un **`ExperimentProfile`**.

*(Ver detalles completos en `vr_logger/README.md`)*

### Paso 2: Análisis Automático
El script `python_analysis/vr_analysis.py` se conecta a MongoDB, descarga los nuevos logs y genera los informes.

**Ejecución manual:**
```bash
python -m python_analysis.vr_analysis
```

**Automatización (Task Scheduler / Cron):**
Puedes programar este script para que se ejecute cada noche y tener los informes listos por la mañana.

*   **Windows (Task Scheduler):**
    1.  Crear Tarea Básica > "Analisis Diario VR".
    2.  Acción: Iniciar programa.
    3.  Programa: `python.exe`.
    4.  Argumentos: `ruta/al/proyecto/python_analysis/vr_analysis.py`.
*   **Mac/Linux (Cron):**
    ```bash
    0 3 * * * /usr/bin/python3 /ruta/proyecto/python_analysis/vr_analysis.py >> /tmp/log_analisis.txt
    ```

### Paso 3: Visualización en Dashboard
El dashboard interactivo permite filtrar y comparar datos en tiempo real.

```bash
streamlit run python_visualization/visual_dashboard.py
```

**Características del Dashboard:**
*   **Filtros Dinámicos:** Selecciona un `User ID`, `Group ID` (Control vs Experimental) o `Session ID` específico.
*   **Comparativas:** Visualiza gráficas de barras comparando métricas entre grupos.
*   **Deep Dive:** Haz clic en los puntos de las gráficas de dispersión para ver detalles de esa sesión.

---

## 📊 Glosario de Métricas

Para una interpretación científica correcta, distinguimos entre variables independientes (lo que cambias) y dependientes (lo que mides).

### 🔹 Variables Independientes (Input)
Son las condiciones que manipulas en el experimento. Se configuran en el `ExperimentProfile`:
*   **Grupo:** (Ej: "Con Ayudas" vs "Sin Ayudas").
*   **Variable Independiente:** Un valor específico (ej: "Velocidad=Alta", "Iluminación=Baja") que define la condición de la sesión.

### 🔸 Variables Dependientes (Output)
Son las métricas calculadas automáticamente por el sistema.

#### 1. Efectividad (¿Logran el objetivo?)
*   **`Hit Ratio`**: Precisión pura. `(Aciertos / Disparos Totales)`. Ideal para shooters o tareas de selección.
*   **`Success Rate`**: Tasa de éxito en tareas. `(Tareas Completadas / Tareas Intentadas)`.
*   **`Success After Restart`**: Resiliencia. `(Reinicios seguidos de éxito / Total de reinicios)`. Indica si el usuario aprende tras fallar.

#### 2. Eficiencia (¿Cuánto recursos consumen?)
*   **`Avg Task Duration`**: Tiempo medio (ms) en completar una tarea exitosa.
*   **`Navigation Errors`**: Cantidad de colisiones o salidas de ruta (`navigation_error` role).
*   **`Time Per Success`**: Tiempo total de sesión dividido por número de éxitos. Métrica global de rendimiento.

#### 3. Satisfacción (Experiencia de usuario)
*   **`Aid Usage`**: Cuántas veces el usuario solicitó ayuda o usó pistas (`help_event`).
*   **`Interface Errors`**: Errores al interactuar con UI (botones equivocados, menús cerrados sin querer).
*   **`Voluntary Play Time`**: Tiempo (s) que el usuario sigue jugando *después* de completar la tarea obligatoria. Indicador fuerte de "Engagement".

#### 4. Presencia (Inmersión)
*   **`Sound Localization Time`**: Tiempo (s) desde que suena un estímulo (`audio_event`) hasta que el usuario lo mira (`head_turn`).
*   **`Activity Level`**: Cantidad de acciones por minuto.
*   **`Inactivity Time`**: Tiempo acumulado sin inputs ni movimiento significativo.

---

## 📂 Estructura de Salida
Cada análisis genera una carpeta con fecha en `python_analysis/pruebas/`:
*   `results.json/csv`: Datos crudos para Excel/SPSS.
*   `grouped_metrics.csv`: Una fila por sesión (ideal para ANOVA).
*   `final_report.pdf`: Informe ejecutivo automático con gráficas y tablas.
*   `figures/`: Todas las gráficas en formato PNG de alta resolución.

---

## 📅 Autoría y licencia

Proyecto **VR USER EVALUATION v2.1**
Licencia: Uso académico y experimental.
