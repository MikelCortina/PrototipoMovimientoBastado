using Photon.Pun;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPunCallbacks
{
    public Movement movement;
    public GameObject playerCamera;

    [Header("Objetos a ocultar para el dueño")]
    public GameObject[] objectsToHide;

    void Start()
    {
        if (photonView.IsMine)
        {
            // Soy yo
            movement.enabled = true;
            playerCamera.SetActive(true);

            // OCULTAR objetos que no quiero ver de mi propio cuerpo
            foreach (GameObject obj in objectsToHide)
            {
                // Opción A: Desactivarlos por completo
                // obj.SetActive(false); 

                // Opción B: Solo ocultar el renderizado (mejor si tienen scripts o huesos)
                if (obj.GetComponent<Renderer>() != null)
                    obj.GetComponent<Renderer>().enabled = false;
            }
        }
        else
        {
            // Es otro jugador
            movement.enabled = false;
            playerCamera.SetActive(false);

            // Asegurarse de que los otros jugadores SI vean mi cuerpo
            foreach (GameObject obj in objectsToHide)
            {
                if (obj.GetComponent<Renderer>() != null)
                    obj.GetComponent<Renderer>().enabled = true;
            }
        }
    }
}