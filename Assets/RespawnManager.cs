using UnityEngine;
using Photon.Pun;
using System.Collections;

public class RespawnManager : MonoBehaviourPunCallbacks
{
    public static RespawnManager Instance;
    public RoomManager roomManager;
    public float respawnDelay = 1.5f;
    private bool isResetting = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OnPlayerDied()
    {
        if (isResetting) return; // Si ya estamos cambiando de mapa, ignorar

        isResetting = true;
        photonView.RPC("RPC_NextMap", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_NextMap()
    {
        isResetting = true; // Bloquear en todos los clientes
        StartCoroutine(SwitchMapSequence());
    }

    private IEnumerator SwitchMapSequence()
    {
        isResetting = true;

        // 1. CADA cliente busca y destruye SU propio jugador
        // No importa quién murió, ambos deben desaparecer para ir al nuevo mapa
        GameObject myPlayer = null;
        PhotonView[] allViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in allViews)
        {
            if (view.IsMine && view.CompareTag("Player"))
            {
                myPlayer = view.gameObject;
                break;
            }
        }

        if (myPlayer != null)
        {
            PhotonNetwork.Destroy(myPlayer);
        }

        // 2. Pausa para visualización (muerte, fundido a negro, etc.)
        yield return new WaitForSeconds(respawnDelay);

        // 3. SOLO el Master incrementa el índice
        if (PhotonNetwork.IsMasterClient)
        {
            int currentIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["CurrentMapIndex"];
            // Usamos el conteo de mapas del RoomManager
            int nextIndex = (currentIndex + 1) % roomManager.maps.Count;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("CurrentMapIndex", nextIndex);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // 4. Importante: Resetear el candado para permitir la siguiente muerte
        isResetting = false;
    }
}