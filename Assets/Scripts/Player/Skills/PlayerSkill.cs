using UnityEngine;

public abstract class PlayerSkill : MonoBehaviour
{
    [SerializeField] protected float duration = 5.0f;
    public float Duration => duration;

    [SerializeField] protected float cooldown = 20.0f;
    public float Cooldown => cooldown;

    // Skill timers
    protected Timer durationTimer;
    protected Timer coolDownTimer;

    protected virtual void Awake()
    {
        if (durationTimer == null)
        {
            durationTimer = gameObject.AddComponent<Timer>();
        }

        durationTimer.OnTimeUp += StopSkill;

        if (coolDownTimer == null)
        {
            coolDownTimer = gameObject.AddComponent<Timer>();
        }

        // When duration ends, start cooldown
        durationTimer.OnTimeUp += StartCoolDownTimer;
    }

    protected virtual void OnDestroy()
    {
        if (durationTimer != null)
        {
            durationTimer.OnTimeUp -= StopSkill;
            durationTimer.OnTimeUp -= StartCoolDownTimer;
        }
    }

    private void StartCoolDownTimer()
    {
        coolDownTimer.StartTimer(cooldown);
    }

    public abstract void UseSkill();
    public abstract void StopSkill();
}
