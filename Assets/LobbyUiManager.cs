using Unity.Netcode;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    [SerializeField] private GameObject mainLobbyPanel; // El que contiene Join, Create, etc.

    private void Start()
    {
        // Nos suscribimos a los eventos de NetworkManager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnHandleConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnHandleDisconnect;
        }
    }

    private void OnDestroy()
    {
        // Siempre es buena práctica desvincularse al destruir el objeto
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnHandleConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnHandleDisconnect;
        }
    }

    private void OnHandleConnection(ulong clientId)
    {
        // Solo queremos ocultar el menú para el jugador local cuando ÉL se conecta
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            mainLobbyPanel.SetActive(false);
            Debug.Log("Conectado con éxito: Ocultando menú.");
        }
    }

    private void OnHandleDisconnect(ulong clientId)
    {
        // Si el jugador local se desconecta, volvemos a mostrar el menú
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            mainLobbyPanel.SetActive(true);
            Debug.Log("Desconectado: Mostrando menú.");
        }
    }
}