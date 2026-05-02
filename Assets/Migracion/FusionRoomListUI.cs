using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FusionRoomListUI : MonoBehaviour
{
    public Transform rowsContainer;
    public FusionRoomListRow rowPrefab;

    private readonly List<FusionRoomListRow> _spawnedRows = new List<FusionRoomListRow>();

    public void RefreshRooms(List<SessionInfo> sessions, FusionLauncher launcher)
    {
        ClearRows();

        if (sessions == null || rowPrefab == null || rowsContainer == null)
            return;

        foreach (var session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible)
                continue;

            FusionRoomListRow row = Instantiate(rowPrefab, rowsContainer);

            int currentPlayers = session.PlayerCount;
            int maxPlayers = session.MaxPlayers;

            row.SetData(session.Name, currentPlayers, maxPlayers, launcher);
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