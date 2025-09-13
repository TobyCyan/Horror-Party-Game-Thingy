using UnityEngine;

public enum TrapPlacementKind { Manual, Auto, Default }
public enum TrapTriggerSource { Player, Game }

public struct TrapTriggerContext
{
    public TrapTriggerSource source;
    public GameObject instigator;   // player/enemy/game system
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public object payload;         // optional data (e.g., damage scale)
}

public interface ITrap
{
    TrapPlacementKind Placement { get; }
    bool IsDeployed { get; }
    bool IsArmed { get; }
    float Cooldown { get; }      // seconds, 0 = no cooldown
    bool OneShot { get; }      // destroy after first trigger

    void Deploy(Vector3 pos, Quaternion rot, GameObject owner); // Manual only
    void Arm();
    void Disarm();
    bool CanTrigger();
    void Trigger(in TrapTriggerContext ctx);

    event System.Action<ITrap> OnDeployed;
    event System.Action<ITrap> OnArmed;
    event System.Action<ITrap> OnDisarmed;
    event System.Action<ITrap, TrapTriggerContext> OnTriggered;
}
