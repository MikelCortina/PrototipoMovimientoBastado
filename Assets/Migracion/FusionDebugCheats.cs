using Fusion;
using UnityEngine;

public class FusionDebugCheats : NetworkBehaviour
{
    private FusionPlayerState playerState;

    public override void Spawned()
    {
        playerState = GetComponent<FusionPlayerState>();
    }

    private void Update()
    {
        if (!HasInputAuthority)
            return;

        if (FusionGameState.Instance == null)
            return;

        if (playerState == null)
            playerState = GetComponent<FusionPlayerState>();

        if (playerState == null)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (playerState.HasStateAuthority)
                playerState.DebugGiveGrenade();
            else
                playerState.RPC_DebugGiveGrenade();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (playerState.HasStateAuthority)
                playerState.DebugGiveAirStrike();
            else
                playerState.RPC_DebugGiveAirStrike();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (playerState.HasStateAuthority)
                playerState.DebugGiveTurret();
            else
                playerState.RPC_DebugGiveTurret();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (playerState.HasStateAuthority)
                playerState.DebugAddKillStreakStep();
            else
                playerState.RPC_DebugAddKillStreakStep();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (playerState.HasStateAuthority)
                playerState.DebugResetStreakOnly();
            else
                playerState.RPC_DebugResetStreakOnly();
        }
    }
}