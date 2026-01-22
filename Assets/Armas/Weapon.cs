using UnityEngine;
using Unity.Netcode;
using UnityEngine.Audio;

public class WeaponR301 : NetworkBehaviour
{
    [Header("Ajustes de Disparo Normal")]
    public float fireRate = 0.1f;
    public float bulletSpeed = 150f;
    public float bulletGravity = 9.81f;
    public float damage = 14f;
    public Vector3 recoilKickback = new Vector3(0f, 0f, -0.1f);
    public Vector3 recoilRotation = new Vector3(-2f, 0f, 0f);
    public AudioSource shootAudioSource;
    public AudioClip shootClip;

    [Header("Bloom Angular (Disparo Normal)")]
    public float maxSpreadDegrees = 2.0f;
    public float bloomPerShot = 0.2f;
    public float recoverRate = 1.5f;

    [Header("Modo Escopeta (Click Derecho)")]
    public int pellets = 8;
    public float shotgunSpreadDegrees = 6f;
    public float shotgunFireRate = 0.6f;
    public float shotgunDamageMultiplier = 0.6f;
    public float shotgunBulletSpeed = 90f;
    public Vector3 shootgunRecoilKickback = new Vector3(0f, 0f, -0.2f);
    public Vector3 shootgunRecoilRotation = new Vector3(-3f, 0f, 0f);

    [Header("Referencias")]
    public Camera mainCamera;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public Recoil recoil;

    private float _nextFireTime;
    private float _nextShotgunTime;
    private float _currentBloomDegrees = 0f;
    private int _fireSequenceCounter = 0;

    void Update()
    {
        // Solo el dueño del objeto puede procesar el Input
        if (!IsOwner) return;

        // Recuperación de Bloom
        if (!Input.GetButton("Fire1"))
        {
            _currentBloomDegrees = Mathf.MoveTowards(_currentBloomDegrees, 0f, recoverRate * Time.deltaTime);
        }

        // DISPARO NORMAL
        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            HandleFire(false);
        }

        // DISPARO ESCOPETA
        if (Input.GetButtonDown("Fire2") && Time.time >= _nextShotgunTime)
        {
            _nextShotgunTime = Time.time + shotgunFireRate;
            HandleFire(true);
        }
    }

    private void HandleFire(bool isShotgun)
    {
        Vector3 targetPoint = GetCameraTargetPoint();

        // 1. Obtenemos el tiempo de red PRIMERO para que sea consistente en todas las llamadas
        float strikeTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;

        // 2. Generamos la semilla
        int shotSeed = Random.Range(1, int.MaxValue) + _fireSequenceCounter;
        _fireSequenceCounter++;

        // 3. CORRECCIÓN ERROR CS7036: Ahora pasamos strikeTime (el 5º argumento)
        ExecuteVisualFire(targetPoint, isShotgun, true, shotSeed, strikeTime);

        // 4. Enviamos todo al servidor
        FireServerRpc(targetPoint, isShotgun, shotSeed, strikeTime);
    }

    [ServerRpc]
    void FireServerRpc(Vector3 targetPoint, bool isShotgun, int shotSeed, float strikeTime)
    {
        // CORRECCIÓN ERROR CS7036: Añadido strikeTime a la llamada del ClientRpc
        FireClientRpc(targetPoint, isShotgun, shotSeed, strikeTime);

        // Validación de lag (Seguridad)
        float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        float rtt = currentTime - strikeTime;
        float maxLag = 0.25f;
        float validatedStrikeTime = (rtt < 0 || rtt > maxLag) ? currentTime - Mathf.Clamp(rtt, 0, maxLag) : strikeTime;

        LagCompensator.RewindAll(validatedStrikeTime);

        Random.InitState(shotSeed);
        if (isShotgun) SpawnShotgunProjectiles(targetPoint, true, validatedStrikeTime);
        else SpawnSingleProjectile(targetPoint, true, validatedStrikeTime);

        LagCompensator.RestoreAll();
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    void FireClientRpc(Vector3 targetPoint, bool isShotgun, int shotSeed, float strikeTime)
    {
        if (IsOwner) return;
        // CORRECCIÓN ERROR CS1501: Ahora pasamos los 5 argumentos requeridos
        ExecuteVisualFire(targetPoint, isShotgun, false, shotSeed, strikeTime);
    }

    // Asegúrate de que la firma de esta función sea EXACTAMENTE así:
    void ExecuteVisualFire(Vector3 targetPoint, bool isShotgun, bool applyRecoil, int shotSeed, float strikeTime)
    {
        Random.InitState(shotSeed);

        if (isShotgun)
        {
            // CORRECCIÓN ERROR CS0103: Pasamos strikeTime que ahora sí existe en el contexto
            SpawnShotgunProjectiles(targetPoint, false, strikeTime);
            shootAudioSource.pitch = Random.Range(2f, 2.15f);
            if (applyRecoil && recoil != null) recoil.ApplyRecoil(shootgunRecoilKickback, shootgunRecoilRotation);
        }
        else
        {
            // CORRECCIÓN ERROR CS0103: Pasamos strikeTime que ahora sí existe en el contexto
            SpawnSingleProjectile(targetPoint, false, strikeTime);
            if (applyRecoil && recoil != null) recoil.ApplyRecoil(recoilKickback, recoilRotation);
        }

        shootAudioSource.PlayOneShot(shootClip);
        shootAudioSource.pitch = 1f;
    }
    // --- LÓGICA DE SPAWN ADAPTADA ---

    void SpawnSingleProjectile(Vector3 targetPoint, bool isServer, float startTime)
    {
        Vector3 baseDirection = (targetPoint - muzzlePoint.position).normalized;
        Vector2 randomCircle = Random.insideUnitCircle * _currentBloomDegrees;
        Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
        Vector3 finalDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

        if (!isServer) _currentBloomDegrees = Mathf.Clamp(_currentBloomDegrees + bloomPerShot, 0f, maxSpreadDegrees);

        CreateProjectile(finalDirection, bulletSpeed, damage, isServer, startTime);
    }

    void SpawnShotgunProjectiles(Vector3 targetPoint, bool isServer, float startTime)
    {
        Vector3 baseDirection = (targetPoint - muzzlePoint.position).normalized;
        for (int i = 0; i < pellets; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * shotgunSpreadDegrees;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
            Vector3 pelletDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

            CreateProjectile(pelletDirection, shotgunBulletSpeed, damage * shotgunDamageMultiplier, isServer, startTime);
        }
    }

    void CreateProjectile(Vector3 direction, float speed, float dmg, bool isServer, float startTime = 0)
    {
        GameObject bulletGO = BulletPool.Instance.GetBullet();
        if (bulletGO != null)
        {
            bulletGO.transform.position = muzzlePoint.position;
            bulletGO.transform.rotation = Quaternion.LookRotation(direction);
            bulletGO.SetActive(true);

            Projectile proj = bulletGO.GetComponent<Projectile>();
            if (proj != null)
            {
                // PASAMOS EL startTime AQUÍ
                proj.Initialize(direction, speed, bulletGravity, dmg, OwnerClientId, isServer, startTime);
            }
        }
    }

    Vector3 GetCameraTargetPoint()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return Physics.Raycast(ray, out RaycastHit hit, 1000f) ? hit.point : ray.GetPoint(1000f);
    }
}