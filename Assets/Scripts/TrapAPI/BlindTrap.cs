using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BlindEffect))]
public class BlindTrap : TrapBase
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float silhouetteDuration = 5f;

    [Header("Visuals")]
    [SerializeField] private Renderer trapRenderer;
    [SerializeField] private Color armedColor = Color.red;
    [SerializeField] private Color disarmedColor = Color.gray;

    private BlindEffect blindEffect;
    private Material trapMaterial;

    protected override void Start()
    {
        base.Start();

        blindEffect = GetComponent<BlindEffect>();

        if (trapRenderer != null)
        {
            trapMaterial = trapRenderer.material;
        }

        // Subscribe to lifecycle events
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        // IMPORTANT: Check current state and update visual immediately
        UpdateVisualColor();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Update visual when we spawn on network
        if (trapRenderer != null && trapMaterial == null)
        {
            trapMaterial = trapRenderer.material;
        }

        // Check state after network spawn
        UpdateVisualColor();
    }

    private void OnDestroy()
    {
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;
    }

    private void UpdateVisualColor()
    {
        if (trapMaterial != null)
        {
            // Use IsArmed property which checks the NetworkVariable
            trapMaterial.color = IsArmed ? armedColor : disarmedColor;
            Debug.Log($"[BlindTrap] Visual updated - IsArmed: {IsArmed}, IsDeployed: {IsDeployed}, Color: {(IsArmed ? "Red" : "Gray")}");
        }
    }

    private void HandleArmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log("[BlindTrap] HandleArmed called - color should be red now");
    }

    private void HandleDisarmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log("[BlindTrap] HandleDisarmed called - color should be gray now");
    }

    // OnTriggerEnter should be on the trap's collider
    private void OnTriggerEnter(Collider other)
    {
        HandleTriggerEnter(other);
    }

    public void HandleTriggerEnter(Collider other)
    {
        // Only process on server
        if (!IsServer) return;

        Debug.Log($"[BlindTrap] Trigger entered by {other.gameObject.name} - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}, CanTrigger: {CanTrigger()}");

        if (!CanTrigger()) return;
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

        Debug.Log($"[BlindTrap] Triggering trap for {other.gameObject.name}");

        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player,
            instigator = other.gameObject,
            hitPoint = other.ClosestPoint(transform.position),
            hitNormal = Vector3.up
        };

        Trigger(ctx);
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[BlindTrap] OnTriggerCore called on client!");
            return;
        }

        var player = ctx.instigator.GetComponentInParent<Player>();
        if (player == null) return;

        blindEffect.Apply(player);
        Debug.Log($"[BlindTrap] Applied blind effect to {ctx.instigator.name}");

        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
        }

        if (OneShot)
        {
            Disarm();
        }
    }
}