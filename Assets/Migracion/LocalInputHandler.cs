using Fusion;
using UnityEngine;

public class LocalInputHandler : MonoBehaviour
{
    private FusionCameraHandler _cameraHandler; // Asumo que tienes este script para la rotación

    void Awake()
    {
        _cameraHandler = GetComponent<FusionCameraHandler>();
    }

    public NetworkInputData GetNetworkInput()
    {
        var data = new NetworkInputData();
        if (_cameraHandler == null) return data;

        var look = _cameraHandler.GetLookRotation();

        data.direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        data.yaw = look.x;
        data.pitch = look.y;

        data.buttons.Set(NetworkInputData.BUTTON_JUMP, Input.GetKey(KeyCode.Space));
        data.buttons.Set(NetworkInputData.BUTTON_DASH, Input.GetKey(KeyCode.LeftShift));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE1, Input.GetButton("Fire1"));
        data.buttons.Set(NetworkInputData.BUTTON_FIRE2, Input.GetButton("Fire2"));

        return data;
    }
}