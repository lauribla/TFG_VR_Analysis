using UnityEngine;
using UnityEngine.UI;
using VRLogger;

namespace VRLogger.Components
{
    /// <summary>
    /// Intercepta transparentemente las interacciones con elementos de la Interfaz de Usuario de Unity (Canvas 2D o VR).
    /// Se acopla al botón existente añadiendo un listener pasivo. No interrumpe la lógica que el desarrollador haya
    /// preconfigurado en el Inspector de Unity EventSystem del botón.
    /// Sirve para: Medir errores de navegación en menús, contabilizar selecciones o recolectar respuestas de un formulario.
    /// </summary>
    public class UIActionInterceptorLogger : MonoBehaviour
    {
        [Header("Configuración del Log UI")]
        [Tooltip("Id semántico de la acción (ej: Menu_Opcion_Audio, Contestar_A_Cuestionario).")]
        public string actionId;

        [Tooltip("¿Pinchar aquí se considera formalmente un error de navegación o respuesta incorrecta en esta prueba?")]
        public bool isErrorContext = false;
        
        [Tooltip("Tipo de control. Rellenado automáticamente por defecto basado en la clase detectada.")]
        [SerializeField] private string controlType;

        private void Awake()
        {
            if (GetComponent<Selectable>() == null)
            {
                Debug.LogWarning($"[UIActionInterceptorLogger] ⚠️ {gameObject.name} no tiene un componente interactuable (Button, Toggle...). Este logger se quedará huérfano y no detectará clicks automáticos.");
            }

            if (string.IsNullOrEmpty(actionId))
            {
                actionId = gameObject.name;
            }

            // Detección del tipo de elemento UI para añadir el listener interceptor adecuado.
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                controlType = "Button";
                btn.onClick.AddListener(OnUIActionInvoked);
                return;
            }

            Toggle toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                controlType = "Toggle";
                toggle.onValueChanged.AddListener((val) => OnUIActionInvoked());
                return;
            }

            Slider slider = GetComponent<Slider>();
            if (slider != null)
            {
                controlType = "Slider";
                // Usamos un simple hook al cambio de valor, aunque cuidado con los sliders, 
                // pueden spanear muchos logs. Sería ideal limitarlo al dejar el slider si hay OnPointerUp/EndDrag.
                // Lo dejamos para interacciones directas discretas.
                slider.onValueChanged.AddListener((val) => OnUIActionInvoked()); 
                return;
            }

            Dropdown dropdown = GetComponent<Dropdown>();
            if (dropdown != null)
            {
                controlType = "Dropdown";
                dropdown.onValueChanged.AddListener((val) => OnUIActionInvoked());
                return;
            }

            controlType = "UnknownSelectable";
        }

        private void OnUIActionInvoked()
        {
            string eventNameToLog = isErrorContext ? "ui_error" : "UI_INTERACTION";

            LoggerService.LogEvent(
                eventType: "metrics_ui",
                eventName: eventNameToLog,
                eventValue: new { 
                    actionId = this.actionId, 
                    isError = this.isErrorContext,
                    controlType = this.controlType
                },
                eventContext: null
            );
        }
    }
}
