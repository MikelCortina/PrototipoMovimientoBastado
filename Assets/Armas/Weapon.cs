using NUnit.Framework.Constraints;
using UnityEngine;

public class WeaponR301 : MonoBehaviour
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
        // DISPARO NORMAL
        if (Input.GetButton("Fire1"))
        {
            if (Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireRate;
                FireSingle();
            }
        }
        else
        {
            _currentBloomDegrees = Mathf.MoveTowards(
                _currentBloomDegrees,
                0f,
                recoverRate * Time.deltaTime
            );
        }

        // DISPARO ESCOPETA
        if (Input.GetButtonDown("Fire2"))
        {
            if (Time.time >= _nextShotgunTime)
            {
                _nextShotgunTime = Time.time + shotgunFireRate;
                FireShotgun();
            }
        }
    }

    void FireSingle()
    {
        Vector3 targetPoint = GetCameraTargetPoint();
        Vector3 baseDirection = (targetPoint - muzzlePoint.position).normalized;

        Vector2 randomCircle = Random.insideUnitCircle * _currentBloomDegrees;
        Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
        Vector3 finalDirection =
            Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

        _currentBloomDegrees = Mathf.Clamp(
            _currentBloomDegrees + bloomPerShot,
            0f,
            maxSpreadDegrees
        );

        SpawnBullet(finalDirection, damage);
        shootAudioSource.PlayOneShot(shootClip);

        if (recoil != null) recoil.ApplyRecoil(recoilKickback, recoilRotation);
    }

    void FireShotgun()
    {
        Vector3 targetPoint = GetCameraTargetPoint();
        Vector3 baseDirection = (targetPoint - muzzlePoint.position).normalized;

        for (int i = 0; i < pellets; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * shotgunSpreadDegrees;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
            Vector3 pelletDirection =
                Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

            SpawnShotgunPellet(pelletDirection);
        }

        if (recoil != null) recoil.ApplyRecoil(shootgunRecoilKickback, shootgunRecoilRotation);
    }


    void SpawnBullet(Vector3 direction, float dmg)
    {
        GameObject bulletGO = Instantiate(
            bulletPrefab,
            muzzlePoint.position,
            Quaternion.LookRotation(direction)
        );

        Projectile proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, bulletSpeed, bulletGravity, dmg, muzzlePoint); 
        }
    }

    void SpawnShotgunPellet(Vector3 direction)
    {
        GameObject bulletGO = Instantiate(
            bulletPrefab,
            muzzlePoint.position,
            Quaternion.LookRotation(direction)
        );

        Projectile proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(
                direction,
                shotgunBulletSpeed,                 // 👈 velocidad propia
                bulletGravity,
                damage * shotgunDamageMultiplier,
                muzzlePoint
            );
        }
    }


    Vector3 GetCameraTargetPoint()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return Physics.Raycast(ray, out RaycastHit hit, 1000f)
            ? hit.point
            : ray.GetPoint(1000f);
    }
}
