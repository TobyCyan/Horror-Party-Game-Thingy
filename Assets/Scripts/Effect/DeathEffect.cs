public class DeathEffect : EffectBase
{
    protected override void ApplyEffect(Player target)
    {
        // Simply eliminate the player
        target.EliminatePlayer();
    }

    protected override void ApplySubscriptions()
    {
        return;
    }
}
