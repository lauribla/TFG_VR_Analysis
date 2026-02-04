using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace VRLogger.UI
{
    public class ConfigUILoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            GameObject loader = new GameObject("ConfigUILoader");
            loader.AddComponent<ConfigUILoader>();
        }

        void Start()
        {
            CreateConfigUI();
        }

        void CreateConfigUI()
        {
            // 1. Event System (Create only if needed)
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // 2. Canvas
            GameObject canvasObj = new GameObject("VRConfigCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Panel (Container)
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900, 700); // 900x700 Fixed Center
            panelRect.anchoredPosition = Vector2.zero;

            ConfigUI configUI = panelObj.AddComponent<ConfigUI>();

            // 4. Header (Wider to prevent wrapping)
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0];

            GameObject headerObj = new GameObject("HeaderContainer");
            headerObj.transform.SetParent(panelObj.transform, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1); 
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 100); 
            headerRect.anchoredPosition = new Vector2(0, 0);

            CreateText(headerObj, "CONFIGURACIÓN COMPLETA", font, 36);

            // 5. Scroll View
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0); 
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 100); // Space for button
            scrollRect.offsetMax = new Vector2(-20, -100); // Space for header

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
            viewRect.pivot = new Vector2(0, 1);
            viewRect.sizeDelta = Vector2.zero;
            
            Image viewImg = viewport.AddComponent<Image>();
            viewImg.color = new Color(0,0,0,0.01f);
            Mask viewMask = viewport.AddComponent<Mask>();
            viewMask.showMaskGraphic = false;

            scroll.viewport = viewRect;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0); 

            VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = 10;
            contentLayout.padding = new RectOffset(40, 40, 20, 120); 

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // 6. MAP CONFIG FIELDS
            JObject currentConfig = ExperimentConfig.Instance.GetConfig();
            if (currentConfig != null)
            {
                // SESSION
                CreateSectionHeader(contentObj, "SESSION", font);
                AddStringInput(contentObj, "session.session_name", currentConfig, configUI, font);
                AddStringInput(contentObj, "session.group_name", currentConfig, configUI, font);
                AddStringInput(contentObj, "session.independent_variable", currentConfig, configUI, font);
                AddNumberInput(contentObj, "session.turn_duration_seconds", currentConfig, configUI, font);

                // PARTICIPANTS
                CreateSectionHeader(contentObj, "PARTICIPANTS", font);
                AddNumberInput(contentObj, "participants.count", currentConfig, configUI, font);
                
                if(currentConfig["participants"] is JObject p && p["order"] is JArray arr && arr.Count > 0)
                {
                    AddStringInput(contentObj, "participants.order[0]", currentConfig, configUI, font);
                }

                // EXPERIMENT SELECTION
                CreateSectionHeader(contentObj, "EXPERIMENT SELECTION", font);
                AddStringInput(contentObj, "experiment_selection.experiment_id", currentConfig, configUI, font);
                AddStringInput(contentObj, "experiment_selection.formula_profile", currentConfig, configUI, font);

                // MODULES
                CreateSectionHeader(contentObj, "MODULES", font);
                if (currentConfig["modules"] is JObject modules)
                {
                    foreach (var prop in modules.Properties())
                    {
                        AddToggleInput(contentObj, "modules." + prop.Name, currentConfig, configUI, font);
                    }
                }

                // FLOW
                CreateSectionHeader(contentObj, "FLOW", font);

                // Virtual Toggle Logic: Convert "end_condition" string to boolean for UI
                string endCond = (string)currentConfig["participant_flow"]["end_condition"];
                currentConfig["participant_flow"]["use_timer"] = (endCond == "timer");

                AddToggleInput(contentObj, "participant_flow.use_timer", currentConfig, configUI, font);
            }

            // 7. Start Button
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(panelObj.transform, false);
            RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(1, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.sizeDelta = new Vector2(0, 90);
            btnRect.anchoredPosition = Vector2.zero;

            // Widen button to fit "GUARDAR Y COMENZAR"
            GameObject btnObj = CreateButton(btnContainer, "GUARDAR Y COMENZAR", font);
            configUI.startButton = btnObj.GetComponent<Button>();

            // Note: Camera controller is found dynamically in ConfigUI using ICameraController interface
        }

        // --- Helpers ---

        void CreateSectionHeader(GameObject parent, string title, TMP_FontAsset font)
        {
            GameObject txtObj = new GameObject("SectionHeader");
            txtObj.transform.SetParent(parent.transform, false);
            LayoutElement le = txtObj.AddComponent<LayoutElement>();
            le.minHeight = 55; le.preferredHeight = 55;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = title;
            txt.font = font;
            txt.fontSize = 28;
            txt.alignment = TextAlignmentOptions.BottomLeft;
            txt.color = new Color(0.4f, 0.8f, 1f);
            txt.fontStyle = FontStyles.Bold;
            txt.margin = new Vector4(0, 10, 0, 0);
        }

        void AddStringInput(GameObject parent, string jsonPath, JObject root, ConfigUI ui, TMP_FontAsset font)
        {
             string val = GetValueByPath(root, jsonPath);
             var input = CreateLabeledInput(parent, jsonPath, val, font);
             ui.RegisterInput(jsonPath, input);
        }

        void AddNumberInput(GameObject parent, string jsonPath, JObject root, ConfigUI ui, TMP_FontAsset font)
        {
             string val = GetValueByPath(root, jsonPath);
             var input = CreateLabeledInput(parent, jsonPath, val, font);
             input.contentType = TMP_InputField.ContentType.DecimalNumber;
             ui.RegisterInput(jsonPath, input);
        }

        void AddToggleInput(GameObject parent, string jsonPath, JObject root, ConfigUI ui, TMP_FontAsset font)
        {
            bool val = GetBoolByPath(root, jsonPath);

            GameObject container = new GameObject("Toggle_" + jsonPath);
            container.transform.SetParent(parent.transform, false);
            LayoutElement le = container.AddComponent<LayoutElement>();
            le.minHeight = 45; le.preferredHeight = 45;

            HorizontalLayoutGroup hg = container.AddComponent<HorizontalLayoutGroup>();
            hg.childControlWidth = false;
            hg.childForceExpandWidth = false;
            hg.spacing = 30;
            hg.childAlignment = TextAnchor.MiddleRight;

            // Label
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(container.transform, false);
            LayoutElement leTxt = txtObj.AddComponent<LayoutElement>();
            leTxt.minWidth = 550; leTxt.preferredWidth = 550; // Fit inside 900 without crowding

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = jsonPath;
            txt.font = font;
            txt.fontSize = 18;
            txt.alignment = TextAlignmentOptions.MidlineRight;
            txt.color = new Color(0.9f, 0.9f, 0.9f);

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(container.transform, false);
            LayoutElement leTog = toggleObj.AddComponent<LayoutElement>();
            leTog.minWidth = 32; leTog.preferredWidth = 32;
            leTog.minHeight = 32; leTog.preferredHeight = 32;

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            Image bg = toggleObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f);

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(toggleObj.transform, false);
            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.offsetMin = Vector2.zero; checkRect.offsetMax = Vector2.zero;

            Image checkImg = checkObj.AddComponent<Image>();
            checkImg.color = Color.green;

            toggle.graphic = checkImg;
            toggle.targetGraphic = bg;
            toggle.isOn = val;

            ui.RegisterToggle(jsonPath, toggle);
        }

        string GetValueByPath(JToken root, string path)
        {
            try {
                var token = root.SelectToken(path);
                return token != null ? token.ToString() : "";
            } catch { return ""; }
        }

        bool GetBoolByPath(JToken root, string path)
        {
            try {
                var token = root.SelectToken(path);
                return token != null && (bool)token;
            } catch { return false; }
        }

        // --- Basic UI Creation ---

        TMP_InputField CreateLabeledInput(GameObject parent, string labelText, string defaultVal, TMP_FontAsset font)
        {
            GameObject container = new GameObject("InputGroup_" + labelText);
            container.transform.SetParent(parent.transform, false);
            LayoutElement layout = container.AddComponent<LayoutElement>();
            layout.minHeight = 50;
            layout.preferredHeight = 50;

            HorizontalLayoutGroup hg = container.AddComponent<HorizontalLayoutGroup>();
            hg.childControlWidth = true; hg.childForceExpandWidth = false;
            hg.spacing = 30;
            hg.childAlignment = TextAnchor.MiddleLeft;

            // Label
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(container.transform, false);
            LayoutElement txtLayout = txtObj.AddComponent<LayoutElement>();
            txtLayout.minWidth = 400; txtLayout.preferredWidth = 400;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = labelText;
            txt.font = font;
            txt.fontSize = 18;
            txt.alignment = TextAlignmentOptions.MidlineRight;
            txt.color = new Color(0.8f, 0.8f, 0.8f);

            // Input
            GameObject inputObj = new GameObject("Input_" + labelText);
            inputObj.transform.SetParent(container.transform, false);
            LayoutElement inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minWidth = 350; inputLayout.preferredWidth = 350;
            inputLayout.minHeight = 40;

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();

            // Show Cursor (Caret)
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.caretWidth = 2;

            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform areaRect = textArea.AddComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10, 0); areaRect.offsetMax = new Vector2(-10, 0);

            GameObject textObj = new GameObject("InputText");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.font = font;
            textComp.fontSize = 18;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.MidlineLeft;

            input.textViewport = areaRect;
            input.textComponent = textComp;
            input.text = defaultVal;

            return input;
        }

        void CreateText(GameObject parent, string content, TMP_FontAsset font, float size)
        {
            GameObject txtObj = new GameObject("LabelTitle");
            txtObj.transform.SetParent(parent.transform, false);
            LayoutElement layout = txtObj.AddComponent<LayoutElement>();
            layout.minHeight = size + 10;
            layout.preferredHeight = size + 10;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = size;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(1f, 1f, 0.4f);
        }

        GameObject CreateButton(GameObject parent, string label, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject("ButtonStart");
            btnObj.transform.SetParent(parent.transform, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 60); // Wider Button

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("BtnText");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = txtObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.black;
            txt.fontStyle = FontStyles.Bold;

            return btnObj;
        }
    }
}
