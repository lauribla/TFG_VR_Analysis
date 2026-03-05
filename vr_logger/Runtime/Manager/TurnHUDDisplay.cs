using UnityEngine;
using UnityEngine.UI;

namespace VRLogger
{
    /// <summary>
    /// TurnHUDDisplay — Componente opcional de UI para mostrar información de turnos.
    /// 
    /// INSTRUCCIONES DE USO:
    ///   1. Añade este script a cualquier GameObject vacío en tu escena.
    ///   2. Entra en Play Mode — el HUD aparecerá automáticamente flotando frente a la cámara.
    ///   3. Para ocultarlo, desactiva el GameObject o desmarca el componente.
    ///
    /// COMPATIBILIDAD: Funciona en escritorio y en VR (Canvas World Space, compatible con XR).
    /// SEGURIDAD: Si ParticipantFlowController no está presente, el componente no hace nada.
    /// </summary>
    public class TurnHUDDisplay : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────
        // Inspector Settings
        // ──────────────────────────────────────────────────────────────────
        [Header("Visibility")]
        [Tooltip("Mostrar HUD cuando el modo es 'turns'")]
        public bool showInTurnsMode = true;
        [Tooltip("Mostrar HUD cuando el modo es 'manual' / GM")]
        public bool showInGMMode = true;

        [Header("Camera")]
        [Tooltip("Arrastra aquí la cámara que debe seguir el HUD. Tiene prioridad sobre Camera Name.")]
        public Camera cameraOverride;
        [Tooltip("Nombre exacto del GameObject de la cámara (ej: 'Main Camera', 'CenterEyeAnchor'). Se usa si Camera Override está vacío.")]
        public string cameraName = "";

        [Header("Position")]
        [Tooltip("Distancia frente a la cámara (metros). Recomendado: 1.5-2.5 en VR")]
        public float distanceFromCamera = 2.0f;
        [Tooltip("Offset vertical respecto al centro de la cámara")]
        public float verticalOffset = -0.3f;

        [Header("Appearance")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.78f);
        public Color textColor       = Color.white;
        public Color cooldownColor   = new Color(1f, 0.6f, 0f, 1f);   // naranja
        public Color pausedColor     = new Color(0.9f, 0.9f, 0.2f, 1f); // amarillo
        public Color doneColor       = new Color(0.4f, 1f, 0.5f, 1f);  // verde claro
        [Range(8, 24)]
        public int fontSize = 14;

        // ──────────────────────────────────────────────────────────────────
        // Internal references (built at runtime, no prefab dependency)
        // ──────────────────────────────────────────────────────────────────
        private Canvas      _canvas;
        private CanvasGroup _canvasGroup;
        private Text        _mainText;
        private Image       _bgImage;

        private Camera      _cam;
        private bool        _uiBuilt = false;

        // ──────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────
        void Start()
        {
            // Use override if assigned, otherwise try to find automatically
            _cam = ResolveCamera();

            BuildUI();
        }

        void Update()
        {
            if (!_uiBuilt) return;

            // If camera still not found (spawned dynamically), keep retrying
            if (_cam == null)
            {
                _cam = ResolveCamera();
                if (_cam == null) return; // still not ready, wait next frame
            }

            UpdatePosition();
            UpdateContent();
        }

        // ──────────────────────────────────────────────────────────────────
        // Camera Resolution: override → name → MainCamera tag → any camera
        // ──────────────────────────────────────────────────────────────────
        private Camera ResolveCamera()
        {
            if (cameraOverride != null) return cameraOverride;
            if (!string.IsNullOrEmpty(cameraName))
            {
                GameObject go = GameObject.Find(cameraName);
                if (go != null) return go.GetComponent<Camera>();
            }
            return Camera.main ?? FindFirstObjectByType<Camera>();
        }

