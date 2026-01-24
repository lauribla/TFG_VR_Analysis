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

    // IMPORTANT: session_start/session_end los emite UserSessionManager, no VRTrackingManager.
    // _ = LogAPI.LogSessionStart(sessionId);

    // --- Trackers (no duplicar si ya existen) ---
    if (useGazeTracker && vrCamera != null)
    {
        if (vrCamera.gameObject.GetComponent<GazeTracker>() == null)
            vrCamera.gameObject.AddComponent<GazeTracker>();
    }

    if (useMovementTracker && playerTransform != null)
    {
        if (playerTransform.gameObject.GetComponent<MovementTracker>() == null)
            playerTransform.gameObject.AddComponent<MovementTracker>();
    }

    if (useFootTracker)
    {
        if (leftFoot != null)
        {
            var ft = leftFoot.gameObject.GetComponent<FootTracker>();
            if (ft == null) ft = leftFoot.gameObject.AddComponent<FootTracker>();
            ft.footName = "left";
        }

        if (rightFoot != null)
        {
            var ft = rightFoot.gameObject.GetComponent<FootTracker>();
            if (ft == null) ft = rightFoot.gameObject.AddComponent<FootTracker>();
            ft.footName = "right";
        }
    }

    if (useHandTracker)
    {
        if (leftHand != null)
        {
            var ht = leftHand.gameObject.GetComponent<HandTracker>();
            if (ht == null) ht = leftHand.gameObject.AddComponent<HandTracker>();
            ht.handName = "left";
        }

        if (rightHand != null)
        {
            var ht = rightHand.gameObject.GetComponent<HandTracker>();
            if (ht == null) ht = rightHand.gameObject.AddComponent<HandTracker>();
            ht.handName = "right";
        }
    }

    if (useRaycastLogger)
    {
        if (gameObject.GetComponent<RaycastLogger>() == null)
            gameObject.AddComponent<RaycastLogger>();
    }

    if (useCollisionLogger)
    {
        if (gameObject.GetComponent<CollisionLogger>() == null)
            gameObject.AddComponent<CollisionLogger>();
    }

    Debug.Log($"[VRTracking] Tracking ON → {userId} / {sessionId}");
}

public void EndTracking()
{
    // IMPORTANT: aqui NO se manda session_end. Eso es responsabilidad de UserSessionManager.
    // _ = LogAPI.LogSessionEnd(UserSessionManager.Instance.GetSessionId());

    // Desactivar/eliminar componentes si existen. Totalmente seguro si no estan.
    SafeRemove<GazeTracker>(vrCamera != null ? vrCamera.gameObject : null);
    SafeRemove<MovementTracker>(playerTransform != null ? playerTransform.gameObject : null);

    SafeRemove<FootTracker>(leftFoot != null ? leftFoot.gameObject : null);
    SafeRemove<FootTracker>(rightFoot != null ? rightFoot.gameObject : null);

    SafeRemove<HandTracker>(leftHand != null ? leftHand.gameObject : null);
    SafeRemove<HandTracker>(rightHand != null ? rightHand.gameObject : null);

    // Loggers anadidos en este mismo objeto
    SafeRemove<RaycastLogger>(gameObject);
    SafeRemove<CollisionLogger>(gameObject);

    Debug.Log($"[VRTracking] Tracking OFF → {userId} / {sessionId}");
}

private void SafeRemove<T>(GameObject go) where T : Component
{
    if (go == null) return;

    // Puede haber mas de uno por errores previos: quitamos todos.
    var comps = go.GetComponents<T>();
    if (comps == null || comps.Length == 0) return;

    for (int i = 0; i < comps.Length; i++)
    {
        if (comps[i] != null)
            Destroy(comps[i]);
    }
}


    }
}
