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
    bool useEyeTracker = (bool?)modules["useEyeTracker"] ?? false; // Default false
    bool useMovementTracker = (bool)modules["useMovementTracker"];
    bool useFootTracker = (bool)modules["useFootTracker"];
    bool useHandTracker = (bool)modules["useHandTracker"];
    bool useRaycastLogger = (bool)modules["useRaycastLogger"];
    bool useCollisionLogger = (bool)modules["useCollisionLogger"];

    userId = UserSessionManager.Instance.GetUserId();
    sessionId = UserSessionManager.Instance.GetSessionId();

    // 1. Intentar buscar referencias dinámicamente si faltan
    TryFindReferences();

    // 2. Inicializar Logger
    LoggerService.Init(
        UserSessionManager.Instance.connectionString,
        UserSessionManager.Instance.dbName,
        UserSessionManager.Instance.collectionName,
        userId
    );

    // --- Trackers ---
    
    // Gaze Tracker
    if (useGazeTracker)
    {
        if (vrCamera != null)
        {
            var gt = vrCamera.gameObject.GetComponent<GazeTracker>();
            if (gt == null) gt = vrCamera.gameObject.AddComponent<GazeTracker>();
            
            if (gt != null)
            {
                gt.vrCamera = vrCamera; 
                gt.enabled = true;
            }
            else Debug.LogWarning("[VRTracking] ⚠️ Error al añadir GazeTracker (Script missing?)");
        }
        else Debug.LogWarning("[VRTracking] ⚠️ No se puede iniciar GazeTracker: vrCamera es null");
    }

    // Eye Tracker
    if (useEyeTracker)
    {
        if (vrCamera != null)
        {
            var et = vrCamera.gameObject.GetComponent<EyeTracker>();
            if (et == null) et = vrCamera.gameObject.AddComponent<EyeTracker>();
            
            if (et != null) et.enabled = true;
            else Debug.LogWarning("[VRTracking] ⚠️ Error al añadir EyeTracker (Script missing?)");
        }
        else Debug.LogWarning("[VRTracking] ⚠️ No se puede iniciar EyeTracker: vrCamera es null");
    }

    // Movement Tracker
    if (useMovementTracker)
    {
        if (playerTransform != null)
        {
            var mt = playerTransform.gameObject.GetComponent<MovementTracker>();
            if (mt == null) mt = playerTransform.gameObject.AddComponent<MovementTracker>();
            
            if (mt != null)
            {
                mt.trackedObject = playerTransform; 
                mt.enabled = true;
            }
            else Debug.LogWarning("[VRTracking] ⚠️ Error al añadir MovementTracker (Script missing?)");
        }
        else Debug.LogWarning("[VRTracking] ⚠️ No se puede iniciar MovementTracker: playerTransform es null (XR Origin no encontrado)");
    }

    // Hand Trackers
    if (useHandTracker)
    {
        if (leftHand != null)
        {
            var ht = leftHand.gameObject.GetComponent<HandTracker>();
            if (ht == null) ht = leftHand.gameObject.AddComponent<HandTracker>();
            
            if (ht != null)
            {
                ht.handName = "left";
                ht.enabled = true;
            }
        }
        
        if (rightHand != null)
        {
            var ht = rightHand.gameObject.GetComponent<HandTracker>();
            if (ht == null) ht = rightHand.gameObject.AddComponent<HandTracker>();
            
            if (ht != null)
            {
                ht.handName = "right";
                ht.enabled = true;
            }
        }
    }

    // Loggers (Raycast/Collision)
    if (useRaycastLogger)
    {
        var rl = gameObject.GetComponent<RaycastLogger>();
        if (rl == null) rl = gameObject.AddComponent<RaycastLogger>();
        if (rl != null) rl.enabled = true;
    }

    if (useCollisionLogger)
    {
        // CollisionLogger requiere un Collider. Si no hay, AddComponent fallará o devolverá un componente inútil.
        // Verificamos si podemos añadirlo.
        var col = gameObject.GetComponent<Collider>();
        if (col != null)
        {
            var cl = gameObject.GetComponent<CollisionLogger>();
            if (cl == null) cl = gameObject.AddComponent<CollisionLogger>();
            if (cl != null) cl.enabled = true;
        }
        else
        {
            Debug.LogWarning("[VRTracking] ⚠️ CollisionLogger habilitado pero no hay Collider en VRTrackingManager. Se omite para evitar errores.");
        }
    }

    Debug.Log($"[VRTracking] Tracking ON → {userId} / {sessionId}");
}

// Helper para buscar referencias automáticamente si no están asignadas
private void TryFindReferences()
{
    // 1. Cámara
    if (vrCamera == null)
    {
        vrCamera = Camera.main;
        if (vrCamera == null) vrCamera = Object.FindFirstObjectByType<Camera>(); // Fallback agresivo
        if (vrCamera != null) Debug.Log("[VRTracking] 🔍 Auto-asignada Main Camera (o primera cámara encontrada).");
    }

    // 2. XR Origin / Player
    if (playerTransform == null)
    {
        // Intentos comunes de nombres para XR Interaction Toolkit / Unity XR
        var origin = GameObject.Find("XR Origin") ?? 
                     GameObject.Find("XR Rig") ?? 
                     GameObject.Find("Player") ??
                     GameObject.FindWithTag("Player");
        
        if (origin != null)
        {
            playerTransform = origin.transform;
            Debug.Log($"[VRTracking] 🔍 Auto-asignado Player Transform: {origin.name}");
        }
        else if (vrCamera != null)
        {
            // Fallback para No-VR: El usuario es la cámara
            playerTransform = vrCamera.transform;
            Debug.Log("[VRTracking] ⚠️ XR Origin no encontrado. Usando la Cámara como Player Transform (Modo No-VR).");
        }
    }

    // 3. Manos (Búsqueda algo heurística)
    if (leftHand == null)
    {
        var l = GameObject.Find("Left Controller") ?? GameObject.Find("LeftHand Controller");
        if (l != null) leftHand = l.transform;
    }
    
    if (rightHand == null)
    {
        var r = GameObject.Find("Right Controller") ?? GameObject.Find("RightHand Controller");
        if (r != null) rightHand = r.transform;
    }
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
