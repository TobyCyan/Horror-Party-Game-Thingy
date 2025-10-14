using UnityEngine;

public abstract class TrapBase : MonoBehaviour, ITrap
{
    [Header("Trap")]
    [SerializeField] TrapPlacementKind placement = TrapPlacementKind.Default;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] bool oneShot = false;

    protected GameObject owner;
    protected float lastTriggerTime = -999f;

    public TrapPlacementKind Placement => placement;
    public bool IsDeployed { get; protected set; }
    public bool IsArmed { get; protected set; }
    public float Cooldown => cooldown;
    public bool OneShot => oneShot;

    public event System.Action<ITrap> OnDeployed, OnArmed, OnDisarmed;
    public event System.Action<ITrap, TrapTriggerContext> OnTriggered;
    public static event System.Action<ITrap, TrapTriggerContext> StaticOnTriggered; // help

    // ----- lifecycle -----
    protected virtual void Start()
    {
        if (placement == TrapPlacementKind.Auto || placement == TrapPlacementKind.Default)
        {
            IsDeployed = true;
            Arm();
        }
    }

    public virtual void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        if (placement != TrapPlacementKind.Manual) return;
        transform.SetPositionAndRotation(pos, rot);
        owner = ownerGO;
        gameObject.SetActive(true);
        IsDeployed = true;
        Arm();
        OnDeployed?.Invoke(this);
    }

    public virtual void Arm() { IsArmed = true; OnArmed?.Invoke(this); }
    public virtual void Disarm() { IsArmed = false; OnDisarmed?.Invoke(this); }

    public virtual bool CanTrigger() =>
        IsDeployed && IsArmed && (Time.time - lastTriggerTime) >= cooldown;

    public void Trigger(in TrapTriggerContext ctx)
    {
        if (!CanTrigger()) return;
        lastTriggerTime = Time.time;
        OnTriggered?.Invoke(this, ctx);
        StaticOnTriggered?.Invoke(this, ctx);
        OnTriggerCore(ctx);
        if (oneShot) Destroy(gameObject);
    }

    // implement trap-specific effect (damage, VFX, etc.)
    protected abstract void OnTriggerCore(TrapTriggerContext ctx);
}
