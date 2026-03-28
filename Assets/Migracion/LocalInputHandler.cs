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

        // 2. Si el sistema de cámara está listo, leemos la rotación
        if (_cameraHandler != null)
        {
            var look = _cameraHandler.GetLookRotation();
            data.yaw = look.x;
            data.pitch = look.y;
        }

        // Leemos teclas (GetAxisRaw es vital para que no haya suavizados raros de Unity)
        data.direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        data.buttons.Set(NetworkInputData.BUTTON_JUMP, Input.GetKey(KeyCode.Space));
        data.buttons.Set(NetworkInputData.BUTTON_DASH, Input.GetKey(KeyCode.LeftShift));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE1, Input.GetButton("Fire1"));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE2, Input.GetButton("Fire2"));

        return data;
    }
}