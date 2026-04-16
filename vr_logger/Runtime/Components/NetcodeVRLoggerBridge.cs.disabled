using Unity.Netcode;
using UnityEngine;
using VRLogger;

/// <summary>
/// PUENTE NGO DE VR LOGGER.
/// Esta es la forma más limpia y cómoda de añadir red a los controles del GM
/// sin ensuciar ni romper el código interno del VR Logger (que seguiría funcionando
/// para proyectos de 1 solo jugador).
/// 
/// Instrucciones:
/// 1. Pon este script en un GameObject que sea NetworkObject en tu escena (o en el prefab de Player/NetworkManager).
/// 2. Asegúrate de desactivar temporalmente los teclados en el Perfil de VRLogger,
///    o de lo contrario el Host saltará de participante 2 veces (una por local y otra por red).
/// </summary>
public class NetcodeVRLoggerBridge : NetworkBehaviour
{
    void Update()
    {
        // Solo enviamos los comandos si somos el dueño de este script/jugador en red local
        // para que no se presione en los simulacros de los otros clones
        if (!IsOwner) return;

        // Si este cliente aprieta N, manda la orden al Servidor (Host)
        if (Input.GetKeyDown(KeyCode.N))
        {
            RequestNextParticipantServerRpc();
        }
        
        // Si aprieta P, manda orden de pausa
        if (Input.GetKeyDown(KeyCode.P))
        {
            RequestPauseServerRpc();
        }

        // Si aprieta E, manda orden de finalizar
        if (Input.GetKeyDown(KeyCode.E))
        {
            RequestEndParticipantServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextParticipantServerRpc()
    {
        // El servidor recibe la petición y la re-envía a ABSOLUTAMENTE TODOS los ordenadores
        ExecuteNextParticipantClientRpc();
    }

    [ClientRpc]
    private void ExecuteNextParticipantClientRpc()
    {
        // Todos los ordenadores ejecutan localmente el avance de participante
        if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsRunning())
        {
            Debug.Log("[NetcodeVRLoggerBridge] Ejecutando orden de RED: GM_NextParticipant");
            ParticipantFlowController.Instance.GM_NextParticipant();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPauseServerRpc()
    {
        ExecutePauseClientRpc();
    }

    [ClientRpc]
    private void ExecutePauseClientRpc()
    {
        if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsRunning())
        {
            Debug.Log("[NetcodeVRLoggerBridge] Ejecutando orden de RED: TogglePause");
            ParticipantFlowController.Instance.TogglePause();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEndParticipantServerRpc()
    {
        ExecuteEndParticipantClientRpc();
    }

    [ClientRpc]
    private void ExecuteEndParticipantClientRpc()
    {
        if (ParticipantFlowController.Instance != null && ParticipantFlowController.Instance.IsRunning())
        {
            Debug.Log("[NetcodeVRLoggerBridge] Ejecutando orden de RED: GM_EndTurn");
            ParticipantFlowController.Instance.GM_EndTurn();
        }
    }
}
