using Fusion;
using UnityEngine;

public class FusionPlayerState : NetworkBehaviour
{
    [Networked] public NetworkString<_16> playerName { get; set; }
    [Networked] public int kills { get; set; }
    [Networked] public int deaths { get; set; }
    [Networked] public int score { get; set; }

    [Networked] public NetworkBool grenadeRewardGrantedThisStreak { get; set; }
    [Networked] public NetworkBool airStrikeRewardGrantedThisStreak { get; set; }
    [Networked] public NetworkBool turretRewardGrantedThisStreak { get; set; }

    [Networked] public int killStreak { get; set; }
    [Networked] public NetworkBool hasGrenadeStreak { get; set; }
    [Networked] public NetworkBool hasAirStrikeStreak { get; set; }
    [Networked] public NetworkBool hasTurretStreak { get; set; }


    public void DebugGiveGrenade()
    {
        if (!HasStateAuthority)
            return;

        hasGrenadeStreak = true;
        grenadeRewardGrantedThisStreak = true;

        Debug.Log($"DEBUG | {playerName} recibió Granada");
    }

    public void DebugGiveAirStrike()
    {
        if (!HasStateAuthority)
            return;

        hasAirStrikeStreak = true;
        airStrikeRewardGrantedThisStreak = true;

        Debug.Log($"DEBUG | {playerName} recibió Ataque Aéreo");
    }

    public void DebugGiveTurret()
    {
        if (!HasStateAuthority)
            return;

        hasTurretStreak = true;
        turretRewardGrantedThisStreak = true;

        Debug.Log($"DEBUG | {playerName} recibió Torreta");
    }

    public void DebugAddKillStreakStep()
    {
        if (!HasStateAuthority)
            return;

        AddKill();
    }

    public void DebugResetStreakOnly()
    {
        if (!HasStateAuthority)
            return;

        ResetKillStreakRewards();

        Debug.Log($"DEBUG | {playerName} reseteó racha");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DebugGiveGrenade()
    {
        if (!HasStateAuthority)
            return;

        DebugGiveGrenade();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DebugGiveAirStrike()
    {
        if (!HasStateAuthority)
            return;

        DebugGiveAirStrike();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DebugGiveTurret()
    {
        if (!HasStateAuthority)
            return;

        DebugGiveTurret();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DebugAddKillStreakStep()
    {
        if (!HasStateAuthority)
            return;

        DebugAddKillStreakStep();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DebugResetStreakOnly()
    {
        if (!HasStateAuthority)
            return;

        DebugResetStreakOnly();
    }
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            kills = 0;
            deaths = 0;
            score = 0;

            killStreak = 0;
            hasGrenadeStreak = false;
            hasAirStrikeStreak = false;
            hasTurretStreak = false;

            grenadeRewardGrantedThisStreak = false;
            airStrikeRewardGrantedThisStreak = false;
            turretRewardGrantedThisStreak = false;
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowStreakUnlockedMessage(string message)
    {
        if (FusionStreakMessageUI.Instance != null)
            FusionStreakMessageUI.Instance.ShowMessage(message);
    }

    public void SetPlayerName(string newName)
    {
        if (!HasStateAuthority)
            return;

        if (string.IsNullOrWhiteSpace(newName))
            newName = "Player";

        playerName = newName.Trim();
        Debug.Log($"SetPlayerName | object: {name} | playerName: {playerName} | HasStateAuthority: {HasStateAuthority}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string newName)
    {
        if (!HasStateAuthority)
            return;

        SetPlayerName(newName);
    }

    public void AddKill()
    {
        if (!HasStateAuthority)
            return;

        kills++;
        score += 100;
        killStreak++;

        if (killStreak >= 3 && !grenadeRewardGrantedThisStreak)
        {
            hasGrenadeStreak = true;
            grenadeRewardGrantedThisStreak = true;
            RPC_ShowStreakUnlockedMessage("Granada desbloqueada - pulsa Z");
        }

        if (killStreak >= 5 && !airStrikeRewardGrantedThisStreak)
        {
            hasAirStrikeStreak = true;
            airStrikeRewardGrantedThisStreak = true;
            RPC_ShowStreakUnlockedMessage("Ataque aéreo desbloqueado - pulsa X");
        }

        if (killStreak >= 10 && !turretRewardGrantedThisStreak)
        {
            hasTurretStreak = true;
            turretRewardGrantedThisStreak = true;
            RPC_ShowStreakUnlockedMessage("Torreta desbloqueada - pulsa C");
        }

        Debug.Log($"AddKill | object: {name} | kills: {kills} | score: {score} | streak: {killStreak} | grenade: {hasGrenadeStreak} | air: {hasAirStrikeStreak} | turret: {hasTurretStreak} | HasStateAuthority: {HasStateAuthority}");
    }

    public void AddDeath()
    {
        if (!HasStateAuthority)
            return;

        deaths++;
        score = Mathf.Max(0, score - 50);

        ResetKillStreakRewards();

        Debug.Log($"AddDeath | object: {name} | deaths: {deaths} | score: {score} | streak reset | HasStateAuthority: {HasStateAuthority}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddKill()
    {
        if (!HasStateAuthority)
            return;

        AddKill();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddDeath()
    {
        if (!HasStateAuthority)
            return;

        AddDeath();
    }

    public void ConsumeGrenadeStreak()
    {
        if (!HasStateAuthority)
            return;

        hasGrenadeStreak = false;
    }

    public void ConsumeAirStrikeStreak()
    {
        if (!HasStateAuthority)
            return;

        hasAirStrikeStreak = false;
    }

    public void ConsumeTurretStreak()
    {
        if (!HasStateAuthority)
            return;

        hasTurretStreak = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConsumeGrenadeStreak()
    {
        if (!HasStateAuthority)
            return;

        ConsumeGrenadeStreak();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConsumeAirStrikeStreak()
    {
        if (!HasStateAuthority)
            return;

        ConsumeAirStrikeStreak();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConsumeTurretStreak()
    {
        if (!HasStateAuthority)
            return;

        ConsumeTurretStreak();
    }

    public void ResetKillStreakRewards()
    {
        if (!HasStateAuthority)
            return;

        killStreak = 0;
        hasGrenadeStreak = false;
        hasAirStrikeStreak = false;
        hasTurretStreak = false;

        grenadeRewardGrantedThisStreak = false;
        airStrikeRewardGrantedThisStreak = false;
        turretRewardGrantedThisStreak = false;
    }
    public void ResetStats()
    {
        if (!HasStateAuthority)
            return;

        kills = 0;
        deaths = 0;
        score = 0;

        ResetKillStreakRewards();
    }
}
