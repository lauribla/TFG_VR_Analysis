using UnityEngine;

namespace VRLogger
{
    public class VRTrackingManager : MonoBehaviour
    {
        [Header("General Config")]
        public string userId = "U001";
        public string sessionId = "S001";
        public string mongoConnection = "mongodb://localhost:27017";
        public string dbName = "VRLogsDB";
        public string collectionName = "events";

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

        void Start()
        {
            // Inicializar logger
            LoggerService.Init(mongoConnection, dbName, collectionName, userId);

            // Log inicio de sesión
            _ = LogAPI.LogSessionStart(sessionId);

            // Activar módulos
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
                var left = leftFoot.gameObject.AddComponent<FootTracker>();
                left.footName = "left";
                var right = rightFoot.gameObject.AddComponent<FootTracker>();
                right.footName = "right";
            }

            if (useHandTracker)
            {
                var left = leftHand.gameObject.AddComponent<HandTracker>();
                left.handName = "left";
                var right = rightHand.gameObject.AddComponent<HandTracker>();
                right.handName = "right";
            }

            if (useRaycastLogger)
            {
                gameObject.AddComponent<RaycastLogger>();
            }

            if (useCollisionLogger)
            {
                gameObject.AddComponent<CollisionLogger>();
            }
        }

        void OnApplicationQuit()
        {
            _ = LogAPI.LogSessionEnd(sessionId);
        }
    }
}
