using Fusion;
using UnityEngine;

public class FusionCameraHandler : NetworkBehaviour
{
    [Header("Ajustes de Ratón")]
    public float mouseSensitivity = 2f;
    public float maxPitch = 89f;
    public float minPitch = -89f;

    private float _yaw;
    private float _pitch;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
        }
    }

    void Update()
    {
        if (!HasInputAuthority)
            return;

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState == MatchState.Finished)
        {
            return;
        }

        _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    public Vector2 GetLookRotation()
    {
        return new Vector2(_yaw, _pitch);
    }

    public void SetCursorGameplayState(bool gameplayActive)
    {
        if (!HasInputAuthority)
            return;

        Cursor.lockState = gameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplayActive;
    }
}