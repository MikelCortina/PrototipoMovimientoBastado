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
    public float detachDistance = 3.0f;

    private Vector3 _visualTailPos;
    private const float SUB_STEP_TIME = 0.01f;

    public void Initialize(Vector3 direction, float speed, float gravity, float dmg, Transform muzzle, float lag, NetworkObject source)
    {
        lr = GetComponent<LineRenderer>();
        _muzzleAnchor = muzzle;
        _sourcePlayer = source;
        this.gravity = gravity;
        this.damage = dmg;
        this.velocity = direction * speed;

        transform.position = muzzle.position;

        if (lag > 0)
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

        lr.positionCount = 2;
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, transform.position);
    }

    void Update()
    {
        float timeLeft = Time.deltaTime;

        while (timeLeft > 0)
        {
            float dt = Mathf.Min(timeLeft, SUB_STEP_TIME);

            velocity += Vector3.down * gravity * dt;
            Vector3 step = velocity * dt;
            Vector3 nextPosition = transform.position + step;

            Vector3 direction = nextPosition - _lastPosition;
            float distance = direction.magnitude;

            if (distance > 0)
            {
                if (Physics.Raycast(_lastPosition, direction.normalized, out RaycastHit hit, distance))
                {
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

        if (transform.position.y < -100f || _distanceTraveled > 2000f)
            gameObject.SetActive(false);
    }

    void OnHit(RaycastHit hit)
    {
        if (isLocal)
        {
            NetworkObject hitNO = hit.collider.GetComponentInParent<NetworkObject>();

            // Si golpeamos nuestro propio cuerpo, lo ignoramos
            if (hitNO != null && _sourcePlayer != null && hitNO.Id == _sourcePlayer.Id)
            {
                gameObject.SetActive(false);
                return;
            }

            // CORRECCIÓN: Buscamos la nueva interfaz (usamos InParent por si la bala da en un brazo/pierna)
            IFusionDamageable target = hit.collider.GetComponentInParent<IFusionDamageable>();

            if (target != null)
            {
                // CORRECCIÓN: Usamos el nuevo struct
                FusionDamageData data = new FusionDamageData
                {
                    amount = this.damage,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    type = FusionDamageType.Bullet, // CORRECCIÓN: Usamos el nuevo enum
                    instigator = _sourcePlayer      // CORRECCIÓN: Asignamos el jugador que disparó
                };
                target.TakeDamage(data);
            }
        }

        gameObject.SetActive(false);
    }

    void UpdateTracerVisuals()
    {
        if (lr.positionCount < 2) return;

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
                gameObject.SetActive(false);
        }
    }

    public void ResetProjectile()
    {
        _distanceTraveled = 0f;
        _isAttached = true;
        _lastPosition = transform.position;
        _visualTailPos = transform.position;
        if (lr != null) lr.positionCount = 2;
    }
}