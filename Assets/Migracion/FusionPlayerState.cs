using Fusion;
using UnityEngine;

public class FusionPlayerState : NetworkBehaviour
{
    [Networked] public NetworkString<_16> playerName { get; set; }
    [Networked] public int kills { get; set; }
    [Networked] public int deaths { get; set; }
    [Networked] public int score { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            kills = 0;
            deaths = 0;
            score = 0;
        }
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

        Debug.Log($"AddKill | object: {name} | kills: {kills} | score: {score} | HasStateAuthority: {HasStateAuthority}");
    }

    public void AddDeath()
    {
        if (!HasStateAuthority)
            return;

        deaths++;
        score = Mathf.Max(0, score - 50);

        Debug.Log($"AddDeath | object: {name} | deaths: {deaths} | score: {score} | HasStateAuthority: {HasStateAuthority}");
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

    public void ResetStats()
    {
        if (!HasStateAuthority)
            return;

        kills = 0;
        deaths = 0;
        score = 0;
    }
}