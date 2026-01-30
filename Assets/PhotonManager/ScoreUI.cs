using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;

public class ScoreUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text scoreText;

    public override void OnEnable()
    {
        base.OnEnable();
        UpdateScoreDisplay();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(ScoreManager.P1_SCORE) ||
            propertiesThatChanged.ContainsKey(ScoreManager.P2_SCORE))
        {
            UpdateScoreDisplay();
        }
    }

    private void UpdateScoreDisplay()
    {
        if (!PhotonNetwork.InRoom) return;

        int p1 = GetProp(ScoreManager.P1_SCORE);
        int p2 = GetProp(ScoreManager.P2_SCORE);

        scoreText.text = $"P1: {p1} | P2: {p2}";
    }

    private int GetProp(string key)
    {
        return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object val) ? (int)val : 0;
    }
}