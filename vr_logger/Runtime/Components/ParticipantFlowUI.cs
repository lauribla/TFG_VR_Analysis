using UnityEngine;
using UnityEngine.UI; // Usamos UI estándar para que funcione en cualquier proyecto sin depender de TextMeshPro inicialmente

namespace VRLogger.Components
{
    /// <summary>
    /// Componente UI no invasivo para mostrar el flujo de participantes.
    /// Puedes arrastrar este script a cualquier Canvas u objeto en tu escena VR, 
    /// y enlazar los Textos (UI) para mostrar quién juega ahora, quién después y el tiempo restante.
    /// </summary>
    public class ParticipantFlowUI : MonoBehaviour
    {
        [Header("Referencias a Textos UI (Opcionales)")]
        [Tooltip("Texto para mostrar el ID del participante actual")]
        public Text currentParticipantText;
        
        [Tooltip("Texto para mostrar el ID del siguiente participante")]
        public Text nextParticipantText;
        
        [Tooltip("Texto para mostrar el tiempo restante (mm:ss)")]
        public Text timeRemainingText;
        
        [Tooltip("Texto para mostrar el estado actual (Esperando, En Curso, Cooldown, Finalizado)")]
        public Text statusText;

        [Header("Textos Personalizables")]
        public string waitingText = "ESPERANDO INICIO";
        public string playingText = "TURNO EN CURSO";
        public string cooldownText = "PREPARANDO SIGUIENTE...";
        public string pausedText = "PAUSADO";
        public string endedText = "EXPERIMENTO FINALIZADO";

        void Update()
        {
            // Si el GameManager/Logger no está en la escena, no hacemos nada que rompa
            if (ParticipantFlowController.Instance == null)
            {
                if (statusText != null) statusText.text = "VR Logger No Inicializado";
                if (currentParticipantText != null) currentParticipantText.text = "-";
                if (nextParticipantText != null) nextParticipantText.text = "-";
                if (timeRemainingText != null) timeRemainingText.text = "--:--";
                return;
            }

            var flow = ParticipantFlowController.Instance;

            // 1. Actualizar Participante Actual
            if (currentParticipantText != null)
            {
                currentParticipantText.text = flow.GetCurrentParticipant();
            }

            // 2. Actualizar Siguiente Participante
            if (nextParticipantText != null)
            {
                nextParticipantText.text = flow.GetNextParticipant();
            }

            // 3. Lógica de Estado y Tiempo
            if (!flow.IsRunning())
            {
                // Si no hay participantes o el índice llegó al final, posiblemente terminó o no ha empezado
                if (flow.GetCurrentParticipant() == "-" || flow.GetCurrentParticipant() == "O") // "O" is unlikely but checking for empty
                {
                   if (statusText != null) statusText.text = endedText;
                }
                else
                {
                   if (statusText != null) statusText.text = waitingText;
                }
                
                if (timeRemainingText != null) timeRemainingText.text = "--:--";
            }
            else if (flow.IsPaused)
            {
                if (statusText != null) statusText.text = pausedText;
                UpdateTimeDisplay(flow.GetTimeRemaining());
            }
            else if (flow.IsCooldown())
            {
                if (statusText != null) statusText.text = cooldownText;
                UpdateTimeDisplay(flow.GetTimeRemaining());
            }
            else
            {
                if (statusText != null) statusText.text = playingText;
                
                if (flow.GetEndCondition() == "timer")
                {
                    UpdateTimeDisplay(flow.GetTimeRemaining());
                }
                else
                {
                    // Si el GM controla el pase de turno manual, el timer es irrelevante
                    if (timeRemainingText != null) timeRemainingText.text = "Control Manual";
                }
            }
        }

        private void UpdateTimeDisplay(float timeInSeconds)
        {
            if (timeRemainingText == null) return;
            
            if (timeInSeconds < 0) timeInSeconds = 0;
            
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            
            // Formato mm:ss
            timeRemainingText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
