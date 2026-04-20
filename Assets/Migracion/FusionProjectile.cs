using UnityEngine;
using Fusion;

public class FusionProjectile : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 velocity;
    private float gravity;
    private float damage;
    private Vector3 _lastPosition;

    [HideInInspector] public bool isLocal;

    private Transform _muzzleAnchor;
    private NetworkObject _sourcePlayer;

    private bool _isAttached = true;
    private float _distanceTraveled = 0f;

    [Header("Tracer")]
    public float detachDistance = 3.0f;

    [Header("Collision")]
    public LayerMask hitMask = ~0;
    public float maxDistance = 2000f;
    public float minSelfIgnoreDistance = 1.5f;

    private Vector3 _visualTailPos;
    private const float SUB_STEP_TIME = 0.01f;

    public void Initialize(
        Vector3 direction,
        float speed,
        float gravity,
        float dmg,
        Transform muzzle,
        float lag,
        NetworkObject source)
    {
        lr = GetComponent<LineRenderer>();
        _muzzleAnchor = muzzle;
        _sourcePlayer = source;

        this.gravity = gravity;
        this.damage = dmg;
        this.velocity = direction * speed;

        transform.position = muzzle != null ? muzzle.position : transform.position;

        if (lag > 0f)
        {
            float simTime = 0f;
            float timeStep = 0.02f;

            while (simTime < lag)
            {
                float dt = Mathf.Min(timeStep, lag - simTime);
                velocity += Vector3.down * gravity * dt;
                transform.position += velocity * dt;
                simTime += dt;
            }
        }

        _lastPosition = transform.position;
        _visualTailPos = transform.position;
        _distanceTraveled = 0f;
        _isAttached = true;

        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, transform.position);
        }
    }

    private void Update()
    {
        float timeLeft = Time.deltaTime;

        while (timeLeft > 0f)
        {
            float dt = Mathf.Min(timeLeft, SUB_STEP_TIME);

            velocity += Vector3.down * gravity * dt;
            Vector3 step = velocity * dt;
            Vector3 nextPosition = transform.position + step;

            Vector3 direction = nextPosition - _lastPosition;
            float distance = direction.magnitude;

            if (distance > 0f)
            {
                if (Physics.Raycast(_lastPosition, direction.normalized, out RaycastHit hit, distance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (ShouldIgnoreHit(hit))
                    {
                        _lastPosition = nextPosition;
                        transform.position = nextPosition;
                        _distanceTraveled += step.magnitude;
                        timeLeft -= dt;
                        continue;
                    }

                    transform.position = hit.point;
                    OnHit(hit);
                    return;
                }
            }

            _lastPosition = transform.position;
            transform.position = nextPosition;
            _distanceTraveled += step.magnitude;
            timeLeft -= dt;
        }

        UpdateTracerVisuals();

        if (transform.position.y < -100f || _distanceTraveled > maxDistance)
            DeactivateProjectile();
    }

    private bool ShouldIgnoreHit(RaycastHit hit)
    {
        if (_sourcePlayer == null)
            return false;

        NetworkObject hitNO = hit.collider.GetComponentInParent<NetworkObject>();
        if (hitNO == null)
            return false;

        if (hitNO.Id != _sourcePlayer.Id)
            return false;

        return _distanceTraveled < minSelfIgnoreDistance;
    }

    private void OnHit(RaycastHit hit)
    {
        if (isLocal)
            TryReportHit(hit);

        DeactivateProjectile();
    }

    private void TryReportHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return;

        NetworkObject targetObject = hit.collider.GetComponentInParent<NetworkObject>();
        if (targetObject == null)
            return;

        FusionDamageData data = new FusionDamageData
        {
            amount = damage,
            hitPoint = hit.point,
            hitNormal = hit.normal,
            type = FusionDamageType.Bullet,
            instigator = _sourcePlayer
        };

        Debug.Log($"TryReportHit | isLocal: {isLocal} | sourcePlayer: {(_sourcePlayer != null ? _sourcePlayer.name : "NULL")} | target: {(targetObject != null ? targetObject.name : "NULL")}");

        if (FusionGameState.Instance != null)
        {
            FusionGameState.Instance.RequestDamage(targetObject, data);
        }
    }

    private void UpdateTracerVisuals()
    {
        if (lr == null || lr.positionCount < 2)
            return;

        lr.SetPosition(1, transform.position);

        if (_isAttached && _distanceTraveled < detachDistance && _muzzleAnchor != null)
        {
            _visualTailPos = _muzzleAnchor.position;
            lr.SetPosition(0, _visualTailPos);
        }
        else
        {
            _isAttached = false;
            _visualTailPos = Vector3.Lerp(_visualTailPos, transform.position, Time.deltaTime * 25f);
            lr.SetPosition(0, _visualTailPos);

            if (Vector3.Distance(_visualTailPos, transform.position) < 0.1f)
                DeactivateProjectile();
        }
    }

    private void DeactivateProjectile()
    {
        gameObject.SetActive(false);
    }

    public void ResetProjectile()
    {
        _distanceTraveled = 0f;
        _isAttached = true;
        _lastPosition = transform.position;
        _visualTailPos = transform.position;

        if (lr == null)
            lr = GetComponent<LineRenderer>();

        if (lr != null)
            lr.positionCount = 2;
    }
}