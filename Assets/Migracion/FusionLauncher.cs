using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [Header("Configuración")]
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private GameObject menuPanel;

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(10, 10, 220, 40), "Shared (Crear / Unirse)"))
            {
                StartGame(GameMode.Shared);
            }
        }
    }

    async void StartGame(GameMode mode)
    {
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
        }

        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        try
        {
            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = sceneRef,
                SceneManager = gameObject.GetOrAddComponent<NetworkSceneManagerDefault>()
            });

            if (menuPanel != null)
                menuPanel.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Fusion failed to start: {e.Message}");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Jugador {player} se ha unido.");

        if (player == runner.LocalPlayer)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (FusionRespawnManager.Instance != null)
            {
                Transform spawnPoint = FusionRespawnManager.Instance.GetRandomSpawnPoint();
                if (spawnPoint != null)
                {
                    spawnPosition = spawnPoint.position;
                    spawnRotation = spawnPoint.rotation;
                }
            }

            NetworkObject playerObject = runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
            Debug.Log($"Spawn de jugador local realizado para {player}");

            FusionPlayerState playerState = playerObject.GetComponent<FusionPlayerState>();
            if (playerState == null)
                playerState = playerObject.GetComponentInParent<FusionPlayerState>();

            if (playerState != null)
            {
                string localName = "Player";

                if (FusionPlayerNameStore.Instance != null)
                    localName = FusionPlayerNameStore.Instance.CurrentPlayerName;

                playerState.RPC_SetPlayerName(localName);
            }
        }

        UpdateConnectedPlayers(runner);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        UpdateConnectedPlayers(runner);
    }

    private void UpdateConnectedPlayers(NetworkRunner runner)
    {
        if (FusionGameState.Instance == null)
            return;

        if (!FusionGameState.Instance.HasStateAuthority)
            return;

        int count = 0;
        foreach (var p in runner.ActivePlayers)
        {
            count++;
        }

        FusionGameState.Instance.SetConnectedPlayers(count);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var handlers = FindObjectsByType<LocalInputHandler>(FindObjectsSortMode.None);

        foreach (var handler in handlers)
        {
            if (handler.Object != null && handler.Object.Runner == runner && handler.HasInputAuthority)
            {
                input.Set(handler.GetNetworkInput());
                break;
            }
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}