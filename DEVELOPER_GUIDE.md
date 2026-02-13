
# 👨‍💻 VR LOGGER – MANUAL DEL DESARROLLADOR
*(Actualizado: Soporte para ExperimentProfile, Custom Roles y Guía de Métricas)*


## 📘 Introducción


Este documento es la referencia técnica para integrar **VR Logger** en proyectos Unity. Explica cómo configurar el sistema, cómo usar los nuevos **Experiment Profiles** para gestionar diferentes minijuegos, y **cómo programar los eventos** específicos para que el sistema de análisis (Python) calcule automáticamente cada métrica.


---


## ⚙️ 1️⃣ Configuración del Sistema


### A. Dependencias
Asegúrate de tener las DLLs de MongoDB en `Assets/Plugins/`:
* `MongoDB.Driver.dll`, `MongoDB.Bson.dll`, etc. Además, asegúrate de que todo requirement.txt está instalado. Finalmente, debes tener un archivo Experiment_config.json en Assets/Resources (hay un ejemplo en la carpeta vr-logger).


### B. Inicialización
Agrega el componente `UserSessionManager` a un objeto persistente de la escena que tiene los comportamientos a medir (ej. `VRManager`).
El sistema necesita un **ExperimentConfig** para funcionar correctamente (puedes añadirlo al objeto que tiene UserSessionManager).


1. Crea un objeto vacío llamado `ExperimentManager`.
2. Añádele el script `ExperimentConfig` y `UserSessionManager`.
3. Arrastra a su ranura "Active Profile" un perfil de experimento (ver sección 2).


---


## 📝 2️⃣ Experiment Profiles y Mapeo de Roles


El sistema permite guardar configuraciones distintas para cada minijuego (Shooter, Puzzles, etc.) usando **ScriptableObjects**.


### Creación de un Perfil
1. Crea una carpeta Project en Assets: **Click Derecho -> Create -> VRLogger -> Experiment Profile**.
2. Ponle nombre (ej: `Profile_Shooter`).
3. Configura los IDs (`ExperimentId`, `SessionName`).
4. Decide cuántas personas serán en el experimento y el grupo al que pertenecen (Puede ser 1 y puedes añadir su ID (recomendable o se le asiganrá por defecto u001)), recomendable añadir la variable independiente que se mide (para luego poder usarla para comporar en el análisis posterior).
5. Decide qué metricas quieres medir según el juego que tengas para el experimento. Puedes medir todas y decidir qué peso tendrán en su categoría correspondiente (Efectividad, eficiencia, presencia o satisfacción)


### ⭐ Mapeo de Eventos (Custom Event Roles)
Esta es la parte más potente. Puedes definir qué eventos de **TU** juego cuentan como "éxito", "fallo", etc., sin tocar código.


En el Perfil, busca la lista **"Custom Event Roles"**:
* **Event Name**: El nombre del evento que envías desde código (ej: `"globo_explotado"`).
* **Role**: El rol semántico que el análisis debe interpretar (ej: `action_success`).


**Ejemplo:**
* `"globo_explotado"` -> `action_success` (Cuenta para HitRatio)
* `"globo_escapado"` -> `action_fail` (Cuenta para HitRatio)
* `"tocar_pincho"` -> `navigation_error` (Cuenta para NavigationErrors)


---


## 💻 3️⃣ Guía de Implementación de Métricas


Aquí se detalla **qué código C# debes escribir** para alimentar cada métrica individualmente.


### 📌 Métrica: Hit Ratio y Accuracy
* **Fórmula**: `Aciertos / (Aciertos + Fallos)`
* **Requiere**: Eventos con rol `action_success` y `action_fail`.


```csharp
// Cuando el jugador acierta (ej. rompe un jarrón)
LoggerService.LogEvent("gameplay", "jarron_roto", 1, new {
   event_role = "action_success", // O mapeado en perfil
   weapon = "piedra"
});


// Cuando el jugador falla (ej. tira piedra fuera)
LoggerService.LogEvent("gameplay", "tiro_fallido", 0, new {
   event_role = "action_fail"
});
```


### 📌 Métrica: Success Rate (Tasa de Éxito de Tareas)
* **Fórmula**: `% de Tareas completadas con éxito`.
* **Requiere**: Eventos `task_end` con valor explícito "success" o "fail".


