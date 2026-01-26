using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
    public GameObject _player;

    void Start()
    {
      Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    void Update()
    {
       
    }
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

               Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
       
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 10 }, TypedLobby.Default);
        Debug.Log("In a room"); 

      
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        GameObject player = PhotonNetwork.Instantiate(_player.name,
          spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity);
    }
}
