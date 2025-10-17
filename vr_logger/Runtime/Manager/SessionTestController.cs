using UnityEngine;
using System.Collections;
using VRLogger;

public class SessionTestController : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Esperamos un frame para asegurarnos de que UserSessionManager ya ha hecho Awake()
        yield return null;

        var sessionManager = UserSessionManager.Instance;

        if (sessionManager == null)
        {
            Debug.LogError("[SessionTestController] ❌ No se encontró UserSessionManager en la escena.");
            yield break;
        }

        // Mostramos info básica
        Debug.Log($"✅ Sesión iniciada correctamente:\n" +
                  $"- User ID: {sessionManager.GetUserId()}\n" +
                  $"- Group ID: {sessionManager.GetGroupId()}\n" +
                  $"- Session ID: {sessionManager.GetSessionId()}");

        // Simulamos algunos eventos con logs
        Debug.Log("🟢 Registrando eventos de prueba...");

        yield return sessionManager.LogEventWithSession("task", "task_start", new { task_name = "shooting_test" });
        yield return new WaitForSeconds(1.5f);
        yield return sessionManager.LogEventWithSession("task", "target_hit", new { target_id = "TGT_001", reaction_time_ms = 850 });
        yield return new WaitForSeconds(0.8f);
        yield return sessionManager.LogEventWithSession("task", "task_end", new { success = true, total_time_s = 2.3f });

        Debug.Log("🟩 Fin de prueba de sesión.");
    }
}