```csharp
// Al completar un nivel o puzzle
LoggerService.LogEvent("flow", "task_end", "success", new {
   event_role = "task_end",
   puzzle_id = "puzzle_01"
});


// Al perder o abandonar
LoggerService.LogEvent("flow", "task_end", "fail", new {
   event_role = "task_end"
});
```


### 📌 Métrica: Average Reaction Time (Tiempo de Reacción)
* **Fórmula**: Tiempo desde `task_start` hasta el PRIMER `action_success` o `action_fail`.
* **Uso**: Ideal para medir reflejos (ej. aparece un estímulo y el usuario dispara).


```csharp
// 1. Inicia el cronómetro (aparece el objetivo)
LoggerService.LogEvent("flow", "stimulus_appeared", 1, new {
   event_role = "task_start"
});


// ... pasa el tiempo ...


// 2. El usuario reacciona (acierta o falla)
// El sistema calculará automáticamente la diferencia de tiempo.
LoggerService.LogEvent("input", "disparo", 1, new {
   event_role = "action_success"
});
```


### 📌 Métrica: Navigation Errors (Errores de Navegación)
* **Requiere**: Eventos con rol `navigation_error` o `collision`.


```csharp
// Al chocar con una pared o entrar en zona prohibida
void OnCollisionEnter(Collision collision) {
   if (collision.gameObject.CompareTag("Wall")) {
       LoggerService.LogEvent("physics", "wall_collision", 1, new {
           event_role = "navigation_error"
       });
   }
}
```


### 📌 Métrica: Voluntary Play Time (Tiempo de Juego Voluntario)
* **Fórmula**: Tiempo que el usuario sigue jugando DESPUÉS de completar la tarea principal (`task_end` -> `success`).
* **Requiere**: Seguir enviando eventos después de la victoria.


```csharp
// El usuario gana
LoggerService.LogEvent("flow", "task_end", "success", ...);


// DEJA QUE SIGA JUGANDO.
// Cualquier evento posterior (movimiento, interacción) contará como tiempo voluntario
// hasta que cierres la sesión.
```


### 📌 Métrica: Aid Usage (Uso de Ayudas)
* **Requiere**: Eventos con rol `help_event`.


```csharp
// Usuario pulsa botón de pistas
public void OnHintButtonPressed() {
   LoggerService.LogEvent("ui", "hint_requested", 1, new {
       event_role = "help_event"
   });
}
```


### 📌 Métrica: Interface Errors (Errores de UI)
* **Requiere**: Eventos con rol `interface_error`.


```csharp
// Usuario intenta pulsar botón bloqueado o se equivoca
LoggerService.LogEvent("ui", "invalid_click", 1, new {
   event_role = "interface_error",
   button = "start_game_disabled"
});
```


### 📌 Métrica: Sound Localization Time (Localización de Sonido)
* **Fórmula**: Tiempo entre `audio_triggered` y `head_turn`.
* **Requiere**:


```csharp
// 1. Suena un audio 3D
LoggerService.LogEvent("audio", "enemy_footstep", 1, new {
   event_role = "audio_triggered",
   position = transform.position
});


// 2. El sistema detecta que el usuario gira la cabeza hacia la fuente
// (Esto suele requerir lógica en Update() para comprobar el ángulo)
if (IsLookingAtSource()) {
   LoggerService.LogEvent("movement", "head_turn_to_source", 1, new {
       event_role = "head_turn"
   });
}
```


---


## 📊 4️⃣ Verificar Resultados


1. Ejecuta tu escena en Unity.
2. Genera los eventos.
3. Cierra la app (para enviar `session_end` automáticamente si usas `UserSessionManager`).
4. Ejecuta el script de análisis: `python run_analysis.py`.
5. Abre el PDF generado en `python_analysis/pruebas/analysis_XXX/final_report.pdf`.


Si los datos salen a 0, verifica:
1. ¿Has asignado los **Roles** correctos en el Perfil o en el código (`event_role`)?
2. ¿Has enviado el par de eventos necesarios (ej: `task_start` Y `action_success` para tiempos)?


---


**Soporte**
Para dudas sobre el pipeline de Python, revisa `metrics.py` para ver la lógica exacta de cálculo.
