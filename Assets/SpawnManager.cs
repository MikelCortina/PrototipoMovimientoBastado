using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;
    [SerializeField] private GameObject playerPrefab; // El mismo que está en el NetworkManager

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Este método SOLO se llama desde el HealthSystem cuando la vida llega a 0
    public void RequestRespawn(ulong clientId)
    {
        if (!IsServer) return;
        StartCoroutine(RespawnCoroutine(clientId));
    }

    private IEnumerator RespawnCoroutine(ulong clientId)
    {
        // Pequeña espera para que el Despawn del objeto viejo termine
        yield return new WaitForSeconds(0.1f);

        Vector3 spawnPos = new Vector3(Random.Range(-5, 5), 2, Random.Range(-5, 5));

        // Creamos el nuevo objeto
        GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Lo vinculamos al cliente que murió
        newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Debug.Log($"Respawn completado para el cliente: {clientId}");
    }
}