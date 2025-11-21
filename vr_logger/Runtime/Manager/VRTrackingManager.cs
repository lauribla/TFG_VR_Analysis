using UnityEngine;

namespace VRLogger
{
    public class VRTrackingManager : MonoBehaviour
    {
        private bool useGazeTracker;
        private bool useMovementTracker;
        private bool useFootTracker;
        private bool useHandTracker;
        private bool useRaycastLogger;
        private bool useCollisionLogger;

        [Header("References")]
        public Camera vrCamera;
        public Transform playerTransform;
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform leftHand;
        public Transform rightHand;
        public static VRTrackingManager Instance;


        private string userId;
        private string sessionId;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void BeginTrackingForUser()
{
    if (UserSessionManager.Instance == null)
            {
                Debug.LogError("[VRTrackingManager] No se encontró UserSessionManager.");
                return;
            }

            // 1. Leer configuración desde JSON
            var cfg = ExperimentConfig.Instance.config.modules;

            useGazeTracker = cfg.useGazeTracker;
            useMovementTracker = cfg.useMovementTracker;
            useFootTracker = cfg.useFootTracker;
            useHandTracker = cfg.useHandTracker;
            useRaycastLogger = cfg.useRaycastLogger;
            useCollisionLogger = cfg.useCollisionLogger;

            // 2. Obtener datos del usuario
            userId = UserSessionManager.Instance.GetUserId();
            sessionId = UserSessionManager.Instance.GetSessionId();

            // 3. Inicializar Mongo
            LoggerService.Init(
                UserSessionManager.Instance.connectionString,
                UserSessionManager.Instance.dbName,
                UserSessionManager.Instance.collectionName,
                userId
            );

            // 4. Registrar inicio de tracking
            _ = LogAPI.LogSessionStart(sessionId);

            // 5. Activar módulos
            if (useGazeTracker && vrCamera != null)
                vrCamera.gameObject.AddComponent<GazeTracker>().vrCamera = vrCamera;

            if (useMovementTracker && playerTransform != null)
                playerTransform.gameObject.AddComponent<MovementTracker>().player = playerTransform;

            if (useFootTracker)
            {
                if (leftFoot != null)
                    leftFoot.gameObject.AddComponent<FootTracker>().footName = "left";
                if (rightFoot != null)
                    rightFoot.gameObject.AddComponent<FootTracker>().footName = "right";
            }

            if (useHandTracker)
            {
                if (leftHand != null)
                    leftHand.gameObject.AddComponent<HandTracker>().handName = "left";
                if (rightHand != null)
                    rightHand.gameObject.AddComponent<HandTracker>().handName = "right";
            }

            if (useRaycastLogger)
                gameObject.AddComponent<RaycastLogger>();

            if (useCollisionLogger)
                gameObject.AddComponent<CollisionLogger>();

            Debug.Log($"[VRTrackingManager] Config aplicado. User {userId} Session {sessionId}");
}

public void EndTracking()
{
    _ = LogAPI.LogSessionEnd(UserSessionManager.Instance.GetSessionId());
}


        void Start()
        {
            
        }

        void OnApplicationQuit()
        {
            _ = LogAPI.LogSessionEnd(sessionId);
        }
    }
}
