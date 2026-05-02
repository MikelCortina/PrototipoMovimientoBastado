using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FusionScoreboardUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject scoreboardPanel;
    public Transform rowsContainer;
    public FusionScoreboardRow rowPrefab;

    private readonly List<FusionScoreboardRow> _spawnedRows = new List<FusionScoreboardRow>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("TAB detectado");
        }

        if (scoreboardPanel == null)
        {
            Debug.LogWarning("scoreboardPanel es NULL");
            return;
        }

        bool show = Input.GetKey(KeyCode.Tab);

        if (scoreboardPanel.activeSelf != show)
        {
            Debug.Log("Cambio estado panel: " + show);
        }

        scoreboardPanel.SetActive(show);

        if (show)
        {
            RefreshScoreboard();
        }
    }

    private void RefreshScoreboard()
    {
        ClearRows();

        FusionPlayerState[] players = FindObjectsByType<FusionPlayerState>(FindObjectsSortMode.None);

        List<FusionPlayerState> playerList = new List<FusionPlayerState>(players);

        playerList.Sort((a, b) =>
        {
            if (b.score != a.score)
                return b.score.CompareTo(a.score);

            if (b.kills != a.kills)
                return b.kills.CompareTo(a.kills);

            return a.deaths.CompareTo(b.deaths);
        });

        foreach (FusionPlayerState player in playerList)
        {
            if (player == null || !player.Object || !player.Object.IsValid)
                continue;

            if (rowPrefab == null || rowsContainer == null)
            {
                Debug.LogWarning("rowPrefab o rowsContainer es NULL");
                return;
            }

            FusionScoreboardRow row = Instantiate(rowPrefab, rowsContainer);

            string displayName = player.playerName.ToString();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = player.name;

            row.SetData(displayName, player.kills, player.deaths, player.score);
            _spawnedRows.Add(row);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }

        _spawnedRows.Clear();
    }
}