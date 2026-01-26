using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRLogger.UI
{
    public class GMHUDLoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            GameObject loader = new GameObject("GMHUDLoader");
            loader.AddComponent<GMHUDLoader>();
        }

        private GameObject canvasObj;
        private TextMeshProUGUI pauseText;
        private Image pauseBtnImage;
        private Button pauseBtn;
        private Button nextBtn;

        private Camera gmCamera;
        private Camera pcUiCamera;
        private Transform playerTransform;

        void Start()
        {
            CreateHUD();
            // Wait a frame for other systems? No, call immediately but be safe.
            SetupAsymmetricView();
        }

        private GameObject gmBackgroundObj;

        private float retryTimer = 0f;

        void Update()
        {
            if (ParticipantFlowController.Instance == null) return;
            
            // Lazy Init: If PC Camera isn't ready, try setup periodically
            if (pcUiCamera == null)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer > 1.0f) // Retry every 1s
                {
                    retryTimer = 0f;
                    SetupAsymmetricView();
                }
            }
            // Also retry finding Config Canvas if missed
            else if (configCanvasRef == null)
            {
                 HijackConfigCanvas();
            }

            // DEBUG: Toggle Views with 'M' (Mode)
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[GMHUD] Toggle Input Detected (M)");
                
                if (pcUiCamera == null)
                {
                    Debug.LogWarning("[GMHUD] 'M' pressed but System NOT Ready. Waiting for VR Camera...");
                    return;
                }

                bool isActive = !pcUiCamera.gameObject.activeSelf;
                pcUiCamera.gameObject.SetActive(isActive);
                
                if (canvasObj != null) 
                    canvasObj.SetActive(isActive);

                Debug.Log(isActive ? "[GMHUD] Showing GM View (Observer)" : "[GMHUD] Showing Player View (VR)");
            }

            // Update Pause Visual state
            bool isPaused = ParticipantFlowController.Instance.IsPaused;
            if (pauseText != null)
            {
                pauseText.text = isPaused ? "RESUME (P)" : "PAUSE (P)";
                pauseBtnImage.color = isPaused ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.4f, 0.4f);
            }
        }

        // Ensure GM view remains active after start
        private void EnsureGMViewActive()
        {
            if (gmCamera != null && !gmCamera.enabled)
            {
                gmCamera.enabled = true;
                Debug.Log("[GMHUD] Re-enabled GM Camera after start.");
            }
            if (pcUiCamera != null && !pcUiCamera.gameObject.activeSelf)
            {
                pcUiCamera.gameObject.SetActive(true);
                Debug.Log("[GMHUD] Re-enabled PC UI Camera after start.");
            }
            if (canvasObj != null && !canvasObj.activeSelf)
            {
                canvasObj.SetActive(true);
                Debug.Log("[GMHUD] Re-enabled GM HUD Canvas after start.");
            }
        }

        void SetupAsymmetricView()
        {
            Camera mainCam = null;
            if (VRTrackingManager.Instance != null && VRTrackingManager.Instance.vrCamera != null)
                mainCam = VRTrackingManager.Instance.vrCamera;

            if (mainCam == null)
                mainCam = Camera.main;

            // Fallback: Find ANY valid scene camera (excluding our own UI cameras)
            if (mainCam == null)
            {
                foreach (var cam in Camera.allCameras)
                {
                    if (cam != null && cam.isActiveAndEnabled && !cam.name.Contains("GM_") && !cam.name.Contains("PC_UI"))
                    {
                        mainCam = cam;
                        break;
                    }
                }
            }

            if (mainCam == null)
            {
                 Debug.LogWarning($"[GMHUD] Waiting for Source Camera... (Main={Camera.main})");
                return;
            }
            
            Debug.Log($"[GMHUD] SetupAsymmetricView using Source Camera: '{mainCam.name}'");

            // 1. CULL UI from VR Camera (So headset doesn't see buttons)
            mainCam.cullingMask = mainCam.cullingMask & ~(1 << LayerMask.NameToLayer("UI"));

            // 2. Create Render Texture (The "CCTV" Feed)
            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            rt.name = "GM_RenderTexture";

            // 3. Create GM Camera (CLONE)
            // Parent it to VR Camera so it moves 1:1 with head
            GameObject camObj = new GameObject("GM_Camera_Clone");
            camObj.transform.SetParent(mainCam.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            
            gmCamera = camObj.AddComponent<Camera>();
            gmCamera.CopyFrom(mainCam); // Copy settings (FOV, etc)
            gmCamera.targetTexture = rt;
            gmCamera.cullingMask = mainCam.cullingMask; // See what VR sees (minus UI)
            // Ensure Stereo is off for this clone
            gmCamera.stereoTargetEye = StereoTargetEyeMask.None;

            // 4. Create PC UI Camera (Renders only UI to the Monitor)
            GameObject uiCamObj = new GameObject("PC_UI_Camera");
            pcUiCamera = uiCamObj.AddComponent<Camera>();
            pcUiCamera.clearFlags = CameraClearFlags.SolidColor;
            pcUiCamera.backgroundColor = Color.black; // Background
            pcUiCamera.cullingMask = (1 << LayerMask.NameToLayer("UI")); // ONLY UI
            pcUiCamera.targetDisplay = 0; // Main Monitor
            pcUiCamera.depth = 100; // Render on top of everything else (VR mirror)

            // 5. Configure Canvas to use PC UI Camera
            Canvas c = canvasObj.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = pcUiCamera;
            c.planeDistance = 1; // Close to cam

            // IMPORTANT: Set GMControlCanvas (and children) to UI layer for pcUiCamera
            int uiLayer = LayerMask.NameToLayer("UI");
            SetLayerRecursively(canvasObj, uiLayer);

            // 6. Create Background RawImage (The Video Feed)
            // It sits IN the Canvas, so PC UI Camera renders it.
            GameObject bgObj = new GameObject("GM_Background_View");
            gmBackgroundObj = bgObj;
            bgObj.transform.SetParent(canvasObj.transform, false);
            bgObj.transform.SetAsFirstSibling(); // Behind buttons

            RectTransform rtRect = bgObj.AddComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero; rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = Vector2.zero; rtRect.offsetMax = Vector2.zero;

            RawImage rawImg = bgObj.AddComponent<RawImage>();
            rawImg.texture = rt;
            rawImg.color = Color.white;
            
            // 7. HIJACK Config Canvas - Retry Logic needed as it might spawn late
            HijackConfigCanvas();
        }

        private Canvas configCanvasRef;
        private void HijackConfigCanvas()
        {
            if (configCanvasRef != null) return; // Already done

            GameObject configObj = GameObject.Find("VRConfigCanvas");
            if (configObj == null)
            {
                // Try finding by type
                var loader = FindObjectOfType<ConfigUILoader>();
                if (loader != null)
                {
                    // It might be a child or created by it. Loader creates "VRConfigCanvas" at root.
                    // If loader exists but obj not found, maybe next frame.
                }
                return; 
            }

            // Ensure the Config Canvas and its children are on the UI layer
            int uiLayer = LayerMask.NameToLayer("UI");
            SetLayerRecursively(configObj, uiLayer);

            Canvas cc = configObj.GetComponent<Canvas>();
            if (cc != null && pcUiCamera != null)
            {
                cc.renderMode = RenderMode.ScreenSpaceCamera;
                cc.worldCamera = pcUiCamera;
                cc.sortingOrder = 999;
                cc.planeDistance = 0.5f; // In front of GM HUD
                configCanvasRef = cc;
                Debug.Log("[GMHUD] Successfully Hijacked Config Canvas for PC View.");
            }
        }

        // Helper to set layer on a GameObject and all its children
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        // Public method to ensure GM view components are active
        public void ActivateGMView()
        {
            if (gmCamera != null && !gmCamera.enabled)
            {
                gmCamera.enabled = true;
                Debug.Log("[GMHUD] GM Camera re-enabled.");
            }
            if (pcUiCamera != null && !pcUiCamera.gameObject.activeSelf)
            {
                pcUiCamera.gameObject.SetActive(true);
                Debug.Log("[GMHUD] PC UI Camera re-enabled.");
            }
            if (canvasObj != null && !canvasObj.activeSelf)
            {
                canvasObj.SetActive(true);
                Debug.Log("[GMHUD] HUD Canvas re-enabled.");
            }
        }

        void CreateHUD()
        {
            // 1. Canvas
            canvasObj = new GameObject("GMControlCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // Above game, below config (999)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // NOTE: We don't need a Panel parent for buttons if we want them floating, 
            // but sticking to the previous layout is fine. 
            // The Background RawImage is added to canvasObj directly in SetupGMCamera.

            // 2. Button Container (Bottom Right)
            GameObject panelObj = new GameObject("GMPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);
            panelRect.sizeDelta = new Vector2(300, 100);
            panelRect.anchoredPosition = new Vector2(-20, 20); // Padding

            // Layout
            HorizontalLayoutGroup hg = panelObj.AddComponent<HorizontalLayoutGroup>();
            hg.childControlWidth = true;
            hg.childForceExpandWidth = true;
            hg.spacing = 10;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0];

            // 3. Pause Button
            GameObject pBtnObj = CreateButton(panelObj, "PAUSE (P)", font, new Color(1f, 0.4f, 0.4f));
            pauseBtn = pBtnObj.GetComponent<Button>();
            pauseBtnImage = pBtnObj.GetComponent<Image>();
            pauseText = pBtnObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            
            pauseBtn.onClick.AddListener(() => {
                if (ParticipantFlowController.Instance != null)
                    ParticipantFlowController.Instance.TogglePause();
            });

            // 4. Next Button
            GameObject nBtnObj = CreateButton(panelObj, "NEXT (N)", font, new Color(0.4f, 0.6f, 1f));
            nextBtn = nBtnObj.GetComponent<Button>();
            
            nextBtn.onClick.AddListener(() => {
                if (ParticipantFlowController.Instance != null)
                {
                    // Check mode to warn user? For now just execute.
                    ParticipantFlowController.Instance.GM_NextParticipant();
                }
            });
        }

        GameObject CreateButton(GameObject parent, string label, TMP_FontAsset font, Color color)
        {
            GameObject btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(parent.transform, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = txtObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5,5); textRect.offsetMax = new Vector2(-5,-5);
            
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.black;
            txt.fontStyle = FontStyles.Bold;

            return btnObj;
        }
    }
}
