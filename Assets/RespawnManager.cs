using UnityEngine;

using Photon.Pun;

using System.Collections;



public class RespawnManager : MonoBehaviour

{

    public static RespawnManager Instance;



    [Header("Configuración")]

    public string playerPrefabName = "Player"; // El nombre exacto en la carpeta Resources

    public float respawnDelay = 3f;

    public Transform[] spawnPoints;



    void Awake()

    {

        // Singleton simple para acceder desde el HealthSystem

        if (Instance == null) Instance = this;

    }



    public void RespawnPlayer()

    {

        StartCoroutine(RespawnCoroutine());

    }



    private IEnumerator RespawnCoroutine()

    {

        yield return new WaitForSeconds(respawnDelay);



        // Elegir un punto de spawn aleatorio

        Transform selectedPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];



        // Instanciar el nuevo jugador a través de la red

        PhotonNetwork.Instantiate(playerPrefabName, selectedPoint.position, selectedPoint.rotation);

    }

}