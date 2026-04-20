using Fusion;
using UnityEngine;
using System.Collections;

public class FusionRespawnManager : NetworkBehaviour
{
    public static FusionRespawnManager Instance;

    [Header("Configuración de Respawn")]
    public Transform[] spawnPoints;
    public float respawnDelay = 3f;

    private int _lastSpawnIndex = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RespawnPlayer(NetworkObject playerObj, FusionHealthSystem healthSystem)
    {
        if (!HasStateAuthority)
            return;

        if (playerObj == null || !playerObj.IsValid || healthSystem == null)
            return;

        StartCoroutine(RespawnCoroutine(playerObj, healthSystem));
    }

    private IEnumerator RespawnCoroutine(NetworkObject playerObj, FusionHealthSystem healthSystem)
    {
        healthSystem.RPC_RequestSetAliveState(false);

        yield return new WaitForSeconds(respawnDelay);

        if (playerObj == null || !playerObj.IsValid || healthSystem == null)
            yield break;

        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("No hay spawnPoints asignados en FusionRespawnManager.");
            yield break;
        }

        healthSystem.RPC_ForceRespawnAt(spawnPoint.position, spawnPoint.rotation);
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (spawnPoints.Length == 1)
            return spawnPoints[0];

        int index;
        do
        {
            index = Random.Range(0, spawnPoints.Length);
        }
        while (index == _lastSpawnIndex);

        _lastSpawnIndex = index;
        return spawnPoints[index];
    }
}