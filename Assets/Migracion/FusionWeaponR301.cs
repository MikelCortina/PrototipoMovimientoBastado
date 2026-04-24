using Fusion;
using UnityEngine;
using UnityEngine.Audio;

public class FusionWeaponR301 : NetworkBehaviour
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
    public Vector3 shotgunRecoilKickback = new Vector3(0f, 0f, -0.2f);
    public Vector3 shotgunRecoilRotation = new Vector3(-3f, 0f, 0f);

    [Header("Main Weapon Mode")]
    public bool isUsingShotgunModeAsMainWeapon = false;
    public int mainWeaponPellets = 8;
    public float mainWeaponSpreadDegrees = 6f;
    public float mainWeaponDamageMultiplier = 0.6f;

    [Header("Referencias")]
    public Camera mainCamera;
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public FusionRecoil recoil;

    private PlayerAnimations playerAnimations;

    [Networked] private TickTimer nextFireTimer { get; set; }
    [Networked] private TickTimer nextShotgunTimer { get; set; }
    [Networked] private float currentBloomDegrees { get; set; }
    [Networked] private NetworkBool isShootingHeld { get; set; }

    [Networked] public NetworkButtons buttonsPrevious { get; set; }

    public override void Spawned()
    {
        playerAnimations = GetComponent<PlayerAnimations>();
        if (playerAnimations == null)
            Debug.LogError("No se encontró PlayerAnimations en el jugador.");

        nextFireTimer = TickTimer.None;
        nextShotgunTimer = TickTimer.None;
        currentBloomDegrees = 0f;
    }

    public override void FixedUpdateNetwork()
    {
        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState == MatchState.Finished)
        {
            if (isShootingHeld)
            {
                isShootingHeld = false;

                if (Runner.IsForward && playerAnimations != null)
                    playerAnimations.SetShooting(false);
            }

            return;
        }

        if (GetInput(out NetworkInputData input))
        {
            var pressed = input.buttons.GetPressed(buttonsPrevious);
            var released = input.buttons.GetReleased(buttonsPrevious);
            bool fire1Held = input.buttons.IsSet(NetworkInputData.BUTTON_FIRE1);

            if (pressed.IsSet(NetworkInputData.BUTTON_FIRE1))
            {
                isShootingHeld = true;
                if (Runner.IsForward && playerAnimations != null)
                    playerAnimations.SetShooting(true);
            }

            if (released.IsSet(NetworkInputData.BUTTON_FIRE1))
            {
                isShootingHeld = false;
                if (Runner.IsForward && playerAnimations != null)
                    playerAnimations.SetShooting(false);
            }

            if (fire1Held && nextFireTimer.ExpiredOrNotRunning(Runner))
            {
                nextFireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);

                int seed = Runner.Tick;
                Vector3 targetPoint = GetCameraTargetPoint();

                if (isUsingShotgunModeAsMainWeapon)
                {
                    FireShotgunAsMainWeapon(seed, targetPoint);
                }
                else
                {
                    FireSingleLocal(seed, currentBloomDegrees, targetPoint);
                    currentBloomDegrees = Mathf.Clamp(currentBloomDegrees + bloomPerShot, 0f, maxSpreadDegrees);
                }

                RPC_PlayShootEffects(isUsingShotgunModeAsMainWeapon);

                if (Runner.IsForward && HasInputAuthority && shootAudioSource)
                    shootAudioSource.PlayOneShot(shootClip);

                if (Runner.IsForward && HasInputAuthority && recoil != null)
                {
                    if (isUsingShotgunModeAsMainWeapon)
                        recoil.ApplyRecoil(shotgunRecoilKickback, shotgunRecoilRotation);
                    else
                        recoil.ApplyRecoil(recoilKickback, recoilRotation);
                }
            }
            else
            {
                currentBloomDegrees = Mathf.MoveTowards(currentBloomDegrees, 0f, recoverRate * Runner.DeltaTime);
            }

            if (pressed.IsSet(NetworkInputData.BUTTON_FIRE2) && nextShotgunTimer.ExpiredOrNotRunning(Runner))
            {
                nextShotgunTimer = TickTimer.CreateFromSeconds(Runner, shotgunFireRate);

                int seed = Runner.Tick;
                Vector3 targetPoint = GetCameraTargetPoint();

                FireShotgunLocal(seed, targetPoint);
                RPC_PlayShootEffects(true);

                if (Runner.IsForward && HasInputAuthority && shootAudioSource)
                    shootAudioSource.PlayOneShot(shootClip);

                if (Runner.IsForward && HasInputAuthority && recoil != null)
                    recoil.ApplyRecoil(shotgunRecoilKickback, shotgunRecoilRotation);
            }

            buttonsPrevious = input.buttons;
        }
    }

    private void FireSingleLocal(int seed, float currentBloom, Vector3 target)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);

        Vector3 origin = muzzlePoint.position;
        Vector3 baseDirection = (target - origin).normalized;
        Vector2 randomCircle = Random.insideUnitCircle * currentBloom;

        Random.state = oldState;

        Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
        Vector3 finalDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

        SpawnBullet(origin, finalDirection, damage, 0f);
    }

    private void FireShotgunLocal(int seed, Vector3 target)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);

        Vector3 origin = muzzlePoint.position;
        Vector3 baseDirection = (target - origin).normalized;

        for (int i = 0; i < pellets; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * shotgunSpreadDegrees;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
            Vector3 pelletDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

            SpawnShotgunPellet(origin, pelletDirection, 0f, damage * shotgunDamageMultiplier);
        }

        Random.state = oldState;
    }

    private void FireShotgunAsMainWeapon(int seed, Vector3 target)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);

        Vector3 origin = muzzlePoint.position;
        Vector3 baseDirection = (target - origin).normalized;
        int pelletsToUse = Mathf.Max(1, mainWeaponPellets);

        for (int i = 0; i < pelletsToUse; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * mainWeaponSpreadDegrees;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0);
            Vector3 pelletDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;

            SpawnShotgunPellet(origin, pelletDirection, 0f, damage * mainWeaponDamageMultiplier);
        }

        Random.state = oldState;
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_PlayShootEffects(bool isShotgun)
    {
        if (shootAudioSource && shootClip)
            shootAudioSource.PlayOneShot(shootClip);
    }

    private void SpawnBullet(Vector3 origin, Vector3 direction, float dmg, float lag)
    {
        GameObject bulletGO = FusionBulletPool.Instance.GetBullet();

        if (bulletGO != null)
        {
            bulletGO.transform.position = origin;
            bulletGO.transform.rotation = Quaternion.LookRotation(direction);
            bulletGO.SetActive(true);

            FusionProjectile proj = bulletGO.GetComponent<FusionProjectile>();
            if (proj != null)
            {
                proj.ResetProjectile();
                proj.isLocal = HasInputAuthority;
                proj.Initialize(direction, bulletSpeed, bulletGravity, dmg, muzzlePoint, lag, Object);
            }
        }
    }

    private void SpawnShotgunPellet(Vector3 origin, Vector3 direction, float lag, float pelletDamage)
    {
        GameObject bulletGO = FusionBulletPool.Instance.GetBullet();

        if (bulletGO != null)
        {
            bulletGO.transform.position = origin;
            bulletGO.transform.rotation = Quaternion.LookRotation(direction);
            bulletGO.SetActive(true);

            FusionProjectile proj = bulletGO.GetComponent<FusionProjectile>();
            if (proj != null)
            {
                proj.ResetProjectile();
                proj.isLocal = HasInputAuthority;
                proj.Initialize(direction, shotgunBulletSpeed, bulletGravity, pelletDamage, muzzlePoint, lag, Object);
            }
        }
    }

    private Vector3 GetCameraTargetPoint()
    {
        if (mainCamera == null)
            return transform.position + transform.forward * 1000f;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return Physics.Raycast(ray, out RaycastHit hit, 1000f) ? hit.point : ray.GetPoint(1000f);
    }
}