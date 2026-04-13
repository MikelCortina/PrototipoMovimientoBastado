using Fusion;
using UnityEngine;
using System.Collections;

public class FusionRespawnManager : NetworkBehaviour
{
    // Hacemos un Singleton para encontrarlo fácilmente desde cualquier script
    public static FusionRespawnManager Instance;

    [Header("Configuración de Respawn")]
    public Transform defaultSpawnPoint;
    public float respawnDelay = 3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Esta función la llamará el jugador cuando su vida llegue a 0
    public void RespawnPlayer(NetworkObject playerObj, FusionHealthSystem healthSystem)
    {
        // Solo el "Host" (StateAuthority) dirige el tiempo y la lógica de reaparición
        if (HasStateAuthority)
        {
            StartCoroutine(RespawnCoroutine(playerObj, healthSystem));
        }
    }

    private IEnumerator RespawnCoroutine(NetworkObject playerObj, FusionHealthSystem healthSystem)
    {
        // 1. Avisamos a todos los clientes que este jugador está "muerto" (para ocultarlo)
        healthSystem.RPC_SetAliveState(false);

        // 2. Esperamos los segundos de penalización
        yield return new WaitForSeconds(respawnDelay);

        if (playerObj != null && playerObj.IsValid)
        {
            // 3. Teletransportamos al jugador al punto de spawn
            playerObj.transform.position = defaultSpawnPoint.position;
            playerObj.transform.rotation = defaultSpawnPoint.rotation;

            // Frenamos en seco cualquier inercia o caída que tuviera al morir
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 4. Le devolvemos la vida al máximo
            healthSystem.currentHealth = healthSystem.maxHealth;

            // 5. Avisamos a todos para que lo vuelvan a "mostrar" y le devuelvan el control
            healthSystem.RPC_SetAliveState(true);
        }
    }
}