using Fusion;
using UnityEngine;

public class FusionCameraSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public AudioListener audioListener;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
        }
        else
        {
            playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}