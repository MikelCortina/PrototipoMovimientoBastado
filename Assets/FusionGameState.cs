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

    [Header("Killstreaks - Torreta")]
    [SerializeField] private NetworkObject turretPrefab;
    [SerializeField] private float turretSpawnDistance = 3f;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestTurretUse(NetworkId instigatorId, Vector3 origin, Vector3 forward)
    {
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (turretPrefab == null)
        {
            Debug.LogWarning("No hay turretPrefab asignado en FusionGameState.");
            return;
        }

        if (!Runner.TryFindObject(instigatorId, out NetworkObject instigatorObject))
            return;

        FusionPlayerState instigatorState = instigatorObject.GetComponent<FusionPlayerState>();
        if (instigatorState == null)
            instigatorState = instigatorObject.GetComponentInParent<FusionPlayerState>();

        if (instigatorState == null)
            return;

        if (!instigatorState.hasTurretStreak)
            return;

        if (instigatorState.HasStateAuthority)
            instigatorState.ConsumeTurretStreak();
        else
            instigatorState.RPC_ConsumeTurretStreak();

        Vector3 flatForward = forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;

        flatForward.Normalize();

        Vector3 spawnPosition = origin + flatForward * turretSpawnDistance;
        spawnPosition.y = origin.y + 2f;

        if (Physics.Raycast(spawnPosition, Vector3.down, out RaycastHit groundHit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPosition = groundHit.point;
        }

        Debug.Log($"[TURRET] Instigator: {instigatorObject.name} | Origin: {origin} | Forward: {flatForward} | SpawnPosition: {spawnPosition}");

        NetworkObject turretObject = Runner.Spawn(
            turretPrefab,
            spawnPosition,
            Quaternion.LookRotation(flatForward),
            default
        );

        Debug.Log($"[TURRET] Spawned turret network object: {turretObject.name} at {turretObject.transform.position}");

        FusionTurret turret = turretObject.GetComponent<FusionTurret>();
        if (turret != null)
            turret.SetOwner(instigatorObject);

        Debug.Log($"Torreta desplegada por {instigatorObject.name}");
    }

    [Header("Killstreaks - Ataque Aéreo")]
    [SerializeField] private float airStrikeDamage = 100f;
    [SerializeField] private float airStrikeMaxRange = 200f;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAirStrikeUse(NetworkId instigatorId)
    {
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (!Runner.TryFindObject(instigatorId, out NetworkObject instigatorObject))
            return;

        FusionPlayerState instigatorState = instigatorObject.GetComponent<FusionPlayerState>();
        if (instigatorState == null)
            instigatorState = instigatorObject.GetComponentInParent<FusionPlayerState>();

        if (instigatorState == null)
            return;

        if (!instigatorState.hasAirStrikeStreak)
            return;

        NetworkObject targetObject = FindClosestValidEnemy(instigatorObject, airStrikeMaxRange);
        if (targetObject == null)
        {
            Debug.Log("Ataque aéreo cancelado: no hay enemigo válido.");
            return;
        }

        if (instigatorState.HasStateAuthority)
            instigatorState.ConsumeAirStrikeStreak();
        else
            instigatorState.RPC_ConsumeAirStrikeStreak();

        FusionHealthSystem targetHealth = targetObject.GetComponent<FusionHealthSystem>();
        if (targetHealth == null)
            targetHealth = targetObject.GetComponentInParent<FusionHealthSystem>();

        if (targetHealth == null)
            return;

        Vector3 hitPoint = targetObject.transform.position + Vector3.up;
        Vector3 hitNormal = Vector3.down;

        FusionDamageData data = new FusionDamageData
        {
            amount = airStrikeDamage,
            hitPoint = hitPoint,
            hitNormal = hitNormal,
            type = FusionDamageType.Explosion,
            instigator = instigatorObject
        };

        if (targetHealth.HasStateAuthority)
            targetHealth.ApplyValidatedDamage(data);
        else
            targetHealth.RPC_ApplyValidatedDamage(data.amount, data.hitPoint, data.hitNormal, data.type, instigatorObject.Id);

        Debug.Log($"Ataque aéreo golpeó a {targetObject.name}");
    }

    private NetworkObject FindClosestValidEnemy(NetworkObject instigatorObject, float maxRange)
    {
        FusionPlayerState[] allPlayers = FindObjectsByType<FusionPlayerState>(FindObjectsSortMode.None);

        NetworkObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            FusionPlayerState state = allPlayers[i];
            if (state == null || !state.Object || !state.Object.IsValid)
                continue;

            NetworkObject candidate = state.Object;

            if (candidate.Id == instigatorObject.Id)
                continue;

            FusionHealthSystem health = candidate.GetComponent<FusionHealthSystem>();
            if (health == null)
                health = candidate.GetComponentInParent<FusionHealthSystem>();

            if (health == null || health.isDead)
                continue;

            float distance = Vector3.Distance(instigatorObject.transform.position, candidate.transform.position);
            if (distance > maxRange)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    [Header("Killstreaks - Granada")]
    [SerializeField] private float grenadeDamage = 60f;
    [SerializeField] private float grenadeRadius = 6f;
    [SerializeField] private float grenadeRange = 18f;
    [SerializeField] private LayerMask grenadeHitMask = ~0;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestGrenadeUse(NetworkId instigatorId, Vector3 origin, Vector3 forward)
    {
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (!Runner.TryFindObject(instigatorId, out NetworkObject instigatorObject))
            return;

        FusionPlayerState instigatorState = instigatorObject.GetComponent<FusionPlayerState>();
        if (instigatorState == null)
            instigatorState = instigatorObject.GetComponentInParent<FusionPlayerState>();

        if (instigatorState == null)
            return;

        if (!instigatorState.hasGrenadeStreak)
            return;

        if (instigatorState.HasStateAuthority)
            instigatorState.ConsumeGrenadeStreak();
        else
            instigatorState.RPC_ConsumeGrenadeStreak();

        Vector3 explosionPoint = origin + forward.normalized * grenadeRange;

        if (Physics.Raycast(origin, forward.normalized, out RaycastHit hit, grenadeRange, grenadeHitMask, QueryTriggerInteraction.Ignore))
            explosionPoint = hit.point;

        ProcessGrenadeExplosion(instigatorObject, explosionPoint);
    }

    private void ProcessGrenadeExplosion(NetworkObject instigatorObject, Vector3 explosionPoint)
    {
        Collider[] hits = Physics.OverlapSphere(explosionPoint, grenadeRadius, grenadeHitMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            IFusionDamageable damageable = hits[i].GetComponentInParent<IFusionDamageable>();
            if (damageable == null)
                continue;

            NetworkObject hitObject = hits[i].GetComponentInParent<NetworkObject>();
            if (hitObject == null)
                continue;

            if (instigatorObject != null && hitObject.Id == instigatorObject.Id)
                continue;

            Vector3 hitPoint = hits[i].ClosestPoint(explosionPoint);
            Vector3 hitNormal = (hitPoint - explosionPoint).normalized;

            FusionDamageData data = new FusionDamageData
            {
                amount = grenadeDamage,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                type = FusionDamageType.Explosion,
                instigator = instigatorObject
            };

            FusionHealthSystem health = hitObject.GetComponent<FusionHealthSystem>();
            if (health == null)
                health = hitObject.GetComponentInParent<FusionHealthSystem>();

            if (health == null)
                continue;

            if (health.HasStateAuthority)
                health.ApplyValidatedDamage(data);
            else
                health.RPC_ApplyValidatedDamage(data.amount, data.hitPoint, data.hitNormal, data.type, instigatorObject != null ? instigatorObject.Id : default);
        }

        Debug.Log($"Granada explotó en {explosionPoint} con radio {grenadeRadius}");
    }
    public static FusionGameState Instance { get; private set; }

    [Header("Estado global de partida")]
    [Networked] public int connectedPlayers { get; set; }
    [Networked] public MatchState currentMatchState { get; set; }
    [Networked] public TickTimer matchTimer { get; set; }
    [Networked] public NetworkString<_16> winnerName { get; set; }

    [Header("Configuración")]
    [SerializeField] private float matchDuration = 300f;
    [SerializeField] private int scoreLimit = 1500;

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Ya existe un FusionGameState en escena.");
            return;
        }

        Instance = this;

        if (HasStateAuthority)
        {
            currentMatchState = MatchState.Waiting;
            winnerName = "";
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
            Debug.Log("La partida ha terminado por tiempo.");
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
    }

    public void StartMatch()
    {
        if (!HasStateAuthority)
            return;

        currentMatchState = MatchState.Playing;
        winnerName = "";
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
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObject))
            return;

        NetworkObject instigatorObject = null;
        Runner.TryFindObject(instigatorId, out instigatorObject);

        FusionHealthSystem health = targetObject.GetComponent<FusionHealthSystem>();
        if (health == null)
            health = targetObject.GetComponentInParent<FusionHealthSystem>();

        if (health == null)
            return;

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
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (!Runner.TryFindObject(playerId, out NetworkObject playerObject))
            return;

        FusionHealthSystem health = playerObject.GetComponent<FusionHealthSystem>();
        if (health == null)
            health = playerObject.GetComponentInParent<FusionHealthSystem>();

        if (health == null)
            return;

        if (FusionRespawnManager.Instance == null)
            return;

        FusionRespawnManager.Instance.RespawnPlayer(playerObject, health);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReportElimination(NetworkId killerId, NetworkId victimId)
    {
        if (!HasStateAuthority)
            return;

        if (currentMatchState != MatchState.Playing)
            return;

        if (!Runner.TryFindObject(victimId, out NetworkObject victimObject))
            return;

        NetworkObject killerObject = null;
        Runner.TryFindObject(killerId, out killerObject);

        FusionPlayerState victimState = victimObject.GetComponent<FusionPlayerState>();
        if (victimState == null)
            victimState = victimObject.GetComponentInParent<FusionPlayerState>();

        if (victimState == null)
            return;

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
                int projectedScore = killerState.score + 100;

                if (killerState.HasStateAuthority)
                    killerState.AddKill();
                else
                    killerState.RPC_AddKill();

                CheckScoreLimit(killerState, projectedScore);
            }
        }
    }

    private void CheckScoreLimit(FusionPlayerState killerState, int projectedScore)
    {
        if (!HasStateAuthority || killerState == null)
            return;

        if (projectedScore < scoreLimit)
            return;

        string finalWinnerName = killerState.playerName.ToString();
        if (string.IsNullOrWhiteSpace(finalWinnerName))
            finalWinnerName = killerState.name;

        winnerName = finalWinnerName;
        currentMatchState = MatchState.Finished;

        Debug.Log($"Partida terminada por score. Ganador: {winnerName}");
    }

    public void RequestRespawn(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        RPC_RequestRespawn(playerObject.Id);
    }

    public void RequestDamage(NetworkObject targetObject, FusionDamageData data)
    {
        if (targetObject == null)
            return;

        if (data.instigator == null)
            return;

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}