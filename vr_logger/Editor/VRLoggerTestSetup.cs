using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRLogger.Components;

namespace VRLogger.EditorScripts
{
    public class VRLoggerTestSetup : EditorWindow
    {
        [MenuItem("VR Logger/Spawn Test Environment")]
        public static void SpawnTestEnvironment()
        {
            GameObject root = new GameObject("VRLogger_TestEnvironment");

            // Disable main camera to prefer our new player camera
            if (Camera.main != null)
            {
                Camera.main.gameObject.SetActive(false);
                Debug.Log("Cámara principal original desactivada temporalmente para usar la cámara del Player de pruebas.");
            }

            // Create a Floor so the player doesn't fall endlessly
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test_Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(50, 1, 50);
            SetColor(floor, new Color(0.2f, 0.2f, 0.2f));

            // 1. TaskZoneBoundaryLogger
            GameObject taskZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            taskZone.name = "Test_TaskZone";
            taskZone.transform.SetParent(root.transform);
            taskZone.transform.position = new Vector3(2, 1, 2);
            var taskCollider = taskZone.GetComponent<Collider>();
            taskCollider.isTrigger = true;
            var tzLogger = taskZone.AddComponent<TaskZoneBoundaryLogger>();
            tzLogger.taskId = "Test_Puzzle_01";
            tzLogger.validTriggerMask = ~0;

            GameObject taskSuccessZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            taskSuccessZone.name = "Test_TaskSuccessZone";
            taskSuccessZone.transform.SetParent(root.transform);
            taskSuccessZone.transform.position = new Vector3(4, 1, 2);
            var successCollider = taskSuccessZone.GetComponent<Collider>();
            successCollider.isTrigger = true;
            tzLogger.successExitZone = successCollider;
            // Configurar color para distinguirlos
            SetColor(taskZone, Color.yellow);
            SetColor(taskSuccessZone, Color.green);


            // 2. UIActionInterceptorLogger
            GameObject canvasObj = new GameObject("Test_Canvas");
            canvasObj.transform.SetParent(root.transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.transform.position = new Vector3(0, 2, 3);
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            GameObject buttonObj = new GameObject("Test_Button");
            buttonObj.transform.SetParent(canvasObj.transform);
            buttonObj.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            buttonObj.AddComponent<CanvasRenderer>();
            Image btnImage = buttonObj.AddComponent<Image>();
            btnImage.color = Color.blue;
            Button btn = buttonObj.AddComponent<Button>();
            var uiLogger = buttonObj.AddComponent<UIActionInterceptorLogger>();
            uiLogger.actionId = "Test_Menu_Button";

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(root.transform);
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }


            // 3. InertiaInactivityLogger
            GameObject playerMock = new GameObject("Test_PlayerMock");
            playerMock.transform.SetParent(root.transform);
            var inactivityLogger = playerMock.AddComponent<InertiaInactivityLogger>();
            inactivityLogger.inactivityThreshold_s = 3f;


            // 4. LifecycleReactionLogger
            GameObject enemyMock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemyMock.name = "Test_Target_Lifecycle";
            enemyMock.transform.SetParent(root.transform);
            enemyMock.transform.position = new Vector3(-2, 1, 2);
            SetColor(enemyMock, Color.red);
            var lifecycleLogger = enemyMock.AddComponent<LifecycleReactionLogger>();
            lifecycleLogger.targetId = "Enemy_Drone";


            // 5. NavigationErrorColliderLogger
            GameObject errorWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            errorWall.name = "Test_ErrorWall";
            errorWall.transform.SetParent(root.transform);
            errorWall.transform.position = new Vector3(-4, 1, 0);
            errorWall.transform.localScale = new Vector3(1, 2, 4);
            errorWall.layer = LayerMask.NameToLayer("UI"); // Usando UI como capa de ejemplo si no hay capas custom
            SetColor(errorWall, new Color(1, 0.5f, 0)); // Naranja

            GameObject playerColliderMock = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerColliderMock.name = "Test_PlayerColliderMock (Move with WASD)";
            playerColliderMock.transform.SetParent(root.transform);
            playerColliderMock.transform.position = new Vector3(0, 1, 0);
            
            var rb = playerColliderMock.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Para que no se caiga de lado al chocar

            var wasd = playerColliderMock.AddComponent<SimpleWASDPlayer>();
            wasd.speed = 4f;

            // Attach a camera to the player so they can see from it
            GameObject playerCamera = new GameObject("PlayerCam");
            playerCamera.transform.SetParent(playerColliderMock.transform);
            playerCamera.transform.localPosition = new Vector3(0, 0.6f, 0); // Eye level
            playerCamera.AddComponent<Camera>();

            var navErrorLogger = playerColliderMock.AddComponent<NavigationErrorColliderLogger>();
            navErrorLogger.penaltyId = "Wall_Hit";
            navErrorLogger.allowedErrorMask = 1 << errorWall.layer;
            

            // 6. CheckpointProgressionLogger
            GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            checkpoint.name = "Test_Checkpoint";
            checkpoint.transform.SetParent(root.transform);
            checkpoint.transform.position = new Vector3(0, 1, -3);
            checkpoint.GetComponent<Collider>().isTrigger = true;
            SetColor(checkpoint, Color.cyan);
            var checkpointLogger = checkpoint.AddComponent<CheckpointProgressionLogger>();
            checkpointLogger.checkpointName = "Level_1_Midpoint";
            checkpointLogger.progressValue = 50f;
            checkpointLogger.playerMask = ~0; // Todo interactúa con el checkpoint en el test


            // 7. AidInteractionLogger
            GameObject aidPanel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            aidPanel.name = "Test_AidPanel";
            aidPanel.transform.SetParent(root.transform);
            aidPanel.transform.position = new Vector3(2, 2, -2);
            SetColor(aidPanel, Color.magenta);
            var aidLogger = aidPanel.AddComponent<AidInteractionLogger>();
            aidLogger.aidId = "Tutorial_Sign";
            aidLogger.recognitionTimeThreshold = 1.0f;


            // 8. SemanticZoneLogger
            GameObject semanticZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            semanticZone.name = "Test_SemanticZone";
            semanticZone.transform.SetParent(root.transform);
            semanticZone.transform.position = new Vector3(-2, 1, -2);
            semanticZone.GetComponent<Collider>().isTrigger = true;
            SetColor(semanticZone, new Color(0.5f, 0, 1)); // Morado
            var semLogger = semanticZone.AddComponent<SemanticZoneLogger>();
            semLogger.zoneId = "Safe_Room";
            semLogger.validTriggerMask = ~0; // ¡SOLUCIÓN! Necesitaba la máscara -1 para detectarte

            // 8.1 DirectionalSemanticZoneLogger
            GameObject dirZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirZone.name = "Test_DirectionalSemanticZone";
            dirZone.transform.SetParent(root.transform);
            dirZone.transform.position = new Vector3(-6, 1, -2);
            dirZone.GetComponent<Collider>().isTrigger = true;
            SetColor(dirZone, new Color(0, 1, 0.5f)); // Verde azulado / turquesa
            var dirLogger = dirZone.AddComponent<DirectionalSemanticZoneLogger>();
            dirLogger.zoneId = "Crossroads_Test";
            dirLogger.validTriggerMask = ~0;
            // Configuramos una trampa de lados
            dirLogger.exitForwardZ = SemanticZoneType.Success;
            dirLogger.exitBackwardZ = SemanticZoneType.Backtrack;
            dirLogger.exitLeftX = SemanticZoneType.Fail;
            dirLogger.exitRightX = SemanticZoneType.Fail;

            // 9. EnvironmentBoundsMarker (4 corners for Convex Hull in Python)
            Vector3[] corners = new Vector3[] {
                new Vector3(-20, 0, -20),
                new Vector3(20, 0, -20),
                new Vector3(20, 0, 20),
                new Vector3(-20, 0, 20)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject boundsObj = new GameObject($"Test_EnvironmentBoundsMarker_Corner_{i}");
                boundsObj.transform.SetParent(root.transform);
                boundsObj.transform.position = corners[i];
                boundsObj.AddComponent<EnvironmentBoundsMarker>();
            }


            // Seleccionar en el editor para que el usuario lo vea
            Selection.activeGameObject = root;
            Debug.Log("VR Logger Test Environment creado temporalmente en la escena. ¡Recuerda añadir tu ExperimentConfig y VRTrackingManager a la escena!");
        }

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = new Material(renderer.sharedMaterial);
                mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }
    }
}
