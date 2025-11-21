using UnityEngine;
using Newtonsoft.Json.Linq;

namespace VRLogger
{
    public class VRTrackingManager : MonoBehaviour
    {
        public static VRTrackingManager Instance;

        public Camera vrCamera;
        public Transform playerTransform;
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform leftHand;
        public Transform rightHand;

        private string userId;
        private string sessionId;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public void BeginTrackingForUser()
        {
            JObject cfg = ExperimentConfig.Instance.GetConfig();
            if (cfg == null)
            {
                Debug.LogError("[VRTracking] ❌ Config no cargado");
                return;
            }

            JObject modules = (JObject)cfg["modules"];

            bool useGazeTracker = (bool)modules["useGazeTracker"];
            bool useMovementTracker = (bool)modules["useMovementTracker"];
            bool useFootTracker = (bool)modules["useFootTracker"];
            bool useHandTracker = (bool)modules["useHandTracker"];
            bool useRaycastLogger = (bool)modules["useRaycastLogger"];
            bool useCollisionLogger = (bool)modules["useCollisionLogger"];

            userId = UserSessionManager.Instance.GetUserId();
            sessionId = UserSessionManager.Instance.GetSessionId();

            LoggerService.Init(
                UserSessionManager.Instance.connectionString,
                UserSessionManager.Instance.dbName,
                UserSessionManager.Instance.collectionName,
                userId
            );

            _ = LogAPI.LogSessionStart(sessionId);

            if (useGazeTracker && vrCamera != null)
                vrCamera.gameObject.AddComponent<GazeTracker>();

            if (useMovementTracker && playerTransform != null)
                playerTransform.gameObject.AddComponent<MovementTracker>();

            if (useFootTracker)
            {
                if (leftFoot) leftFoot.gameObject.AddComponent<FootTracker>().footName = "left";
                if (rightFoot) rightFoot.gameObject.AddComponent<FootTracker>().footName = "right";
            }

            if (useHandTracker)
            {
                if (leftHand) leftHand.gameObject.AddComponent<HandTracker>().handName = "left";
                if (rightHand) rightHand.gameObject.AddComponent<HandTracker>().handName = "right";
            }

            if (useRaycastLogger)
                gameObject.AddComponent<RaycastLogger>();

            if (useCollisionLogger)
                gameObject.AddComponent<CollisionLogger>();

            Debug.Log($"[VRTracking] Tracking ON → {userId} / {sessionId}");
        }

        public void EndTracking()
        {
            _ = LogAPI.LogSessionEnd(UserSessionManager.Instance.GetSessionId());
        }
    }
}
