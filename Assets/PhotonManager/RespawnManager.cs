using UnityEngine;
using Photon.Pun;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RespawnManager : MonoBehaviourPunCallbacks
{
    public static RespawnManager Instance;
    public RoomManager roomManager;
    public float respawnDelay = 2.0f;
    private bool isResetting = false;
    [Space] public GameObject roundEndCam;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OnPlayerDied(int deadActorID)
    {
        if (isResetting) return;
        // Enviamos el ID de quien murió a todos
        photonView.RPC("RPC_StartMapTransition", RpcTarget.All, deadActorID);
    }

    [PunRPC]
    private void RPC_StartMapTransition(int deadActorID)
    {
        if (isResetting) return;
        isResetting = true;

        // 1. El Master registra el punto
        if (PhotonNetwork.IsMasterClient && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerKilled(deadActorID);
        }

        StartCoroutine(SwitchMapSequence());
    }

    private IEnumerator SwitchMapSequence()
    {
        roundEndCam.SetActive(true);

        // 2. Destruir jugador local
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) PhotonNetwork.Destroy(p);
        }

        yield return new WaitForSeconds(respawnDelay);

        // 3. El Master cambia el mapa oficialmente
        if (PhotonNetwork.IsMasterClient)
        {
            int currentIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["CurrentMapIndex"];
            int nextIndex = (currentIndex + 1) % roomManager.maps.Count;

            Hashtable props = new Hashtable { { "CurrentMapIndex", nextIndex } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        isResetting = false;
    }
}