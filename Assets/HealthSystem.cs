using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class HealthSystem : NetworkBehaviour
{
    [Header("Stats")]
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    public float maxHealth = 100f;

    [Header("Respawn Settings")]
    [SerializeField] private GameObject playerPrefab; // Arrastra aquí el prefab del jugador

    public void ReduceHealth(float amount, Vector3 hitPoint, bool isHeadshot)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        NotifyDamageClientRpc(amount, hitPoint, isHeadshot);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        ulong clientId = OwnerClientId;
        Debug.Log($"Jugador {clientId} ha muerto. Solicitando respawn...");

        // 1. Llamamos al manager pasando nuestra ID
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RequestRespawn(clientId);
        }

        // 2. Nos destruimos (Despawn elimina el objeto en todos los clientes)
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    private void NotifyDamageClientRpc(float damage, Vector3 pos, bool isHeadshot) { /* Tu código UI */ }
}