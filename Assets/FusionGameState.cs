using Fusion;
using UnityEngine;

public enum MatchState
{
    Waiting = 0,
    Playing = 1,
    Finished = 2
}

public class FusionGameState : NetworkBehaviour
{
    public static FusionGameState Instance { get; private set; }

    [Header("Estado global de partida")]
    [Networked] public int connectedPlayers { get; set; }
    [Networked] public MatchState currentMatchState { get; set; }
    [Networked] public TickTimer matchTimer { get; set; }

    [Header("Configuración")]
    [SerializeField] private float matchDuration = 300f;

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Ya existe un FusionGameState en escena.");
            return;
        }

        Instance = this;

        Debug.Log($"FusionGameState Spawned | HasStateAuthority: {HasStateAuthority} | IsProxy: {IsProxy}");

        if (HasStateAuthority)
        {
            currentMatchState = MatchState.Waiting;
            Debug.Log("FusionGameState inicializado en Waiting.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (currentMatchState == MatchState.Playing && matchTimer.Expired(Runner))
        {
            currentMatchState = MatchState.Finished;
            Debug.Log("La partida ha terminado.");
        }
    }

    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }

    public void SetConnectedPlayers(int amount)
    {
        if (!HasStateAuthority)
            return;

        connectedPlayers = amount;
        Debug.Log($"ConnectedPlayers actualizado a: {connectedPlayers}");
    }

    public void StartMatch()
    {
        if (!HasStateAuthority)
            return;

        currentMatchState = MatchState.Playing;
        matchTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);

        Debug.Log("La partida ha comenzado.");
    }

    public void EndMatch()
    {
        if (!HasStateAuthority)
            return;

        currentMatchState = MatchState.Finished;
        Debug.Log("La partida ha finalizado manualmente.");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(NetworkId targetId, float amount, Vector3 hitPoint, Vector3 hitNormal, FusionDamageType type, NetworkId instigatorId)
    {
        Debug.Log($"RPC_RequestDamage | HasStateAuthority: {HasStateAuthority} | targetId: {targetId} | instigatorId: {instigatorId}");

        if (!HasStateAuthority)
            return;

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObject))
        {
            Debug.LogWarning("RPC_RequestDamage cancelado: no se encontró targetObject");
            return;
        }

        NetworkObject instigatorObject = null;
        Runner.TryFindObject(instigatorId, out instigatorObject);

        FusionHealthSystem health = targetObject.GetComponent<FusionHealthSystem>();
        if (health == null)
            health = targetObject.GetComponentInParent<FusionHealthSystem>();

        if (health == null)
        {
            Debug.LogWarning($"RPC_RequestDamage cancelado: {targetObject.name} no tiene FusionHealthSystem");
            return;
        }

        Debug.Log($"RPC_RequestDamage validado | targetObject: {targetObject.name} | instigator: {(instigatorObject != null ? instigatorObject.name : "NULL")} | targetHasAuthority: {health.HasStateAuthority}");

        if (health.HasStateAuthority)
        {
            FusionDamageData data = new FusionDamageData
            {
                amount = amount,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                type = type,
                instigator = instigatorObject
            };

            health.ApplyValidatedDamage(data);
        }
        else
        {
            health.RPC_ApplyValidatedDamage(amount, hitPoint, hitNormal, type, instigatorId);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRespawn(NetworkId playerId)
    {
        Debug.Log($"RPC_RequestRespawn | HasStateAuthority: {HasStateAuthority} | playerId: {playerId}");

        if (!HasStateAuthority)
            return;

        if (!Runner.TryFindObject(playerId, out NetworkObject playerObject))
        {
            Debug.LogWarning("RPC_RequestRespawn cancelado: no se encontró playerObject");
            return;
        }

        FusionHealthSystem health = playerObject.GetComponent<FusionHealthSystem>();
        if (health == null)
            health = playerObject.GetComponentInParent<FusionHealthSystem>();

        if (health == null)
        {
            Debug.LogWarning($"RPC_RequestRespawn cancelado: {playerObject.name} no tiene FusionHealthSystem");
            return;
        }

        if (FusionRespawnManager.Instance == null)
        {
            Debug.LogWarning("RPC_RequestRespawn cancelado: FusionRespawnManager.Instance es NULL");
            return;
        }

        Debug.Log($"RPC_RequestRespawn validado | playerObject: {playerObject.name}");

        FusionRespawnManager.Instance.RespawnPlayer(playerObject, health);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReportElimination(NetworkId killerId, NetworkId victimId)
    {
        Debug.Log($"RPC_ReportElimination | killerId: {killerId} | victimId: {victimId} | HasStateAuthority: {HasStateAuthority}");

        if (!HasStateAuthority)
            return;

        if (!Runner.TryFindObject(victimId, out NetworkObject victimObject))
        {
            Debug.LogWarning("RPC_ReportElimination cancelado: no se encontró victimObject");
            return;
        }

        NetworkObject killerObject = null;
        Runner.TryFindObject(killerId, out killerObject);

        FusionPlayerState victimState = victimObject.GetComponent<FusionPlayerState>();
        if (victimState == null)
            victimState = victimObject.GetComponentInParent<FusionPlayerState>();

        if (victimState == null)
        {
            Debug.LogWarning($"RPC_ReportElimination cancelado: {victimObject.name} no tiene FusionPlayerState");
            return;
        }

        if (victimState.HasStateAuthority)
            victimState.AddDeath();
        else
            victimState.RPC_AddDeath();

        if (killerObject != null && killerObject != victimObject)
        {
            FusionPlayerState killerState = killerObject.GetComponent<FusionPlayerState>();
            if (killerState == null)
                killerState = killerObject.GetComponentInParent<FusionPlayerState>();

            if (killerState != null)
            {
                if (killerState.HasStateAuthority)
                    killerState.AddKill();
                else
                    killerState.RPC_AddKill();
            }
        }
    }
    public void RequestRespawn(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        Debug.Log($"RequestRespawn | playerObject: {playerObject.name}");

        RPC_RequestRespawn(playerObject.Id);
    }

    public void RequestDamage(NetworkObject targetObject, FusionDamageData data)
    {
        Debug.Log($"RequestDamage | target: {(targetObject != null ? targetObject.name : "NULL")} | instigator: {(data.instigator != null ? data.instigator.name : "NULL")} | amount: {data.amount}");

        if (targetObject == null)
            return;

        if (data.instigator == null)
        {
            Debug.LogWarning("RequestDamage cancelado: instigator es NULL");
            return;
        }

        RPC_RequestDamage(
            targetObject.Id,
            data.amount,
            data.hitPoint,
            data.hitNormal,
            data.type,
            data.instigator.Id
        );
    }

    public void ReportElimination(NetworkObject killerObject, NetworkObject victimObject)
    {
        if (victimObject == null)
            return;

        NetworkId killerId = default;
        if (killerObject != null)
            killerId = killerObject.Id;

        RPC_ReportElimination(killerId, victimObject.Id);
    }
}