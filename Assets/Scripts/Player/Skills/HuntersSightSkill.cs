using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HuntersSightSkill : PlayerSkill
{
    protected override void Awake()
    {
        base.Awake();
    }

    public override void UseSkill()
    {
        if (coolDownTimer.IsRunning)
        {
            Debug.LogWarning("Skill is on cooldown.");
            return;
        }

        Debug.Log("Hunter's Sight activated!");
        HighlightPlayers();
        durationTimer.StartTimer(duration);
    }

    public override void StopSkill()
    {
        Debug.Log("Hunter's Sight deactivated!");
        // Empty since server automatically stops highlighting after duration
        return;
    }

    private void HighlightPlayers()
    {
        // Logic to highlight enemies
        ulong hunterId = GetComponent<Player>().Id;

        // Highlight all players except the hunter
        List<Player> players = PlayerManager.Instance.AlivePlayers
            .Where(p => p.Id != hunterId).ToList();

        foreach (var player in players)
        {
            if (player.TryGetComponent(out PlayerSilhouette silhouette))
            {
                silhouette.ShowForSeconds_Server(duration);
            }
        }
    }
}
