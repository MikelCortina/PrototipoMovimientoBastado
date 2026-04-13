using Fusion;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // <-- IMPORTANTE: Necesario para la UI

public class FusionHealthSystem : NetworkBehaviour, IFusionDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    [Networked] public float currentHealth { get; set; }

    [Header("UI de Vida")]
    [Tooltip("El Canvas que contiene la interfaz del jugador")]
    public Canvas playerCanvas;
    [Tooltip("La imagen de la barra de vida (tipo Filled)")]
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

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentHealth = maxHealth;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (HasInputAuthority) FindVignetteEffect();

        // --- LÓGICA DE UI ---
        if (playerCanvas != null)
        {
            // Solo encendemos el Canvas si este jugador es el NUESTRO.
            // (Así evitamos ver la UI de los enemigos pegada en nuestra pantalla)
            playerCanvas.enabled = HasInputAuthority;
        }
        UpdateHealthUI(); // Actualizamos la barra al nacer
        // --------------------

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
                break;
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
                    if (HasInputAuthority) UpdateVignette();
                    UpdateHealthUI(); // <-- ACTUALIZAMOS LA BARRA DE VIDA AQUÍ
                    break;
            }
        }
    }

    // --- NUEVA FUNCIÓN PARA LA BARRA ---
    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            // fillAmount va de 0 a 1, así que dividimos la vida actual entre la máxima
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    public void TakeDamage(FusionDamageData data)
    {
        RPC_TakeDamage(data.amount, data.hitPoint);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float amount, Vector3 hitPoint)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        RPC_PlayDamageEffects(amount, hitPoint);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDamageEffects(float amount, Vector3 hitPoint)
    {
        if (damageSound != null) AudioSource.PlayClipAtPoint(damageSound, hitPoint, volume);
        OnDamageReceived?.Invoke(amount, hitPoint, false);
    }

    private void UpdateVignette()
    {
        if (HasInputAuthority && vignette != null)
        {
            float healthPercent = 1f - (currentHealth / maxHealth);
            vignette.intensity.value = healthPercent * maxVignetteIntensity;
        }
    }

    private void Die()
    {
        if (FusionRespawnManager.Instance != null)
        {
            FusionRespawnManager.Instance.RespawnPlayer(Object, this);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetAliveState(bool isAlive)
    {
        var movement = GetComponent<FusionMovement>();
        if (movement != null) movement.enabled = isAlive;

        var weapon = GetComponentInChildren<FusionWeaponR301>();
        if (weapon != null) weapon.enabled = isAlive;

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in allRenderers) r.enabled = isAlive;

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders) c.enabled = isAlive;

        if (isAlive)
        {
            var renderSetup = GetComponent<FusionRenderSetup>();
            if (renderSetup != null) renderSetup.Spawned();
        }

        // Si revivimos, actualizamos la UI por si acaso
        if (isAlive) UpdateHealthUI();
    }
}