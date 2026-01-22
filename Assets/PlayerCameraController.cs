using Unity.Netcode;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour
{
    public Camera playerCamera;

    void Start()
    {
        if (!IsOwner)
        {
            playerCamera.enabled = false;
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }
    }
}
