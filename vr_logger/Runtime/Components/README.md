# VR Logger - Guía de Componentes Plug & Play

Esta carpeta contiene scripts listos para usar (Drag & Drop) que alimentan *automáticamente* las métricas del dashboard en Python de análisis, sin necesidad de programar en C#.

## Cómo usar el sistema de ayudas

Simplemente arrastra estos componentes desde la carpeta `Runtime/Trackers/` a tus GameObjects en la escena de Unity y configúralos desde el Inspector.

| ¿Qué métrica quieres calcular en Python? | Componente a usar en Unity | Dónde ponerlo | Cómo lo conecto |
| :--- | :--- | :--- | :--- |
| **HitRatio, SuccessTime, ReactionTime** | `ActionOutcomeLogger` | En Enemigos o Dianas | Llama a `ReportSuccess()` desde el evento de recibir daño. |
| **NavigationErrors** | `ObstacleLogger` | En Paredes o Trampas | Marca "Log On Collision Enter". Listo, ¡es automático! |
| **AvgTaskDuration, SuccessRate, Retries** | `TaskFlowLogger` | En un objeto vacío o Botón UI | Llama a `StartTask()`, `EndTaskSuccess()` y `RestartTask()` desde tus botones de Interfaz. |
| **SoundLocalizationTimeS** | `AudioReactionLogger` | En tu AudioSource 3D | ¡Es Automático! Detecta cuando empieza a sonar. |
| **AidUsage** | `AidProviderLogger` | En NPCs consejeros o UI | Llama a `RecordAidUsed()` cuando se pulse el botón de pista. |

## Resumen Técnico

Estos componentes son simplemente "Envolturas Visuales" (Wrappers) sobre nuestra API interna `LogAPI.cs`. Su único trabajo es exponer UnityEvents y campos en el Inspector para invocar las llamadas a MongoDB en el momento exacto, enviando los `"event_roles"` correctos (`action_success`, `navigation_error`, etc.) que espera el script `metrics.py`.
