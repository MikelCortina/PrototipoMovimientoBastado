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
    [SerializeField] private NetworkObject _playerPrefab; // Arrastra aquí tu prefab 'FusionPlayer'

    // Botón para la UI o lo puedes llamar en el Start para pruebas
    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(10, 10, 200, 40), "Host (Crear Partida)"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(10, 60, 200, 40), "Join (Unirse)"))
            {
                StartGame(GameMode.Client);
            }
        }
    }

    async void StartGame(GameMode mode)
    {
        // 1. Check if we already have a runner on this object to avoid the "already added" error
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
        }

        _runner.ProvideInput = true;

        // 2. Scene reference for Fusion 2
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        // 3. Start the game
        try
        {
            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = sceneRef,
                SceneManager = gameObject.GetOrAddComponent<NetworkSceneManagerDefault>()
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Fusion failed to start: {e.Message}");
        }
    }

    // --- CALLBACK: Se ejecuta cuando un jugador entra ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Solo el Host/Servidor tiene permiso para Spawnear objetos
        if (runner.IsServer)
        {
            Debug.Log($"Jugador {player} se ha unido. Spawneando cubo...");

            // Spawneamos en una posición aleatoria para que no se pisen
            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-2, 2), 2, UnityEngine.Random.Range(-2, 2));

            runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
        }
    }

    // --- Métodos obligatorios de la interfaz (Vacíos) ---
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Busca TODOS los handlers en la escena
        var handlers = FindObjectsByType<LocalInputHandler>(FindObjectsSortMode.None);

        foreach (var handler in handlers)
        {
            // Solo extraemos el input si:
            // 1. El objeto es válido.
            // 2. Pertenece a este Runner (vital si pruebas 2 jugadores en el mismo Unity).
            // 3. Nosotros somos los dueños (InputAuthority).
            if (handler.Object != null && handler.Object.Runner == runner && handler.HasInputAuthority)
            {
                input.Set(handler.GetNetworkInput());
                break; // Ya encontramos el nuestro, dejamos de buscar
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
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}
