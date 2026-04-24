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
    [SerializeField] private FusionMenuRoomInput roomInput;
    [SerializeField] private FusionRoomListUI roomListUI;

    private bool _isStartingGame = false;
    private bool _isInLobby = false;

    public async void ConnectToLobby()
    {
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
                _runner = gameObject.AddComponent<NetworkRunner>();
        }

        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;

        try
        {
            await _runner.JoinSessionLobby(SessionLobby.Shared);
            _isInLobby = true;
            Debug.Log("Conectado al lobby de sesiones.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al conectar al lobby: {e.Message}");
        }
    }

    public async void CreateRoom()
    {
        string roomName = "Sala_1";

        if (roomInput != null)
            roomName = roomInput.GetRoomName();

        await StartGame(GameMode.Shared, roomName);
    }

    public async void QuickJoinRoom()
    {
        string roomName = "Sala_1";

        if (roomInput != null)
            roomName = roomInput.GetRoomName();

        await StartGame(GameMode.Shared, roomName);
    }

    public async void JoinSpecificRoom(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
            return;

        await StartGame(GameMode.Shared, sessionName);
    }

    public async void LeaveRoom()
    {
        if (_runner == null)
            return;

        try
        {
            await _runner.Shutdown();
            _runner = null;
            _isInLobby = false;

            if (menuPanel != null)
                menuPanel.SetActive(true);

            Debug.Log("Has salido de la sala.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al salir de la sala: {e.Message}");
        }
    }

    public void ExitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private async System.Threading.Tasks.Task StartGame(GameMode mode, string sessionName)
    {
        if (_isStartingGame)
            return;

        _isStartingGame = true;

        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
                _runner = gameObject.AddComponent<NetworkRunner>();

            _runner.AddCallbacks(this);
            _runner.ProvideInput = true;
        }

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        try
        {
            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                Scene = sceneRef,
                SceneManager = gameObject.GetOrAddComponent<NetworkSceneManagerDefault>()
            });

            _isInLobby = false;

            if (menuPanel != null)
                menuPanel.SetActive(false);

            Debug.Log($"Partida iniciada en la sala: {sessionName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Fusion failed to start: {e.Message}");
        }
        finally
        {
            _isStartingGame = false;
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
            count++;

        FusionGameState.Instance.SetConnectedPlayers(count);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Salas disponibles: {sessionList.Count}");

        if (roomListUI != null)
            roomListUI.RefreshRooms(sessionList, this);
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