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

    // Network variable to sync color state
    private NetworkVariable<bool> isArmedVisual = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    protected override void Start()
    {
        base.Start();
        blindEffect = GetComponent<BlindEffect>();

        if (trapRenderer != null)
        {
            trapMaterial = trapRenderer.material;
            UpdateVisualColor();
        }

        // Subscribe to lifecycle events
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        // Subscribe to network variable changes
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            isArmedVisual.OnValueChanged += OnArmedVisualChanged;
        }
    }

    private void OnDestroy()
    {
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            isArmedVisual.OnValueChanged -= OnArmedVisualChanged;
        }
    }

    private void OnArmedVisualChanged(bool previous, bool current)
    {
        UpdateVisualColor();
    }

    private void UpdateVisualColor()
    {
        if (trapMaterial != null)
        {
            trapMaterial.color = isArmedVisual.Value ? armedColor : disarmedColor;
        }
    }

    private void HandleArmed(ITrap trap)
    {
        // Update network variable on server
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            isArmedVisual.Value = true;
        }
        Debug.Log("BlindTrap: armed, color changed to red");
    }

    private void HandleDisarmed(ITrap trap)
    {
        // Update network variable on server
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            isArmedVisual.Value = false;
        }
        Debug.Log("BlindTrap: disarmed, color changed to gray");
    }

    public void HandleTriggerEnter(Collider other)
    {
        Debug.Log($"BlindTrap: HandleTriggerEnter by {other.gameObject.name}");
        if (!CanTrigger()) return;
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

        Debug.Log($"BlindTrap: triggered by {other.gameObject.name}");
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
        var player = ctx.instigator.GetComponentInParent<Player>();
        if (player == null) return;

        // blindEffect.Apply(player); // Local?
        Debug.Log($"BlindTrap: blinded {ctx.instigator.name}");

        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
        }

        if (OneShot)
        {
            Disarm();
        }
    }

    public override void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        base.Deploy(pos, rot, ownerGO);

        // Set initial visual state on server
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            isArmedVisual.Value = true;
        }

        Debug.Log("BlindTrap: deployed and armed visually");
    }
}