
# 👨‍💻 VR LOGGER – MANUAL DEL DESARROLLADOR
*(Actualizado: Soporte para ExperimentProfile, Custom Roles y Guía de Métricas)*


## 📘 Introducción


Este documento es la referencia técnica para integrar **VR Logger** en proyectos Unity. Explica cómo configurar el sistema, cómo usar los nuevos **Experiment Profiles** para gestionar diferentes minijuegos, y **cómo programar los eventos** específicos para que el sistema de análisis (Python) calcule automáticamente cada métrica.


---


## ⚙️ 1️⃣ Configuración del Sistema


### A. Dependencias
Asegúrate de tener las DLLs de MongoDB en `Assets/Plugins/`:
* `MongoDB.Driver.dll`, `MongoDB.Bson.dll`, etc. Además, asegúrate de que todo requirement.txt está instalado. Finalmente, debes tener un archivo Experiment_config.json en Assets/Resources (hay un ejemplo en la carpeta vr-logger).
* **IMPORTANTE PARA EYE TRACKING**: Si vas a usar seguimiento ocular, necesitas:
    1.  La carpeta **`VIVESR`** (SDK de SRanipal) importada en `Assets/`.
    2.  El prefab **`SRanipal Eye Framework`** presente en la escena.


23: ### B. Inicialización
24: Agrega el componente `UserSessionManager` a un objeto persistente de la escena (ej. `VRManager`).
25: El sistema necesita los siguientes componentes en el mismo objeto (o en la escena):
26: 
27: 1. Crea un objeto vacío llamado `VRManager`.
28: 2. Añádele el script `ExperimentConfig` y `UserSessionManager`.
29: 3. **¡IMPORTANTE!** Añade `VRTrackingManager` y asigna las referencias (Cámara, Manos, XR Origin).
30: 4. Arrastra a su ranura "Active Profile" un perfil de experimento (ver sección 2).


---


## 📝 2️⃣ Experiment Profiles y Mapeo de Roles


El sistema permite guardar configuraciones distintas para diferentes experimentos (Shooter, Puzzles, etc). 


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

### 🌐 VR Logger Configurator (Manejo Vía Streamlit)
Como alternativa a los *ScriptableObjects* en el propio Editor de Unity, los investigadores ahora pueden utilizar la **Aplicación Web** `python_analysis/experiment_configurator.py` escrita en Streamlit.
* Permite crear la configuración desde cualquier navegador.
* Contiene una pestaña nueva para gestionar una **Colección de Participantes** (`test/participants`) donde guardar nombres, edad y notas de experiencia VR.
* Al presionar "Push to MongoDB" desde la web, envía un JSON a la base de datos.
* **En Unity**: El componente `ExperimentConfig` ahora incluye un atajo (*Context Menu*) llamado **`Pull Config from Streamlit (MongoDB)`**. Al usarlo, descargará la última configuración validada que hiciste en la web y la sobrepondrá en el Inspector de Unity al instante para su ejecución.

### 📐 Área de Juego Dinámica para Mapas de Calor
El componente `VRTrackingManager` ahora se encarga de leer el área física de juego configurada en las gafas VR (Guardian/Chaperone) al arrancar el experimento (Runtime). 
Automáticamente enviará esta información (`PlayAreaWidth` y `PlayAreaDepth`) al script `ExperimentConfig`. Por este motivo, estos campos ya no aparecen ocultos ni necesitan ser rellenados a mano por el experimentador. Los scripts de visualización en Python (`spatial_plotter.py` y el dashboard) usarán estas **medidas reales y dinámicas** para dibujar y centrar los mapas 2D de trayectorias (heatmap) de las manos o el usuario con las dimensiones exactas del mundo real.

---


## 💻 3️⃣ Guía de Implementación de Métricas


Aquí se ofrece un ejemplo de **qué código C# puedes escribir** para alimentar cada métrica individualmente en tus experimentos.


### 📌 Métrica: Hit Ratio y Accuracy
* **Fórmula**: `Aciertos / (Aciertos + Fallos)`
* **Requiere**: Eventos con rol `action_success` y `action_fail`.


```csharp
// Cuando el jugador acierta (ej. rompe un jarrón)
LoggerService.LogEvent("gameplay", "jarron_roto", 1, new {
   event_role = "action_success", 
});


// Cuando el jugador falla (ej. tira piedra fuera)
LoggerService.LogEvent("gameplay", "tiro_fallido", 0, new {
   event_role = "action_fail"
});
```

**🔫 Ejemplo Shooter:**
```csharp
// Script en la bala (Bullet.cs)
void OnCollisionEnter(Collision collision) 
{
    // CASO 1: ACIERTO
    if (collision.gameObject.CompareTag("Enemy")) 
    {
        // Calcular distancia (opcional)
        float dist = Vector3.Distance(transform.position, player.position);

        LoggerService.LogEvent("combat", "bullet_hit", 1, new {
            event_role = "action_success",
            target = collision.gameObject.name,
            distance = dist
        });

        Destroy(gameObject); // Destruir bala
    }
    // CASO 2: FALLO (Chocar con suelo/pared)
    else 
    {
        LoggerService.LogEvent("combat", "bullet_miss", 1, new {
            event_role = "action_fail",
            hit_object = collision.gameObject.name
        });
        
        Destroy(gameObject);
    }
}
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

**🔫 Ejemplo Shooter:**
```csharp
// En tu script de gestión de juego (GameManager.cs)
public void CheckWinCondition() 
{
    // CASO 1: VICTORIA (Matar al Boss)
    if (bossHealth <= 0) 
    {
        LoggerService.LogEvent("flow", "mission_complete", "success", new {
            event_role = "task_end",
            enemies_killed = killedCount,
            final_health = playerHealth
        });

        ShowVictoryScreen();
    }
}

