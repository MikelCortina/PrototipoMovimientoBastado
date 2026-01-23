using NUnit.Framework.Constraints;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Audio;

public class WeaponR301 : MonoBehaviourPun
{
    [Header("Ajustes de Disparo Normal")]
    public float fireRate = 0.1f;
    public float bulletSpeed = 150f;
    public float bulletGravity = 9.81f;
    public float damage = 14f;
    public Vector3 recoilKickback = new Vector3(0f, 0f, -0.1f); // desplazamiento hacia atrás
    public Vector3 recoilRotation = new Vector3(-2f, 0f, 0f);   // rotación hacia arriba
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
    public Vector3 shootgunRecoilKickback = new Vector3(0f, 0f, -0.2f); // desplazamiento hacia atrás
    public Vector3 shootgunRecoilRotation = new Vector3(-3f, 0f, 0f);   // rotación hacia arriba


    [Header("Referencias")]
    public Camera mainCamera;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public Recoil recoil;

    private float _nextFireTime;
    private float _nextShotgunTime;
    private float _currentBloomDegrees = 0f;

    void Update()
    {
        if (!photonView.IsMine) return;

        // Disparo Normal
        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;

            int seed = Random.Range(0, 99999);
            Vector3 targetPoint = GetCameraTargetPoint();

            // OPTIMIZACIÓN: Ya no enviamos el 'origin'. El receptor lo tomará de su muzzlePoint local.
            photonView.RPC("RPC_FireSingle", RpcTarget.All, seed, _currentBloomDegrees, targetPoint);

            _currentBloomDegrees = Mathf.Clamp(_currentBloomDegrees + bloomPerShot, 0f, maxSpreadDegrees);
            if (recoil != null) recoil.ApplyRecoil(recoilKickback, recoilRotation);
        }
        else
        {
            _currentBloomDegrees = Mathf.MoveTowards(_currentBloomDegrees, 0f, recoverRate * Time.deltaTime);
        }

        // Modo Escopeta
        if (Input.GetButtonDown("Fire2") && Time.time >= _nextShotgunTime)
        {
            _nextShotgunTime = Time.time + shotgunFireRate;
            int seed = Random.Range(0, 99999);
            Vector3 targetPoint = GetCameraTargetPoint();

            photonView.RPC("RPC_FireShotgun", RpcTarget.All, seed, targetPoint);
        }
    }

    [PunRPC]
    void RPC_FireSingle(int seed, float currentBloom, Vector3 target, PhotonMessageInfo info)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);

        // Usamos muzzlePoint.position del objeto que posee este script en el cliente remoto
        Vector3 origin = muzzlePoint.position;
        Vector3 baseDirection = (target - origin).normalized;
        Vector2 randomCircle = Random.insideUnitCircle * currentBloom;

        Random.state = oldState;

        Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
        Vector3 finalDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

        float lag = (float)(PhotonNetwork.Time - info.SentServerTime);
        SpawnBullet(origin, finalDirection, damage, lag);

        shootAudioSource.PlayOneShot(shootClip);
    }

    [PunRPC]
    void RPC_FireShotgun(int seed, Vector3 target, PhotonMessageInfo info)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);

        Vector3 origin = muzzlePoint.position;
        Vector3 baseDirection = (target - origin).normalized;
        float lag = (float)(PhotonNetwork.Time - info.SentServerTime);

        for (int i = 0; i < pellets; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * shotgunSpreadDegrees;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
            Vector3 pelletDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

            SpawnShotgunPellet(origin, pelletDirection, lag);
        }

        Random.state = oldState;
        if (recoil != null && photonView.IsMine) recoil.ApplyRecoil(shootgunRecoilKickback, shootgunRecoilRotation);
        shootAudioSource.PlayOneShot(shootClip);
    }

    // Actualizamos los métodos de Spawn para aceptar la posición de origen sincronizada
    void SpawnBullet(Vector3 origin, Vector3 direction, float dmg, float lag)
    {
        GameObject bulletGO = BulletPool.Instance.GetBullet();

        if (bulletGO != null)
        {
            bulletGO.transform.position = origin;
            bulletGO.transform.rotation = Quaternion.LookRotation(direction);
            bulletGO.SetActive(true); // Activarla antes de inicializar

            Projectile proj = bulletGO.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.ResetProjectile(); // Importante resetear el estado
                proj.isLocal = photonView.IsMine;
                proj.Initialize(direction, bulletSpeed, bulletGravity, dmg, muzzlePoint, lag);
            }
        }
    }

   void SpawnShotgunPellet(Vector3 origin, Vector3 direction, float lag)
{
    GameObject bulletGO = BulletPool.Instance.GetBullet();

    if (bulletGO != null)
    {
        // 1. Posicionar y rotar la bala ANTES de activarla
        bulletGO.transform.position = origin;
        bulletGO.transform.rotation = Quaternion.LookRotation(direction);

        // 2. ¡IMPORTANTE! Activar el objeto para que el LineRenderer y el Script funcionen
        bulletGO.SetActive(true);

        Projectile proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
        {
            // 3. Resetear el estado interno (distancia, rastro, etc.)
            proj.ResetProjectile(); 
            proj.isLocal = photonView.IsMine;
            
            // 4. Inicializar con los valores de escopeta
            proj.Initialize(direction, shotgunBulletSpeed, bulletGravity, damage * shotgunDamageMultiplier, muzzlePoint, lag);
        }
    }
}
    Vector3 GetCameraTargetPoint()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return Physics.Raycast(ray, out RaycastHit hit, 1000f) ? hit.point : ray.GetPoint(1000f);
    }
}
