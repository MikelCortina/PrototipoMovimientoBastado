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
    private bool _isStartingGame = false;
    private bool _callbacksRegistered = false;

    [Header("Configuración")]
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private GameObject menuCanvasRoot;
    [SerializeField] private GameObject endMatchCanvasRoot;
    [SerializeField] private FusionMenuRoomInput roomInput;
    [SerializeField] private FusionRoomListUI roomListUI;

    public async void ConnectToLobby()
    {
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
                _runner = gameObject.AddComponent<NetworkRunner>();
        }

        RegisterRunnerCallbacks();
        _runner.ProvideInput = true;

        try
        {
            await _runner.JoinSessionLobby(SessionLobby.Shared);
            Debug.Log("Conectado al lobby de sesiones.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al conectar al lobby: {e.Message}");
        }
    }

    public async void CreateRoom()
    {
        string roomName = roomInput != null ? roomInput.GetRoomName() : "Sala_1";
        await StartGame(GameMode.Shared, roomName);
    }

    public async void QuickJoinRoom()
    {
        string roomName = roomInput != null ? roomInput.GetRoomName() : "Sala_1";
        await StartGame(GameMode.Shared, roomName);
    }

    public async void JoinSpecificRoom(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
            return;

        await StartGame(GameMode.Shared, sessionName);
    }

    public async void ReturnToMenu()
    {
        Debug.Log("ReturnToMenu pulsado");

        if (endMatchCanvasRoot != null)
            endMatchCanvasRoot.SetActive(false);

        if (menuCanvasRoot != null)
            menuCanvasRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FusionEndMatchUI endUI = FindFirstObjectByType<FusionEndMatchUI>();
        if (endUI != null)
            endUI.ForceHidePanel();

        if (_runner != null)
        {
            try
            {
                await _runner.Shutdown();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error al cerrar la partida: {e.Message}");
            }

            _runner = null;
            _callbacksRegistered = false;
        }

        Debug.Log("Vuelta al menú completada");
    }

    public void ExitGame()
    {
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
        }

        RegisterRunnerCallbacks();
        _runner.ProvideInput = true;

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

            if (menuCanvasRoot != null)
                menuCanvasRoot.SetActive(false);

            if (endMatchCanvasRoot != null)
                endMatchCanvasRoot.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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

    private void RegisterRunnerCallbacks()
    {
        if (_runner == null || _callbacksRegistered)
            return;

        _runner.AddCallbacks(this);
        _callbacksRegistered = true;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
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

            FusionPlayerState playerState = playerObject.GetComponent<FusionPlayerState>();
            if (playerState == null)
                playerState = playerObject.GetComponentInParent<FusionPlayerState>();

            if (playerState != null)
            {
                string localName = FusionPlayerNameStore.Instance != null
                    ? FusionPlayerNameStore.Instance.CurrentPlayerName
                    : "Player";

                playerState.RPC_SetPlayerName(localName);
            }
        }

        UpdateConnectedPlayers(runner);

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.HasStateAuthority &&
            FusionGameState.Instance.connectedPlayers >= 2 &&
            FusionGameState.Instance.currentMatchState == MatchState.Waiting)
        {
            FusionGameState.Instance.StartMatch();
        }
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
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
}