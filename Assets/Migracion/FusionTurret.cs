using Fusion;
using UnityEngine;

public class FusionTurret : NetworkBehaviour
{
    [Header("Turret Settings")]
    public float detectionRadius = 20f;
    public float fireInterval = 0.5f;
    public float damagePerShot = 20f;
    public float lifeTime = 150f;
    public float rotationSpeed = 8f;
    public LayerMask targetMask = ~0;

    [Header("Visual Feedback")]
    public LineRenderer tracerLine;
    public float tracerDuration = 0.05f;
    public Transform firePoint;
    [Networked] private TickTimer lifeTimer { get; set; }
    [Networked] private TickTimer fireTimer { get; set; }
    [Networked] public NetworkId ownerId { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            fireTimer = TickTimer.None;
        }
    }

    private void ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (tracerLine == null)
            return;

        StartCoroutine(DoTracer(startPoint, endPoint));
    }

    private System.Collections.IEnumerator DoTracer(Vector3 startPoint, Vector3 endPoint)
    {
        tracerLine.gameObject.SetActive(true);
        tracerLine.positionCount = 2;
        tracerLine.SetPosition(0, startPoint);
        tracerLine.SetPosition(1, endPoint);

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.gameObject.SetActive(false);
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState != MatchState.Playing)
        {
            Runner.Despawn(Object);
            return;
        }

        if (lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        NetworkObject target = FindClosestValidTarget();
        if (target == null)
            return;

        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
        }

        if (fireTimer.ExpiredOrNotRunning(Runner))
        {
            fireTimer = TickTimer.CreateFromSeconds(Runner, fireInterval);
            FireAtTarget(target);
        }
    }

    public void SetOwner(NetworkObject owner)
    {
        if (!HasStateAuthority)
            return;

        ownerId = owner != null ? owner.Id : default;
    }

    private NetworkObject FindClosestValidTarget()
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

            if (candidate.Id == ownerId)
                continue;

            FusionHealthSystem health = candidate.GetComponent<FusionHealthSystem>();
            if (health == null)
                health = candidate.GetComponentInParent<FusionHealthSystem>();

            if (health == null || health.isDead)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance > detectionRadius)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        ShowTracer(startPoint, endPoint);
    }

    private void FireAtTarget(NetworkObject target)
    {
        if (target == null)
            return;

        FusionHealthSystem health = target.GetComponent<FusionHealthSystem>();
        if (health == null)
            health = target.GetComponentInParent<FusionHealthSystem>();

        if (health == null)
            return;

        Vector3 hitPoint = target.transform.position + Vector3.up;
        Vector3 hitNormal = (target.transform.position - transform.position).normalized;

        NetworkObject ownerObject = null;
        if (ownerId.Raw != 0)
            Runner.TryFindObject(ownerId, out ownerObject);

        FusionDamageData data = new FusionDamageData
        {
            amount = damagePerShot,
            hitPoint = hitPoint,
            hitNormal = hitNormal,
            type = FusionDamageType.Turret,
            instigator = ownerObject
        };

        if (health.HasStateAuthority)
            health.ApplyValidatedDamage(data);
        else
            health.RPC_ApplyValidatedDamage(data.amount, data.hitPoint, data.hitNormal, data.type, ownerObject != null ? ownerObject.Id : default);

        Debug.Log($"Torreta disparó a {target.name}");

        Vector3 tracerStart = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.2f;
        Vector3 tracerEnd = hitPoint;
        RPC_ShowTracer(tracerStart, tracerEnd);
    }
}