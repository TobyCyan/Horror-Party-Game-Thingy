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
        HighlightEnemies();
        durationTimer.StartTimer(duration);
    }

    public override void StopSkill()
    {
        Debug.Log("Hunter's Sight deactivated!");
        RevertHighlighting();
    }

    private void HighlightEnemies()
    {
        // Logic to highlight enemies
    }

    private void RevertHighlighting()
    {
        // Logic to revert highlighting
    }
}