        // ──────────────────────────────────────────────────────────────────
        // UI Construction (all by code — no assets needed)
        // ──────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // --- Canvas ---
            GameObject canvasGO = new GameObject("TurnHUD_Canvas");
            canvasGO.transform.SetParent(this.transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;       // no bloquea raycast del juego
            _canvasGroup.blocksRaycasts = false;

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(340f, 160f);

            // Scale for world-space legibility in VR
            float scaleFactor = 0.003f;
            canvasGO.transform.localScale = Vector3.one * scaleFactor;

            // --- Background panel ---
            GameObject bgGO = new GameObject("TurnHUD_BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            _bgImage = bgGO.AddComponent<Image>();
            _bgImage.color = backgroundColor;

            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Rounded look via a simple sprite (optional, falls back to solid if null)
            _bgImage.raycastTarget = false;

            // --- Text ---
            GameObject textGO = new GameObject("TurnHUD_Text");
            textGO.transform.SetParent(canvasGO.transform, false);
            _mainText = textGO.AddComponent<Text>();
            _mainText.raycastTarget = false;
            _mainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _mainText.fontSize = fontSize;
            _mainText.color = textColor;
            _mainText.alignment = TextAnchor.MiddleLeft;
            _mainText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _mainText.verticalOverflow   = VerticalWrapMode.Overflow;

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(14f, 10f);
            textRT.offsetMax = new Vector2(-14f, -10f);

            _uiBuilt = true;
        }

        // ──────────────────────────────────────────────────────────────────
        // Position: Float in front of camera (World Space, VR-compatible)
        // ──────────────────────────────────────────────────────────────────
        private void UpdatePosition()
        {
            if (_cam == null) return;

            Transform camT = _cam.transform;
            Vector3 targetPos = camT.position
                              + camT.forward * distanceFromCamera
                              + camT.up      * verticalOffset;

            _canvas.transform.position = targetPos;
            _canvas.transform.rotation = Quaternion.LookRotation(
                _canvas.transform.position - camT.position
            );
        }

        // ──────────────────────────────────────────────────────────────────
        // Content: read-only access to ParticipantFlowController
        // ──────────────────────────────────────────────────────────────────
        private void UpdateContent()
        {
            var pfc = ParticipantFlowController.Instance;

            // Fail-safe: if the controller doesn't exist, hide and do nothing
            if (pfc == null)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            string mode = ExperimentConfig.Instance?.GetConfig()?
                          ["participant_flow"]?["mode"]?.ToString() ?? "turns";

            // Visibility based on mode
            bool shouldShow = (mode == "turns"  && showInTurnsMode)
                           || (mode == "manual" && showInGMMode);

            if (!shouldShow)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            _canvasGroup.alpha = 1f;

            // ── Build display string ──────────────────────────────────────
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Color panelColor = backgroundColor;

            if (!pfc.IsRunning())
            {
                sb.AppendLine("  VRLogger — Experimento");
                sb.AppendLine("  Esperando inicio...");
                panelColor = backgroundColor;
            }
            else if (pfc.IsPaused)
            {
                sb.AppendLine("  ⏸  PAUSADO");
                sb.AppendLine($"  Participante: {pfc.GetCurrentParticipant()}");
                panelColor = pausedColor;
                panelColor.a = backgroundColor.a;
            }
            else if (pfc.IsCooldown())
            {
                float cd = Mathf.Max(0f, pfc.GetTimeRemaining());
                sb.AppendLine("  🔄  COOLDOWN");
                sb.AppendLine($"  Siguiente:  {pfc.GetNextParticipant()}");
                sb.AppendLine($"  Tiempo:     {cd:F0}s");
                panelColor = cooldownColor;
                panelColor.a = backgroundColor.a;
            }
            else
            {
                // Active turn
                string current = pfc.GetCurrentParticipant();
                string next    = pfc.GetNextParticipant();
                string endCond = pfc.GetEndCondition();

                sb.AppendLine($"  👤  Turno: {current}");

                if (mode == "turns")
                {
                    if (endCond == "timer")
                    {
                        float t = Mathf.Max(0f, pfc.GetTimeRemaining());
                        int  mm = Mathf.FloorToInt(t / 60f);
                        int  ss = Mathf.FloorToInt(t % 60f);
                        sb.AppendLine($"  ⏱   Tiempo: {mm:00}:{ss:00}");
                    }
                    else
                    {
                        sb.AppendLine("  ⏱   Fin: GM manual");
                    }

                    if (next == "END")
                        sb.AppendLine("  ▶   Siguiente: último turno");
                    else
                        sb.AppendLine($"  ▶   Siguiente: {next}");
                }
                else // manual / GM
                {
                    sb.AppendLine("  🎮  Modo: GM Manual");
                    sb.AppendLine($"  ▶   Siguiente: {next}");
                }

                panelColor = backgroundColor;
            }

            _mainText.text = sb.ToString();
            _bgImage.color = panelColor;
        }

        // ──────────────────────────────────────────────────────────────────
        // Cleanup
        // ──────────────────────────────────────────────────────────────────
        void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
