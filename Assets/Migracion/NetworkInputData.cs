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
    public const int BUTTON_FIRE1 = 2;
    public const int BUTTON_FIRE2 = 3;

    public const int BUTTON_STREAK_GRENADE = 4;   // Z
    public const int BUTTON_STREAK_AIRSTRIKE = 5; // X
    public const int BUTTON_STREAK_TURRET = 6;    // C
}