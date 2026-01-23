using UnityEngine;
using Photon.Pun;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    [Header("Configuración")]
    public string playerPrefabName = "Player";
    public float respawnDelay = 3f;
    public Transform[] spawnPoints;

    private bool isRespawning = false; // Seguridad para evitar duplicados por spam

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Añadimos el parámetro PhotonView para saber QUIÉN murió
    public void RespawnPlayer(PhotonView targetView)
    {
        // SEGURIDAD 1: Solo el dueño de ese PhotonView puede pedir respawn
        if (targetView != null && !targetView.IsMine) return;

        // SEGURIDAD 2: Evitar que se ejecuten múltiples corrutinas de respawn a la vez
        if (isRespawning) return;

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        if (PhotonNetwork.InRoom)
        {
            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            int spawnIndex = (actorNumber - 1) % spawnPoints.Length;

            Transform selectedPoint = spawnPoints[spawnIndex];

            PhotonNetwork.Instantiate(
                playerPrefabName,
                selectedPoint.position,
                selectedPoint.rotation
            );
        }

        isRespawning = false;
    }
}