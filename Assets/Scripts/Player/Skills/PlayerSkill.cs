using UnityEngine;

public abstract class PlayerSkill : MonoBehaviour
{
    [Min(0f)]
    [SerializeField] protected float duration = 5.0f;
    public float Duration => duration;

    [Header("Cooldown Must Be Larger Than Duration.")]
    [Min(0f)]
    [SerializeField] protected float cooldown = 20.0f;
    public float Cooldown => cooldown;

    // Skill timers
    protected Timer durationTimer;
    protected Timer coolDownTimer;
    public Timer CoolDownTimer => coolDownTimer;

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
    }

    protected virtual void Start()
    {
        if (SkillRegistry.Instance != null)
        {
            SkillRegistry.Instance.RegisterSkill(this);
        }
        else
        {
            Debug.LogError("SkillRegistry instance not found in the scene.");
        }
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
