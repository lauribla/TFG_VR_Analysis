# VR Logger - Wrappers Automáticos de Métricas

Esta colección de componentes (Wrappers) "Plug-and-Play" permite que desarrolladores e investigadores registren el comportamiento del usuario en entornos VR sin necesidad de escribir código adicional ni alterar la lógica de las mecánicas del juego.

Estos componentes se adhieren a entidades ya existentes y aprovechan el motor pasivo de Unity (físicas, ciclo de vida, eventos de UI) para emitir semánticamente eventos al `LoggerService`.

---

## 🚀 Componentes Disponibles

### 1. `TaskZoneBoundaryLogger`
Mide intentos y duraciones de tareas basadas en desplazamientos espaciales.
* **Métricas que alimenta:** `success_rate`, `avg_task_duration_ms`, `time_per_success_s`.
* **Uso sugerido:** Laberintos, escapar de habitaciones, cruzar obstáculos.
* **Cómo usar:**
  1. Añade este componente a un GameObject con un `Collider` en modo `Is Trigger` (representa la zona de inicio).
  2. Configura el `Task Id` (ej. "Puzzle_Luz").
  3. (Opcional) Asigna otro Collider en la variable `Success Exit Zone`. Si el jugador toca ese otro collider, la tarea se marca como éxito automáticamente.

### 2. `UIActionInterceptorLogger`
Intercepta limpiamente interacciones con botones, toggles o sliders estándar de la UI de Unity sin romper la funcionalidad original.
* **Métricas que alimenta:** `interface_errors`, `hit_ratio`.
* **Uso sugerido:** Cuestionarios in-game, paneles de selección de opciones, menús de inicio de experimento.
* **Cómo usar:** 
  1. Arrastra el script a cualquier elemento UI que ya tenga un `Button`, `Toggle`, etc. configurado.
  2. En el Inspector, ponle nombre en `Action Id`. 
  3. Si ese botón representa un error (ej. "Salir" cuando debería haber pulsado "Continuar"), marca `Is Error Context`.

### 3. `InertiaInactivityLogger`
Detecta periodos de inactividad física (el jugador se queda quieto mucho rato mirando al infinito).
* **Métricas que alimenta:** `inactivity_time_s`, `activity_level_per_min`, `voluntary_play_time_s`.
* **Cómo usar:** 
  1. Añádelo al Player/Camera principal o actívalo en tu `UserSessionManager`.
  2. Se configurará automáticamente buscando `Camera.main` si dejas el `Target Tracker` vacío.
  3. Ajusta `Inactivity Threshold S` (ej: 5.0 segundos) y la varianza para considerar temblores residuales (0.03 metros).

### 4. `LifecycleReactionLogger`
Mide "Time To Kill" o tiempo de reacción visual/mecánico atado a la instanciación de un objeto.
* **Métricas que alimenta:** `avg_reaction_time_ms`, `first_success_time_s`.
* **Uso sugerido:** Dianas que aparecen y desaparecen, objetos arrojados que hay que atrapar, enemigos.
* **Cómo usar:**
  1. Añádelo al prefab del elemento objetivo (ej. "Diana_Roja").
  2. Cuando se haga `Instantiate(prefab)`, empezará a cronometrar.
  3. Cuando el objeto sea destruido por el jugador, emitirá automáticamente el log de neutralización.

### 5. `NavigationErrorColliderLogger`
Transforma choques físicos (ruido) en penalizaciones lógicas limpias con *cooldown*.
* **Métricas que alimenta:** `navigation_errors`.
* **Uso sugerido:** Entornos guiados donde no se debe pisar fuera del camino, o chocar contra mobiliario de la habitación real.
* **Cómo usar:**
  1. Añade el script al `PlayerRig`.
  2. Configura en `Allowed Error Mask` la layer de los obstáculos indebidos (ej. Layer "Paredes_Castigo").
  3. Automáticamente dejará un log 1 vez por choque válido, controlado por el delay del Inspector (ej. 500ms).

### 6. `CheckpointProgressionLogger`
Un registrador clásico pero inteligente de un solo uso por nivel.
* **Métricas que alimenta:** `progression`.
* **Uso sugerido:** Puntos narrativos, pasillos conectores, control de flujo lineal.
* **Cómo usar:**
  1. Ponlo en una Trigger Box transparente.
  2. Dale un nombre en `Checkpoint Name` (ej. "Sala_02_Llegada") y un número de progreso (ej. 45%).
  3. El script se auto-destruirá tras emitir el éxito para evitar logs inflados si el usuario vuelve hacia atrás.

### 7. `AidInteractionLogger`
Comprueba si el usuario se ha detenido conscientemente a asimilar una pista de ayuda.
* **Métricas que alimenta:** `aid_usage`.
* **Uso sugerido:** Carteles 3D o tooltips a los que el usuario apunta con la mirada o las manos.
* **Cómo usar:**
  1. Aládelo a un Canvas 3D que funcione con el EventSystem de Unity (puntero láser de los mandos o Gaze de cabeza).
  2. Ajusta el `Recognition Time Threshold`. Si la mirada se aparta *antes* de este tiempo (ej: 1 segundo), se ignora porque fue un roce de vista accidental. Si pasa más tiempo, se cuenta como uso de asistencia logrando registrar hasta qué punto el usuario necesitaba ayuda real.

---
*Diseñado bajo una capa de analítica no intrusiva para el TFG de Monitorización de Conducta en VR.*
