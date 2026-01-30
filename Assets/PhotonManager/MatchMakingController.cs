using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchmakingController : MonoBehaviourPunCallbacks
{

    public GameObject panelBusqueda;
    private bool buscandoPartida = false;
    public void EncontrarPartida()
    {
        buscandoPartida = true;
        panelBusqueda.SetActive(true);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (!buscandoPartida) return;

        Debug.Log("Conectado al Master. Entrando al Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if (!buscandoPartida) return;

        Debug.Log("Ya estamos en el Lobby. Buscando partida...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No hay salas. Creando una...");
        string nombreSala = "Match_" + Random.Range(100, 999);
        PhotonNetwork.CreateRoom(nombreSala, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Esperando rival...");
        // Si la sala está llena (2/2), cargamos la escena de juego
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("Game"); // Usa LoadLevel para sincronizar a todos
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Si somos el Master y entra el segundo jugador, arrancamos
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }
    public void CancelarMatchmaking()
    {
        buscandoPartida = false;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        panelBusqueda.SetActive(false);
        Debug.Log("Matchmaking cancelado por el usuario.");
    }
    public override void OnLeftRoom()
    {
        // Una vez fuera de la sala, podrías querer volver al lobby o simplemente estar listo
        Debug.Log("Salida de la sala confirmada.");
    }
}