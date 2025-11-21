using UnityEngine;
using System.Collections;
using VRLogger;

public class SessionTestController : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Esperar un frame para asegurar que ExperimentConfig y UserSessionManager ya se han inicializado
        yield return null;

        // Obtener instancia del UserSessionManager
        var sessionManager = UserSessionManager.Instance;

        if (sessionManager == null)
        {
            Debug.LogError("[SessionTestController] ❌ No se encontró UserSessionManager en la escena.");
            yield break;
        }

        // Mostrar datos básicos
        Debug.Log($"[SessionTestController] 🟢 Sesión iniciada con éxito:");
        Debug.Log($"- User ID: {sessionManager.GetUserId()}");
        Debug.Log($"- Group ID: {sessionManager.GetGroupId()}");
        Debug.Log($"- Session ID: {sessionManager.GetSessionId()}");

        // =====================================================
        // PRIMER LOG ESPECIAL → CONFIG DEL EXPERIMENTO
        // =====================================================

        Debug.Log("[SessionTestController] 🟡 Enviando configuración del experimento a MongoDB...");

        ExperimentConfig.Instance.SendConfigAsLog();

        Debug.Log("[SessionTestController] 🟢 Configuración del experimento registrada como primer log.");
    }
}
