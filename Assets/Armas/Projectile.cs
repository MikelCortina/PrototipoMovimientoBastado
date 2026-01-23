using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 velocity;
    private float gravity;
    private float damage;
    private Vector3 _lastPosition;

    [HideInInspector] public bool isLocal;
    private Transform _muzzleAnchor;
    private bool _isAttached = true;
    private float _distanceTraveled = 0f;
    public float detachDistance = 3.0f;

    // Configuración de fidelidad visual
    private Vector3 _visualTailPos; // Posición de la cola para el efecto de retraso

    // Configuración de Sub-stepping
    private const float SUB_STEP_TIME = 0.01f;

    public void Initialize(Vector3 direction, float speed, float gravity, float dmg, Transform muzzle, float lag)
    {
        lr = GetComponent<LineRenderer>();
        _muzzleAnchor = muzzle;
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
        _visualTailPos = transform.position; // Inicializar cola en el origen

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
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageData data = new DamageData
                {
                    amount = this.damage,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    type = DamageType.Bullet,
                    instigator = null
                };
                target.TakeDamage(data);
            }
        }

        gameObject.SetActive(false);
    }

    void UpdateTracerVisuals()
    {
        if (lr.positionCount < 2) return;

        // La punta (índice 1) siempre es la posición actual del proyectil
        lr.SetPosition(1, transform.position);

        if (_isAttached && _distanceTraveled < detachDistance && _muzzleAnchor != null)
        {
            // Mientras está cerca del cañón, la cola está pegada al arma
            _visualTailPos = _muzzleAnchor.position;
            lr.SetPosition(0, _visualTailPos);
        }
        else
        {
            _isAttached = false;

            // EFECTO APEX: La cola sigue a la punta con un retraso (Lerp)
            // Esto crea la ilusión de una bala estirada que viaja por el espacio
            _visualTailPos = Vector3.Lerp(_visualTailPos, transform.position, Time.deltaTime * 25f);
            lr.SetPosition(0, _visualTailPos);

            // Si la cola alcanza a la punta, desactivamos (la bala "murió" visualmente)
            if (Vector3.Distance(_visualTailPos, transform.position) < 0.1f)
                gameObject.SetActive(false);
        }
    }

    public void ResetProjectile()
    {
        _distanceTraveled = 0f;
        _isAttached = true;
        _lastPosition = transform.position;
        _visualTailPos = transform.position; // Resetear posición visual
        if (lr != null) lr.positionCount = 2;
    }
}