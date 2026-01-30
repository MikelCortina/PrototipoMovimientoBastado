using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject _player; // Asegúrate de que este prefab esté en Assets/Resources/
    public List<MapData> maps;

    [Space]
    public GameObject roomCam;
    public GameObject roundEndCam;

    void Start()
    {
        // Si ya estamos en una sala (porque Matchmaking nos trajo aquí)
        if (PhotonNetwork.InRoom)
        {
            PrepareGame();
        }
    }

    void PrepareGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master: Configurando partida...");
            ShuffleAndSetMaps();
        }
        else
        {
            // Si soy cliente, espero a que el Master ponga las propiedades
            CheckPropsAndSpawn();
        }
    }

    void ShuffleAndSetMaps()
    {
        List<int> order = new List<int>();
        for (int i = 0; i < maps.Count; i++) order.Add(i);

        // Mezcla rápida (Fisher-Yates)
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

        // Al usar SetCustomProperties, OnRoomPropertiesUpdate se disparará para todos
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("CurrentMapIndex"))
        {
            TrySpawn();
        }
    }

    private void CheckPropsAndSpawn()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CurrentMapIndex"))
        {
            TrySpawn();
        }
    }

    public void TrySpawn()
    {
        // 1. Evitar duplicados
        if (GameObject.FindGameObjectWithTag("Player") != null) return;

        // 2. Apagar cámaras de espera
        if (roomCam) roomCam.SetActive(false);
        if (roundEndCam) roundEndCam.SetActive(false);

        // 3. Spawnear
        SpawnInCurrentMap();
    }

    public void SpawnInCurrentMap()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        // El casteo de arrays en Photon requiere cuidado (a veces llega como int[] y otras como object[])
        int[] order = props["MapOrder"] as int[];
        int currentIdx = (int)props["CurrentMapIndex"];

        MapData currentMap = maps[order[currentIdx]];
        Transform spawnPoint = PhotonNetwork.IsMasterClient ? currentMap.spawn1 : currentMap.spawn2;

        // IMPORTANTE: El nombre del prefab sin la ruta "Resources/"
        PhotonNetwork.Instantiate(_player.name, spawnPoint.position, spawnPoint.rotation);
    }
}