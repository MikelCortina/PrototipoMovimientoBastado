using Photon.Pun;
using System.Globalization;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPunCallbacks
{
    public Movement movement;
    public GameObject playerCamera; // Cambié el nombre porque 'camera' es palabra reservada en versiones viejas
    [SerializeField] GameObject localHiddenObject;

    void Start()
    {
        // photonView.IsMine nos dice si este objeto es EL MÍO o el de otro jugador
        if (photonView.IsMine)
        {
            // Soy yo: Activo mi movimiento y mi cámara
            movement.enabled = true;
            playerCamera.SetActive(true);
            localHiddenObject.SetActive(false);
        }
        else
        {
            // Es otro jugador: Me aseguro de que su movimiento y cámara estén APAGADOS en mi pantalla
            movement.enabled = false;
            playerCamera.SetActive(false);
        }

    }
}