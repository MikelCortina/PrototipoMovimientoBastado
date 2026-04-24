using Fusion;
using UnityEngine;

// 1. Ahora hereda de NetworkBehaviour
public class LocalInputHandler : NetworkBehaviour
{
    private FusionCameraHandler _cameraHandler;

    public override void Spawned()
    {
        _cameraHandler = GetComponent<FusionCameraHandler>();
    }

    public NetworkInputData GetNetworkInput()
    {
        var data = new NetworkInputData();

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState == MatchState.Finished)
        {
            return data;
        }

        if (_cameraHandler != null)
        {
            var look = _cameraHandler.GetLookRotation();
            data.yaw = look.x;
            data.pitch = look.y;
        }

        data.direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        data.buttons.Set(NetworkInputData.BUTTON_JUMP, Input.GetKey(KeyCode.Space));
        data.buttons.Set(NetworkInputData.BUTTON_DASH, Input.GetKey(KeyCode.LeftShift));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE1, Input.GetButton("Fire1"));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE2, Input.GetButton("Fire2"));

        data.buttons.Set(NetworkInputData.BUTTON_STREAK_GRENADE, Input.GetKey(KeyCode.Z));
        data.buttons.Set(NetworkInputData.BUTTON_STREAK_AIRSTRIKE, Input.GetKey(KeyCode.X));
        data.buttons.Set(NetworkInputData.BUTTON_STREAK_TURRET, Input.GetKey(KeyCode.C));

        return data;
    }
}