using UnityEngine;
using System;
using Photon.Pun;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Necesario para acceder a Vignette en URP

public class HealthSystem : MonoBehaviourPun
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [Range(0, 1)][SerializeField] private float volume = 0.7f;

    [Header("Post Processing (Vignette)")]
    [Tooltip("Intensidad máxima de la viñeta cuando el jugador está casi muerto")]
    [SerializeField] private float maxVignetteIntensity = 1f;
    private Vignette vignette;

    public event Action<float, Vector3, bool> OnDamageReceived;

    void Awake()
    {
        currentHealth = maxHealth;

        // Asegurarnos de tener un AudioSource
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Solo buscamos el volumen de post-procesado si este script pertenece al jugador local
        if (photonView.IsMine)
        {
            FindVignetteEffect();
        }
    }

    /// <summary>
    /// Busca en la escena el Global Volume que contiene el efecto de Vignette.
    /// </summary>
    private void FindVignetteEffect()
    {
        // Buscamos todos los Volúmenes en la escena
        Volume[] volumes = GameObject.FindObjectsByType<Volume>(FindObjectsSortMode.None);

        foreach (var vol in volumes)
        {
            // Si el volumen tiene un perfil y ese perfil tiene Vignette, lo asignamos
            if (vol.profile != null && vol.profile.TryGet(out vignette))
            {
                // Inicializamos la intensidad según la vida actual
                UpdateVignette();
                break;
            }
        }

        if (vignette == null)
        {
            Debug.LogWarning("HealthSystem: No se encontró un Volume con el efecto Vignette en la escena.");
        }
    }

    [PunRPC]
    public void ReduceHealth(float amount, Vector3 hitPoint)
    {
        if (photonView.IsMine)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Sincronizamos la salud con los demás
            photonView.RPC("RPC_SyncHealth", RpcTarget.Others, currentHealth);

            // Actualizamos el efecto visual localmente
            UpdateVignette();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // El sonido se reproduce para todos en la posición del impacto
        PlayDamageSound(hitPoint);

        OnDamageReceived?.Invoke(amount, hitPoint, false);
    }

    private void UpdateVignette()
    {
        // Solo el jugador local debe ver su propia viñeta oscurecerse
        if (photonView.IsMine && vignette != null)
        {
            // healthPercent será 0 con vida máxima y 1 con vida en cero
            float healthPercent = 1f - (currentHealth / maxHealth);

            // Aplicamos la intensidad
            vignette.intensity.value = healthPercent * maxVignetteIntensity;

            // Opcional: Descomenta la línea de abajo si quieres que la viñeta se vuelva roja
            // vignette.color.value = Color.Lerp(Color.black, Color.red, healthPercent);
        }
    }

    private void PlayDamageSound(Vector3 position)
    {
        if (damageSound != null)
        {
            // Crea un objeto temporal para el sonido en la posición del impacto
            AudioSource.PlayClipAtPoint(damageSound, position, volume);
        }
    }

    [PunRPC]
    public void RPC_SyncHealth(float newHealth)
    {
        currentHealth = newHealth;
    }

    private void Die()
    {
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.OnPlayerDied(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
}