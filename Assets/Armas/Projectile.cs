using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Projectile : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 velocity;
    private float gravity;
    private float damage;
    private Vector3 _lastPosition;
    public GameObject impactEffectPrefab;

    // Referencia al cañón para que la estela lo siga
    private Transform _muzzleAnchor;
    private bool _isAttached = true;
    private float _distanceTraveled = 0f;

    [Header("Ajustes de Estela")]
    public float detachDistance = 3.0f; // Distancia a la que la bala se "suelta" del arma

    public void Initialize(Vector3 direction, float speed, float gravity, float dmg, Transform muzzle)
    {
        lr = GetComponent<LineRenderer>();
        _muzzleAnchor = muzzle;

        // Configuración visual garantizada
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;

        if (lr.material == null || lr.material.name.Contains("Default"))
        {
            lr.material = new Material(Shader.Find("Unlit/Color"));
            lr.material.color = Color.yellow;
        }

        this.gravity = gravity;
        this.damage = dmg;
        this.velocity = direction * speed;

        // Posición inicial
        transform.position = muzzle.position;
        _lastPosition = transform.position;

        // Inicializar posiciones de la línea
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, transform.position);
    }

    void Update()
    {
        // 1. Calcular física y movimiento
        velocity += Vector3.down * gravity * Time.deltaTime;
        Vector3 step = velocity * Time.deltaTime;
        Vector3 nextPosition = transform.position + step;

        _distanceTraveled += step.magnitude;

        // 2. Raycast Predictivo (Para no atravesar paredes)
        Vector3 direction = nextPosition - _lastPosition;
        float distance = direction.magnitude;

        if (Physics.Raycast(_lastPosition, direction, out RaycastHit hit, distance))
        {
            OnHit(hit);
            return; // Detener actualización si impacta
        }

        // 3. Actualizar posiciones lógicas
        _lastPosition = transform.position;
        transform.position = nextPosition;

        // 4. Actualizar Estela (El truco de Apex)
        UpdateTracerVisuals();

        // Autodestrucción por seguridad (fuera de límites)
        if (transform.position.y < -100f || _distanceTraveled > 2000f)
            Destroy(gameObject);
    }

    void UpdateTracerVisuals()
    {
        // La punta de la bala (Posición 1) siempre es donde está el objeto Projectile
        lr.SetPosition(1, transform.position);

        // El origen de la bala (Posición 0)
        if (_isAttached && _distanceTraveled < detachDistance && _muzzleAnchor != null)
        {
            // Mientras esté cerca, el origen sigue al cañón del arma aunque muevas la cámara
            lr.SetPosition(0, _muzzleAnchor.position);
        }
        else
        {
            // Una vez lejos, el origen se suelta y empieza a perseguir a la punta
            _isAttached = false;
            Vector3 currentStart = lr.GetPosition(0);
            lr.SetPosition(0, Vector3.Lerp(currentStart, transform.position, Time.deltaTime * 15f));

            // Si el inicio casi alcanza a la punta, destruimos el objeto
            if (Vector3.Distance(currentStart, transform.position) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnHit(RaycastHit hit)
    {
        // Aseguramos que la línea llegue hasta el punto de impacto
        lr.SetPosition(1, hit.point);

        // Lógica de daño aquí (ej. hit.collider.GetComponent<Health>().TakeDamage(damage);)
        Debug.Log("Impactó en: " + hit.collider.name);

        // Aquí conectamos con el sistema de IDamageable anterior
        IDamageable target = hit.collider.GetComponent<IDamageable>();

        if (target != null)
        {
            DamageData data = new DamageData
            {
                amount = 15f,
                hitPoint = hit.point,
                hitNormal = hit.normal,
                type = DamageType.Bullet
            };
            target.TakeDamage(data);
        }

        // Feedback visual de impacto (Impact Decals)
       // Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(gameObject);
    }

 
}