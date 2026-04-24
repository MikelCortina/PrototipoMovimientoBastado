using TMPro;
using UnityEngine;

public class FusionEndMatchUI : MonoBehaviour
{
    [SerializeField] private GameObject endMatchPanel;
    [SerializeField] private TMP_Text winnerText;

    private bool wasFinishedLastFrame = false;

    private void Start()
    {
        if (endMatchPanel != null)
            endMatchPanel.SetActive(false);
    }

    private void Update()
    {
        if (FusionGameState.Instance == null)
            return;

        if (!FusionGameState.Instance.Object || !FusionGameState.Instance.Object.IsValid)
            return;

        if (endMatchPanel == null)
            return;

        bool isFinished = FusionGameState.Instance.currentMatchState == MatchState.Finished;

        if (isFinished && !wasFinishedLastFrame)
        {
            endMatchPanel.SetActive(true);

            string winner = FusionGameState.Instance.winnerName.ToString();
            if (string.IsNullOrWhiteSpace(winner))
                winner = "Sin ganador";

            if (winnerText != null)
                winnerText.text = $"Ganador: {winner}";

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!isFinished && wasFinishedLastFrame)
        {
            endMatchPanel.SetActive(false);
        }

        wasFinishedLastFrame = isFinished;
    }

    public void ForceHidePanel()
    {
        if (endMatchPanel != null)
            endMatchPanel.SetActive(false);

        wasFinishedLastFrame = false;
    }
}