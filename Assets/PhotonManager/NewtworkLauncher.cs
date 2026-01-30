using Photon.Pun;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    void Start() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
}