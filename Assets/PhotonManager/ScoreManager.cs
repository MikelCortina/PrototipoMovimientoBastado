using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance;

    // Constantes para evitar errores de escritura
    public const string P1_SCORE = "P1_Score";
    public const string P2_SCORE = "P2_Score";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // El cliente que muere llama a esto. Solo el Master procesa.
    public void OnPlayerKilled(int deadActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Buscamos al asesino (el que no murió)
        int killerActorNumber = -1;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != deadActorNumber)
            {
                killerActorNumber = p.ActorNumber;
                break;
            }
        }

        if (killerActorNumber != -1)
        {
            AddPoint(killerActorNumber);
        }
    }

    private void AddPoint(int killerActorNumber)
    {
        // Determinamos qué "slot" de puntuación le toca
        // Usamos el ActorNumber del Master como referencia para P1
        string key = (killerActorNumber == PhotonNetwork.MasterClient.ActorNumber) ? P1_SCORE : P2_SCORE;

        int currentScore = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value))
        {
            currentScore = (int)value;
        }

        Hashtable propsToUpdate = new Hashtable { { key, currentScore + 1 } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(propsToUpdate);

        Debug.Log($"Punto para Actor {killerActorNumber}. Key: {key} - Nuevo Score: {currentScore + 1}");
    }
}