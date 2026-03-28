using Photon.Pun;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPunCallbacks
{
    public Movement movement;
    public GameObject playerCamera;
    public AudioSource playerAudio; // Tu AudioSource principal (ej. música o sonidos locales)
    public GameObject legs; // Modelo de las piernas del jugador

    [Header("Objetos a ocultar para el dueño")]
    public GameObject[] objectsToHide;

    void Start()
    {
        SetupPlayer();
    }

    public void SetupPlayer()
    {
        if (photonView.IsMine)
        {
            // --- CONFIGURACIÓN PARA EL JUGADOR LOCAL ---
            movement.enabled = true;
            playerCamera.SetActive(true);
            legs.SetActive(true);

            // Asegurar que mi cámara sea la principal y tenga el ÚNICO listener activo
            playerCamera.GetComponent<Camera>().tag = "MainCamera";

            AudioListener myListener = playerCamera.GetComponent<AudioListener>();
            if (myListener != null) myListener.enabled = true;

            // El AudioSource local suele ser 2D para que el jugador lo escuche claro
            if (playerAudio != null) playerAudio.spatialBlend = 0f;

            foreach (GameObject obj in objectsToHide)
            {
                if (obj.GetComponent<SkinnedMeshRenderer>() != null)
                    obj.GetComponent<SkinnedMeshRenderer>().enabled = false;
            }
        }
        else
        {
            movement.enabled = false;
            // --- CONFIGURACIÓN PARA LOS CLONES (OTROS JUGADORES) ---
            movement.enabled = false;
            playerCamera.SetActive(false);
            legs.SetActive(false);

            // 1. DESACTIVAR el Listener de los demás para no oír desde su posición
            AudioListener otherListener = playerCamera.GetComponent<AudioListener>();
            if (otherListener != null) otherListener.enabled = false;

            // 2. CONFIGURAR AudioSource como 3D
            // Esto hace que si el otro jugador emite un sonido, lo oigas venir desde su posición física
            if (playerAudio != null)
            {
                playerAudio.spatialBlend = 1f; // 1.0 = 3D (Sonido posicional)
                playerAudio.rolloffMode = AudioRolloffMode.Linear; // El sonido baja con la distancia
                playerAudio.minDistance = 1f;
                playerAudio.maxDistance = 20f;
            }

            foreach (GameObject obj in objectsToHide)
            {
                if (obj.GetComponent<SkinnedMeshRenderer>() != null)
                    obj.GetComponent<SkinnedMeshRenderer>().enabled = true;
            }
        }
    }
}