using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction;
    public NetworkButtons buttons;
    public float yaw;
    public float pitch;

    public const int BUTTON_JUMP = 0;
    public const int BUTTON_DASH = 1;
    public const int BUTTON_FIRE1 = 2; // Clic Izquierdo (Disparo normal)
    public const int BUTTON_FIRE2 = 3; // Clic Derecho (Escopeta)
}