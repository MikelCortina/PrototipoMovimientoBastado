using Photon.Pun;
using Photon.Realtime;
using UnityEngine;


public class MatchmakingLauncher : MonoBehaviourPunCallbacks
{
    public void StartMatchmaking()
    {
        if (PhotonNetwork.IsConnected)
        {
            // Busca una sala de 2 jugadores que no esté llena
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Si no hay salas disponibles, crea una nueva para 2 jugadores
        string roomName = "Room_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        // Una vez dentro, activamos el RoomManager o cargamos la escena de combate
        Debug.Log("Entraste a la sala: " + PhotonNetwork.CurrentRoom.Name);
    }
}