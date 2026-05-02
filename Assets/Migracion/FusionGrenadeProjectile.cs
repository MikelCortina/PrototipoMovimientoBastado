using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FusionGrenadeProjectile : NetworkBehaviour
{
    [Header("Grenade Settings")]
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float damage = 60f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Networked] private TickTimer fuseTimer { get; set; }
    [Networked] private NetworkId ownerId { get; set; }

    private Rigidbody rb;
    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            fuseTimer = TickTimer.CreateFromSeconds(Runner, fuseTime);
        }
    }

    public void Initialize(NetworkObject owner, Vector3 initialVelocity)
    {
        if (!HasStateAuthority)
            return;

        ownerId = owner != null ? owner.Id : default;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.linearVelocity = initialVelocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || exploded)
            return;

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState != MatchState.Playing)
        {
            Runner.Despawn(Object);
            return;
        }

        if (fuseTimer.ExpiredOrNotRunning(Runner))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (!HasStateAuthority || exploded)
            return;

        exploded = true;

        Vector3 explosionPoint = transform.position;

        NetworkObject ownerObject = null;
        if (ownerId.Raw != 0)
            Runner.TryFindObject(ownerId, out ownerObject);

        Collider[] hits = Physics.OverlapSphere(
            explosionPoint,
            explosionRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            NetworkObject hitObject = hits[i].GetComponentInParent<NetworkObject>();
            if (hitObject == null)
                continue;

            if (ownerObject != null && hitObject.Id == ownerObject.Id)
                continue;

            FusionHealthSystem health = hitObject.GetComponent<FusionHealthSystem>();
            if (health == null)
                health = hitObject.GetComponentInParent<FusionHealthSystem>();

            if (health == null || health.isDead)
                continue;

            Vector3 hitPoint = hits[i].ClosestPoint(explosionPoint);
            Vector3 hitNormal = (hitPoint - explosionPoint).normalized;

            if (hitNormal.sqrMagnitude < 0.001f)
                hitNormal = Vector3.up;

            FusionDamageData data = new FusionDamageData
            {
                amount = damage,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                type = FusionDamageType.Grenade,
                instigator = ownerObject
            };

            if (health.HasStateAuthority)
                health.ApplyValidatedDamage(data);
            else
                health.RPC_ApplyValidatedDamage(
                    data.amount,
                    data.hitPoint,
                    data.hitNormal,
                    data.type,
                    ownerObject != null ? ownerObject.Id : default
                );
        }

        Debug.Log($"Granada explotó en {explosionPoint}");

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}