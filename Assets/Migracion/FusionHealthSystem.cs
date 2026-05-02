using Fusion;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class FusionHealthSystem : NetworkBehaviour, IFusionDamageable
{

    private NetworkObject _lastDamageInstigator;
    private FusionDamageType _lastDamageType = FusionDamageType.Bullet;

    [Header("Stats")]
    public float maxHealth = 100f;
    [Networked] public float currentHealth { get; set; }
    [Networked] public NetworkBool isDead { get; set; }

    [Header("UI de Vida")]
    public Canvas playerCanvas;
    public Image healthBarFill;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip damageSound;
    [Range(0, 1)] public float volume = 0.7f;

    [Header("Efectos Visuales (Vignette)")]
    public float maxVignetteIntensity = 1f;
    private Vignette vignette;
    private ChangeDetector _changeDetector;

    public event Action<float, Vector3, bool> OnDamageReceived;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSetAliveState(bool alive)
    {
        if (!HasStateAuthority)
            return;

        RPC_SetAliveState(alive);
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentHealth = maxHealth;
            isDead = false;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (HasInputAuthority)
            FindVignetteEffect();

        if (playerCanvas != null)
            playerCanvas.enabled = HasInputAuthority;

        UpdateHealthUI();
        UpdateVignette();

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    private void FindVignetteEffect()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);

        foreach (var vol in volumes)
        {
            if (vol.profile != null && vol.profile.TryGet(out vignette))
            {
                UpdateVignette();
                return;
            }
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(currentHealth):
                case nameof(isDead):
                    UpdateHealthUI();

                    if (HasInputAuthority)
                        UpdateVignette();
                    break;
            }
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill == null)
            return;

        float normalizedHealth = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        healthBarFill.fillAmount = Mathf.Clamp01(normalizedHealth);
    }

    private void UpdateVignette()
    {
        if (!HasInputAuthority || vignette == null)
            return;

        float normalizedHealth = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        float missingHealthPercent = 1f - Mathf.Clamp01(normalizedHealth);
        vignette.intensity.value = missingHealthPercent * maxVignetteIntensity;
    }

    public void TakeDamage(FusionDamageData data)
    {
        if (FusionGameState.Instance == null)
            return;

        if (Object == null)
            return;

        FusionGameState.Instance.RequestDamage(Object, data);
    }

    public void ApplyValidatedDamage(FusionDamageData data)
    {
        Debug.Log($"ApplyValidatedDamage | object: {name} | HasStateAuthority: {HasStateAuthority} | amount: {data.amount}");

        if (!HasStateAuthority)
            return;

        ApplyDamageInternal(data);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyValidatedDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, FusionDamageType type, NetworkId instigatorId)
    {
        Debug.Log($"RPC_ApplyValidatedDamage | object: {name} | HasStateAuthority: {HasStateAuthority} | amount: {amount}");

        if (!HasStateAuthority)
            return;

        NetworkObject instigatorObject = null;
        Runner.TryFindObject(instigatorId, out instigatorObject);

        FusionDamageData data = new FusionDamageData
        {
            amount = amount,
            hitPoint = hitPoint,
            hitNormal = hitNormal,
            type = type,
            instigator = instigatorObject
        };

        ApplyDamageInternal(data);
    }

    private void ApplyDamageInternal(FusionDamageData data)
    {
        if (isDead)
            return;

        if (data.amount <= 0f)
            return;

        currentHealth -= data.amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        RPC_PlayDamageEffects(data.amount, data.hitPoint);


        _lastDamageInstigator = data.instigator;
        _lastDamageType = data.type;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDead = true;
            Die(data.instigator);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDamageEffects(float amount, Vector3 hitPoint)
    {
        if (damageSound != null)
            AudioSource.PlayClipAtPoint(damageSound, hitPoint, volume);

        OnDamageReceived?.Invoke(amount, hitPoint, false);
    }

    private FusionKillCause ConvertDamageTypeToKillCause(FusionDamageType damageType)
    {
        switch (damageType)
        {
            case FusionDamageType.Bullet:
                return FusionKillCause.Bullet;

            case FusionDamageType.Grenade:
                return FusionKillCause.Grenade;

            case FusionDamageType.AirStrike:
                return FusionKillCause.AirStrike;

            case FusionDamageType.Turret:
                return FusionKillCause.Turret;

            default:
                return FusionKillCause.Bullet;
        }
    }
    private void Die(NetworkObject killer)
    {
        if (!HasStateAuthority)
            return;

        FusionKillCause cause = ConvertDamageTypeToKillCause(_lastDamageType);

        Debug.Log($"Jugador muerto. Killer: {(killer != null ? killer.name : "desconocido")} | Cause: {cause}");

        if (FusionGameState.Instance != null)
        {
            FusionGameState.Instance.ReportElimination(killer, Object, cause);
            FusionGameState.Instance.RequestRespawn(Object);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetAliveState(bool alive)
    {
        var movement = GetComponent<FusionMovement>();
        if (movement != null)
            movement.enabled = alive;

        var weapon = GetComponentInChildren<FusionWeaponR301>();
        if (weapon != null)
            weapon.enabled = alive;

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in allRenderers)
            r.enabled = alive;

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders)
            c.enabled = alive;

        if (alive)
        {
            if (HasStateAuthority)
            {
                currentHealth = maxHealth;
                isDead = false;
            }

            var renderSetup = GetComponent<FusionRenderSetup>();
            if (renderSetup != null)
                renderSetup.Spawned();
        }

        UpdateHealthUI();

        if (HasInputAuthority)
            UpdateVignette();
    }


    public void ForceRespawnAt(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        transform.position = position;
        transform.rotation = rotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentHealth = maxHealth;
        isDead = false;

        RPC_SetAliveState(true);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ForceRespawnAt(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        ForceRespawnAt(position, rotation);
    }
    public void RestoreFullHealth()
    {
        if (!HasStateAuthority)
            return;

        currentHealth = maxHealth;
        isDead = false;
    }
}