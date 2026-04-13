using UnityEngine;
using VRLogger;

namespace VRLogger.Components
{
    public class ValidationTestController : MonoBehaviour
    {
        private GUIStyle btnStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;

        void OnGUI()
        {
            if (btnStyle == null)
            {
                btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.fontSize = 18;
                
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 22;
                titleStyle.fontStyle = FontStyle.Bold;

                subtitleStyle = new GUIStyle(GUI.skin.label);
                subtitleStyle.fontSize = 18;
                subtitleStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 500), GUI.skin.box);
            GUILayout.Label("PANEL DE VALIDACIÓN TFG", titleStyle);
            GUILayout.Space(10);
            
            GUILayout.Label("1. FLUJO / USUARIOS", subtitleStyle);
            if (GUILayout.Button("Forzar ID 'TESTER_001'", btnStyle))
            {
                if (ExperimentConfig.Instance != null) {
                    ExperimentConfig.Instance.ManualParticipantName = "TESTER_001";
                    Debug.Log("Forzado: ManualParticipantName = TESTER_001. Borrar texto del Inspector si se quiere volver al Array de usuarios.");
                }
            }
            if (GUILayout.Button("Avanzar Participante", btnStyle))
            {
                if (ParticipantFlowController.Instance != null) {
                    ParticipantFlowController.Instance.GM_NextParticipant();
                    Debug.Log("Avanzando al siguiente participante...");
                }
            }
            
            GUILayout.Space(20);
            GUILayout.Label("2. CUSTOM EVENTS Y MAPPING", subtitleStyle);
            if (GUILayout.Button("Lanzar 'target_hit'", btnStyle))
            {
                LoggerService.LogEvent("metrics_custom", "target_hit", new { origen = "Tester Panel" });
                Debug.Log("Log enviado: target_hit");
            }
            if (GUILayout.Button("Lanzar Custom 'MiEventoCustom'", btnStyle))
            {
                LoggerService.LogEvent("metrics_custom", "MiEventoCustom", new { extra = "Para probar en Mapeo" });
                Debug.Log("Log enviado: MiEventoCustom");
            }

            GUILayout.Space(20);
            GUILayout.Label("NOTA:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label("Usa WASD para caminar con la cápsula\ny chocar contra las zonas (cubos) para\nprobar Trackers (Gaze, Move, Zones...).");
            
            GUILayout.EndArea();
        }
    }
}
