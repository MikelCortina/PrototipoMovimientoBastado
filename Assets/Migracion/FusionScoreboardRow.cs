using TMPro;
using UnityEngine;

public class FusionScoreboardRow : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public TMP_Text scoreText;

    public void SetData(string playerName, int kills, int deaths, int score)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (killsText != null) killsText.text = kills.ToString();
        if (deathsText != null) deathsText.text = deaths.ToString();
        if (scoreText != null) scoreText.text = score.ToString();
    }
}