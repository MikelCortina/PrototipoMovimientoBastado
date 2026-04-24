using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FusionRoomListRow : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string _sessionName;
    private FusionLauncher _launcher;

    public void SetData(string sessionName, int playerCount, int maxPlayers, FusionLauncher launcher)
    {
        _sessionName = sessionName;
        _launcher = launcher;

        if (roomNameText != null)
            roomNameText.text = sessionName;

        if (playerCountText != null)
            playerCountText.text = $"{playerCount}/{maxPlayers}";

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }
    }

    private void OnJoinClicked()
    {
        if (_launcher != null && !string.IsNullOrWhiteSpace(_sessionName))
        {
            _launcher.JoinSpecificRoom(_sessionName);
        }
    }
}