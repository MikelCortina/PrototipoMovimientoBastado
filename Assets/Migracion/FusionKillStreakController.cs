using Fusion;
using UnityEngine;

public class FusionKillStreakController : NetworkBehaviour
{
    private FusionPlayerState playerState;
    private NetworkButtons previousButtons;

    public override void Spawned()
    {
        playerState = GetComponent<FusionPlayerState>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        if (FusionGameState.Instance != null &&
            FusionGameState.Instance.currentMatchState != MatchState.Playing)
            return;

        if (playerState == null)
            playerState = GetComponent<FusionPlayerState>();

        if (playerState == null)
            return;

        var pressed = input.buttons.GetPressed(previousButtons);

        if (pressed.IsSet(NetworkInputData.BUTTON_STREAK_GRENADE))
        {
            TryUseGrenadeStreak();
        }

        if (pressed.IsSet(NetworkInputData.BUTTON_STREAK_AIRSTRIKE))
        {
            TryUseAirStrikeStreak();
        }

        if (pressed.IsSet(NetworkInputData.BUTTON_STREAK_TURRET))
        {
            TryUseTurretStreak();
        }

        previousButtons = input.buttons;
    }

    private void TryUseGrenadeStreak()
    {
        if (!playerState.hasGrenadeStreak)
            return;

        if (FusionGameState.Instance == null)
            return;

        if (Object == null || !Object.IsValid)
            return;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 forward = transform.forward;

        FusionGameState.Instance.RPC_RequestGrenadeUse(Object.Id, origin, forward);

        Debug.Log($"{playerState.playerName} ha usado la racha: Granada de pintura");
    }

    private void TryUseAirStrikeStreak()
    {
        if (!playerState.hasAirStrikeStreak)
            return;

        if (FusionGameState.Instance == null)
            return;

        if (Object == null || !Object.IsValid)
            return;

        FusionGameState.Instance.RPC_RequestAirStrikeUse(Object.Id);

        Debug.Log($"{playerState.playerName} ha usado la racha: Ataque aéreo");
    }
    private void TryUseTurretStreak()
    {
        if (!playerState.hasTurretStreak)
            return;

        if (FusionGameState.Instance == null)
            return;

        if (Object == null || !Object.IsValid)
            return;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        FusionGameState.Instance.RPC_RequestTurretUse(Object.Id, origin, forward);

        Debug.Log($"{playerState.playerName} ha usado la racha: Torreta");
    }
}