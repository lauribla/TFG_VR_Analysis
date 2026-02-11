using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRLogger.UI
{
    public class TimerUILoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            GameObject loader = new GameObject("TimerUILoader");
            loader.AddComponent<TimerUILoader>();
        }

        private GameObject canvasObj;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI participantText;
        private TextMeshProUGUI nextText;
        private CanvasGroup canvasGroup;

        void Start()
        {
            CreateTimerUI();
        }

        void Update()
        {
            if (ParticipantFlowController.Instance == null) return;
            if (canvasGroup == null) return;

            // Timer acts based on flow controller state only
            if (ParticipantFlowController.Instance.GetEndCondition() != "timer")
            {
                canvasGroup.alpha = 0;
                return;
            }

            // Only show if running
            if (!ParticipantFlowController.Instance.IsRunning())
            {
                canvasGroup.alpha = 0;
                return;
            }

            canvasGroup.alpha = 1;

            float time = ParticipantFlowController.Instance.GetTimeRemaining();
            string curr = ParticipantFlowController.Instance.GetCurrentParticipant();
            string next = ParticipantFlowController.Instance.GetNextParticipant();

            // Format time 00:00
            int min = Mathf.FloorToInt(time / 60);
            int sec = Mathf.FloorToInt(time % 60);
            
            // Check Cooldown
            if (ParticipantFlowController.Instance.IsCooldown())
            {
                 timerText.color = Color.yellow;
                 timerText.text = $"{min:00}:{sec:00}";
                 participantText.text = $"<color=yellow>WAITING FOR: {curr}</color>";
                 nextText.text = "PREPARE NEXT PARTICIPANT";
            }
            else
            {
                // Standard Running Mode
                if (time < 5) timerText.color = Color.red;
                else timerText.color = Color.white;

                timerText.text = $"{min:00}:{sec:00}";
                participantText.text = $"CURRENT: <color=yellow>{curr}</color>";
                nextText.text = $"NEXT: <color=grey>{next}</color>";
            }
        }

        void CreateTimerUI()
        {
            // 1. Canvas
            canvasObj = new GameObject("TimerHUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // Behind config (999) but above game
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // Hide by default
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 2. Panel Top-Center
            GameObject panelObj = new GameObject("HUDPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.sizeDelta = new Vector2(400, 150);
            panelRect.anchoredPosition = new Vector2(0, -20);

            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black

            // 3. Layout
            VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 5;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0];

            // 4. Texts
            timerText = CreateText(panelObj, "00:00", font, 40, true);
            participantText = CreateText(panelObj, "CURRENT: -", font, 24, false);
            nextText = CreateText(panelObj, "NEXT: -", font, 20, false);
        }

        TextMeshProUGUI CreateText(GameObject parent, string defaultVal, TMP_FontAsset font, float size, bool bold)
        {
            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(parent.transform, false);
            TextMeshProUGUI txt = tObj.AddComponent<TextMeshProUGUI>();
            txt.text = defaultVal;
            txt.font = font;
            txt.fontSize = size;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            if (bold) txt.fontStyle = FontStyles.Bold;
            return txt;
        }
    }
}
