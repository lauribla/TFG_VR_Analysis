using UnityEngine;

namespace VRLogger
{
    public class VRTrackingManager : MonoBehaviour
    {
        [Header("Modules")]
        public bool useGazeTracker = true;
        public bool useMovementTracker = true;
        public bool useFootTracker = false;   // tobillera
        public bool useHandTracker = false;   // manos
        public bool useRaycastLogger = false; // interacción raycast
        public bool useCollisionLogger = false;

        [Header("References")]
        public Camera vrCamera;
        public Transform playerTransform;
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform leftHand;
        public Transform rightHand;

        private string userId;
        private string sessionId;

        void Start()
        {
            // ✅ Obtener datos del UserSessionManager
            if (UserSessionManager.Instance == null)
            {
                Debug.LogError("[VRTrackingManager] No se encontró UserSessionManager en la escena.");
                return;
            }

            userId = UserSessionManager.Instance.GetUserId();
            sessionId = UserSessionManager.Instance.GetSessionId();

            // ✅ Inicializar logger con los mismos parámetros globales
            LoggerService.Init(
                UserSessionManager.Instance.connectionString,
                UserSessionManager.Instance.dbName,
                UserSessionManager.Instance.collectionName,
                userId
            );

            _ = LogAPI.LogSessionStart(sessionId);

            // ✅ Activar módulos de tracking
            if (useGazeTracker && vrCamera != null)
            {
                var gaze = vrCamera.gameObject.AddComponent<GazeTracker>();
                gaze.vrCamera = vrCamera;
            }

            if (useMovementTracker && playerTransform != null)
            {
                var move = playerTransform.gameObject.AddComponent<MovementTracker>();
                move.player = playerTransform;
            }

            if (useFootTracker)
            {
                if (leftFoot != null)
                {
                    var left = leftFoot.gameObject.AddComponent<FootTracker>();
                    left.footName = "left";
                }

                if (rightFoot != null)
                {
                    var right = rightFoot.gameObject.AddComponent<FootTracker>();
                    right.footName = "right";
                }
            }

            if (useHandTracker)
            {
                if (leftHand != null)
                {
                    var left = leftHand.gameObject.AddComponent<HandTracker>();
                    left.handName = "left";
                }

                if (rightHand != null)
                {
                    var right = rightHand.gameObject.AddComponent<HandTracker>();
                    right.handName = "right";
                }
            }

            if (useRaycastLogger)
            {
                gameObject.AddComponent<RaycastLogger>();
            }

            if (useCollisionLogger)
            {
                gameObject.AddComponent<CollisionLogger>();
            }

            Debug.Log($"[VRTrackingManager] Tracking initialized for user {userId} (session {sessionId}).");
        }

        void OnApplicationQuit()
        {
            _ = LogAPI.LogSessionEnd(sessionId);
        }
    }
}
