using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject _player;
    public Transform[] spawnPoints;

    void Start()
    {
        Debug.Log("Iniciando conexión...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("¡Conectado al Servidor Maestro!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("¡En el Lobby!");
        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("¡Dentro de la sala! Spawning player...");

        // ActorNumber empieza en 1
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        int spawnIndex = (actorNumber - 1) % spawnPoints.Length;

        PhotonNetwork.Instantiate(
            _player.name,
            spawnPoints[spawnIndex].position,
            spawnPoints[spawnIndex].rotation
        );
    }
}