// CASO 2: DERROTA (Jugador muere)
public void OnPlayerDeath() 
{
    LoggerService.LogEvent("flow", "mission_failed", "fail", new {
        event_role = "task_end",
        reason = "health_depleted",
        last_damage_source = lastAttacker
    });

    ShowGameOverScreen();
}
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

**🔫 Ejemplo Shooter:**
```csharp
// En tu script Spawner.cs
public void SpawnEnemy() 
{
    // 1. APARECE EL ENEMIGO (Inicio del Cronómetro)
    GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, resultRotation);
    
    LoggerService.LogEvent("combat", "enemy_spawn", 1, new {
        event_role = "task_start", // <--- INICIO TAREA REACCIÓN
        enemy_type = "sniper",
        spawn_id = enemy.GetInstanceID()
    });
}

// En el script del Enemigo (EnemyHealth.cs)
public void TakeDamage(int damage) 
{
    currentHealth -= damage;

    // 2. EL JUGADOR REACCIONA Y ACIERTA (Fin del Cronómetro)
    if (currentHealth <= 0) 
    {
        LoggerService.LogEvent("combat", "sniper_down", 1, new {
            event_role = "action_success", // <--- FIN TAREA (Cálculo automático: t_success - t_start)
            weapon = "rifle" 
        });
        
        Die();
    }
}
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

**🔫 Ejemplo Shooter:**
```csharp
// Jugador choca contra una pared invisible del nivel
void OnControllerColliderHit(ControllerColliderHit hit) {
    if (hit.gameObject.CompareTag("Boundary")) {
        LoggerService.LogEvent("movement", "border_collision", 1, new {
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

**🔫 Ejemplo Shooter:**
```csharp
// En el GameManager, tras acabar la partida
public void OnGameWon() 
{
    // 1. EVENTO FINAL DE TAREA
    LoggerService.LogEvent("flow", "task_end", "success", ...);
    
    // NO CERRAR LA SESIÓN AÚN.
    // Dejar al jugador en el nivel (Free Roam)
    enableFreeRoam = true;
}

// En script de Diana (Target.cs)
void Updated() 
{
    if (enableFreeRoam && wasHit) 
    {
        // CUALQUIER ACTIVIDAD AQUÍ SUMA AL "VOLUNTARY PLAY TIME"
        LoggerService.LogEvent("interaction", "target_practice", 1, new {
            event_role = "interaction_event" // Mantiene el reloj de "Juego Voluntario" contando
        });
    }
}
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

**🔫 Ejemplo Shooter:**
```csharp
// Script en botón UI (HintButton.cs)
public void OnPointerClick() 
{
    // Solo si el juego está activo
    if (GameManager.IsPlaying) 
    {
        ShowPathToObjective();

        // REGISTRAR AYUDA
        LoggerService.LogEvent("ui", "waypoint_requested", 1, new {
            event_role = "help_event", // Cuenta para la métrica AidUsage
            current_objective = GameManager.CurrentObjective
        });
    }
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

**🔫 Ejemplo Shooter:**
```csharp
// Script del Arma (Weapon.cs)
public void TryShoot() 
{
    if (currentAmmo > 0) 
    {
        FireBullet();
    }
    else 
    {
        // JUGADOR INTENTA DISPARAR SIN BALAS -> ERROR DE INTERFAZ/USO
        PlayClickSound();

        LoggerService.LogEvent("combat", "dry_fire", 1, new {
            event_role = "interface_error",
            context = "empty_magazine",
            attempts = consecutiveDryFires++
        });
    }
}
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

**🔫 Ejemplo Shooter:**
```csharp
// Script de Enemigo (EnemyAudio.cs)
public void PlayReloadSound() 
{
    audioSource.PlayOneShot(reloadClip);

    // 1. REGISTRAR EL ESTÍMULO SONORO
    LoggerService.LogEvent("audio", "enemy_reload_cue", 1, new {
        event_role = "audio_triggered",
        pos = transform.position
    });
}

// Script en el Jugador (PlayerListener.cs)
void Update() 
{
    // 2. DETECTAR SI MIRA HACIA EL SONIDO
    Vector3 toSound = (enemyPos - transform.position).normalized;
    float dot = Vector3.Dot(transform.forward, toSound);

    // Si mira directamente (aprox 30 grados)
    if (dot > 0.85f && !hasReacted) 
    {
        hasReacted = true;
        LoggerService.LogEvent("movement", "player_reacted_sound", 1, new {
            event_role = "head_turn" // Cronómetro se para aquí: t_head_turn - t_audio
        });
    }
}
```


---


## 📊 4️⃣ Verificar Resultados


1. Ejecuta tu escena en Unity.
2. Genera los eventos.
3. Cierra la run (para enviar `session_end` automáticamente si usas `UserSessionManager`).
4. Ejecuta el script de análisis: `python run_analysis.py`.
5. Abre el PDF generado en `python_analysis/pruebas/analysis_XXX/final_report.pdf`.
6. Mira los resultados en el dashboard (tiene búsqueda dinámica) con > streamlit run python_visualization/visual_dashboard.py 


Si los datos salen a 0, verifica:
1. ¿Has asignado los **Roles** correctos en el Perfil o en el código (`event_role`)?
2. ¿Has enviado el par de eventos necesarios (ej: `task_start` Y `action_success` para tiempos)?


---