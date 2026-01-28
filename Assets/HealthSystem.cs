using UnityEngine;
using System;
using Photon.Pun;

public class HealthSystem : MonoBehaviourPun
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    // Evento para que otros sistemas (UI, Sonido) escuchen cuando recibimos daño
    // Pasa: (cantidad de daño, punto de impacto, si es crítico)
    public event Action<float, Vector3, bool> OnDamageReceived;

    void Awake()
    {
        currentHealth = maxHealth;
    }


    [PunRPC]
    public void ReduceHealth(float amount, Vector3 hitPoint)
    {
        // Solo el dueño (IsMine) procesa el daño para evitar que la vida baje doble o triple
        if (photonView.IsMine)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Notificamos a todos los demás cuál es nuestra vida actual
            photonView.RPC("RPC_SyncHealth", RpcTarget.Others, currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // El efecto visual/sonido ocurre para todos
        OnDamageReceived?.Invoke(amount, hitPoint, false);
    }

    [PunRPC]
    public void RPC_SyncHealth(float newHealth)
    {
        currentHealth = newHealth;
        Debug.Log($"Sincronizando vida de {gameObject.name}: {currentHealth}/{maxHealth}");
    }

    // Dentro de HealthSystem.cs
    private void Die()
    {
        if (photonView.IsMine)
        {
            Debug.Log("Local player died, requesting respawn...");

            // 1. Notificar al manager (debe estar en un objeto persistente en la escena)
            if (RespawnManager.Instance != null)
            {
                RespawnManager.Instance.OnPlayerDied();
            }
            else
            {
                Debug.LogError("No existe RespawnManager en la escena!");
            }

            // 2. Destruir este objeto en la red
            PhotonNetwork.Destroy(gameObject);
        }
    }
}