using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 velocity;
    private float gravity;
    private float damage;
    private Vector3 _lastPosition;
    private ulong _ownerId;
    private bool _isServerSide;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 3f;
    private float _lifeTimer;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    public void Initialize(Vector3 direction, float speed, float grav, float dmg, ulong ownerId, bool isServer, float startTime = 0)
    {
        gravity = grav;
        damage = dmg;
        velocity = direction * speed;
        _ownerId = ownerId;
        _isServerSide = isServer;

        transform.forward = direction;
        _lastPosition = transform.position;
        _lifeTimer = 0f;

        // --- PREDICCIÓN VISUAL (EXTRAPOLACIÓN) ---
        if (!isServer && startTime > 0)
        {
            float timePassed = NetworkManager.Singleton.ServerTime.TimeAsFloat - startTime;
            // Limitamos la predicción para evitar saltos visuales absurdos (max 250ms)
            timePassed = Mathf.Clamp(timePassed, 0, 0.25f);

            if (timePassed > 0)
            {
                // Calculamos dónde debería estar la bala después de 'timePassed' segundos
                // Posición = P0 + V*t + 0.5 * g * t^2
                Vector3 displacement = (velocity * timePassed) + (0.5f * Vector3.down * gravity * timePassed * timePassed);
                transform.position += displacement;

                // Actualizamos la velocidad con la gravedad aplicada en ese tiempo
                velocity += Vector3.down * gravity * timePassed;
                _lastPosition = transform.position;
            }
        }
        // --- MANEJO VISUAL SEGURO ---

        // 1. Control del LineRenderer (el rastro)
        if (lr != null)
        {
            lr.enabled = !isServer; // Se apaga si es bala de servidor
            if (lr.enabled)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, transform.position);
            }
        }

        // 2. Control del MeshRenderer (el modelo 3D, si lo tuviera)
        // Usamos TryGetComponent para evitar el error si no existe
        if (TryGetComponent<MeshRenderer>(out MeshRenderer mesh))
        {
            mesh.enabled = !isServer; // Se apaga si es bala de servidor
        }
    }

    void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= maxLifetime) { Disable(); return; }

        velocity += Vector3.down * gravity * Time.deltaTime;
        Vector3 nextPosition = transform.position + velocity * Time.deltaTime;
        Vector3 direction = nextPosition - _lastPosition;

        // Raycast para colisiones (esto sigue ocurriendo en servidor y cliente)
        if (Physics.Raycast(_lastPosition, direction.normalized, out RaycastHit hit, direction.magnitude))
        {
            OnHit(hit);
            return;
        }

        transform.position = nextPosition;

        // --- CORRECCIÓN DEL ERROR DE INDEX ---
        // Solo actualizamos el rastro si es una bala visual (no de servidor)
        if (!_isServerSide && lr != null && lr.enabled)
        {
            // Nos aseguramos de que siempre haya 2 puntos antes de asignar
            if (lr.positionCount < 2) lr.positionCount = 2;

            lr.SetPosition(0, _lastPosition);
            lr.SetPosition(1, transform.position);
        }

        _lastPosition = transform.position;

        if (transform.position.y < -5000f) Disable();
    }

    private void OnHit(RaycastHit hit)
    {
        if (_isServerSide)
        {
            // Evitar que el proyectil choque con quien lo disparó
            NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == _ownerId) return;
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(new DamageData
                {
                    amount = damage,
                    hitPoint = hit.point,
                    instigatorId = _ownerId,
                    type = DamageType.Bullet
                });
            }
        }

        Disable();
    }

    private void Disable()
    {
        gameObject.SetActive(false); // 👈 vuelve al pool
    }

    private void OnDisable()
    {
        // Limpieza TOTAL para pooling
        velocity = Vector3.zero;
        gravity = 0f;
        damage = 0f;
        _lifeTimer = 0f;

        if (lr != null)
        {
            lr.positionCount = 0;
        }
    }
}
