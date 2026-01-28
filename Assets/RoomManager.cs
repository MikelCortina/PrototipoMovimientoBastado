using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject _player;
    public List<MapData> maps;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Conectando a Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al Master. Uniéndose al Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("En el Lobby. Uniéndose a la sala...");
        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("¡Unido a la sala! Soy el Master: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
        {
            // El Master decide el orden y lo guarda
            ShuffleAndSetMaps();
        }
        else
        {
            // El cliente intenta spawnear. Si las propiedades ya existen, lo hará.
            // Si no, OnRoomPropertiesUpdate se encargará después.
            TrySpawn();
        }
    }

    void ShuffleAndSetMaps()
    {
        List<int> order = new List<int>();
        for (int i = 0; i < maps.Count; i++) order.Add(i);

        // Mezclar
        for (int i = 0; i < order.Count; i++)
        {
            int temp = order[i];
            int rnd = Random.Range(i, order.Count);
            order[i] = order[rnd];
            order[rnd] = temp;
        }

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "MapOrder", order.ToArray() },
            { "CurrentMapIndex", 0 }
        };

        Debug.Log("Master configurando propiedades de mapa...");
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // Se dispara automáticamente cuando el Master hace SetCustomProperties
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Si el Master cambió el mapa, todos corren a spawnear
        if (propertiesThatChanged.ContainsKey("CurrentMapIndex"))
        {
            Debug.Log("Nuevo mapa detectado. Spawneando...");
            TrySpawn();
        }
    }

    private IEnumerator WaitAndSpawn()
    {
        // Esperamos un frame para asegurar que PhotonNetwork.Destroy se haya procesado
        yield return new WaitForEndOfFrame();
        TrySpawn();
    }

    public void TrySpawn()
    {
        // VALIDACIÓN 1: Si ya estoy en proceso de desconexión o fuera de sala, abortar
        if (!PhotonNetwork.InRoom) return;

        // VALIDACIÓN 2: Buscar si ya tengo un objeto mío en la escena
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Debug.Log("Ya tienes un jugador activo. Abortando spawn para evitar duplicados.");
                return;
            }
        }

        // VALIDACIÓN 3: Verificar que los datos existen
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MapOrder") &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CurrentMapIndex"))
        {
            SpawnInCurrentMap();
        }
    }

    public void SpawnInCurrentMap()
    {
        int currentIdx = (int)PhotonNetwork.CurrentRoom.CustomProperties["CurrentMapIndex"];
        int[] order = (int[])PhotonNetwork.CurrentRoom.CustomProperties["MapOrder"];

        MapData currentMap = maps[order[currentIdx]];

        // Master = Spawn 1, Segundo jugador = Spawn 2
        Transform spawnPoint = PhotonNetwork.IsMasterClient ? currentMap.spawn1 : currentMap.spawn2;

        Debug.Log($"Spawneando en Mapa {order[currentIdx]} - Posición: {spawnPoint.position}");
        PhotonNetwork.Instantiate(_player.name, spawnPoint.position, spawnPoint.rotation);
    }
}