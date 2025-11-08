// SlowTrap.cs - Updated to use player's BlindEffect

using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Serialization;

public class SlowTrap : TrapBase
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask playerMask;
    [FormerlySerializedAs("blindDuration")] [SerializeField] private float duration = 5f;
    [SerializeField] private float silhouetteDuration = 5f;

    [Header("Visuals")]
    [SerializeField] private Renderer trapRenderer;
    [SerializeField] private Material armedMaterial;
    [SerializeField] private Material disarmedMaterial;

    protected override void Awake()
    {
        base.Awake();

        if (trapRenderer != null)
        {
            trapRenderer.material = disarmedMaterial;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Subscribe to lifecycle events
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        // Initial color update
        UpdateVisualColor();

        Debug.Log($"[SlowTrap] Start complete");
    }

    private void OnDestroy()
    {
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;
    }

    private void HandleArmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log($"[SlowTrap] HandleArmed - Updating color to RED");
    }

    private void HandleDisarmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log($"[SlowTrap] HandleDisarmed - Updating color to GRAY");
    }

    private void UpdateVisualColor()
    {
        if (trapRenderer != null)
        {
            Material targetMaterial = IsArmed ? armedMaterial : disarmedMaterial;
            trapRenderer.material = targetMaterial;
            Debug.Log($"[SlowTrap] Color updated to {(IsArmed ? "RED (armed)" : "GRAY (disarmed)")}");
        }
    }

    public override void HandleTriggerEnter(Collider other)
    {
        bool isSpawnedCheck = NetworkObject != null && NetworkObject.IsSpawned;

        Debug.Log($"[SlowTrap] HandleTriggerEnter - IsSpawned: {isSpawnedCheck}, CanTrigger: {CanTrigger()}");

        if (!isSpawnedCheck)
        {
            Debug.LogWarning("[SlowTrap] Not spawned yet");
            return;
        }

        if (!CanTrigger())
        {
            Debug.Log($"[SlowTrap] Cannot trigger - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}");
            return;
        }

        if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[SlowTrap] Wrong layer: {other.gameObject.layer}");
            return;
        }

        var player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning($"[SlowTrap] No Player component on {other.gameObject.name}");
            return;
        }

        Debug.Log($"[SlowTrap] Valid trigger by player {player.OwnerClientId}");
        RequestTriggerServerRpc(player.NetworkObjectId, other.ClosestPoint(transform.position));
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTriggerServerRpc(ulong playerNetworkId, Vector3 hitPoint)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        Debug.Log($"[SlowTrap - SERVER RPC] Received request for player {playerNetworkId}, IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[SlowTrap - SERVER RPC] Not on server!");
            return;
        }

        if (!CanTrigger())
        {
            Debug.LogWarning($"[SlowTrap - SERVER RPC] Cannot trigger");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerNetObj))
        {
            Debug.LogError($"[SlowTrap - SERVER RPC] Player {playerNetworkId} not found!");
            return;
        }

        Player player = playerNetObj.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError($"[SlowTrap - SERVER RPC] No Player component!");
            return;
        }

        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player,
            instigator = player.gameObject,
            hitPoint = hitPoint,
            hitNormal = Vector3.up
        };

        Debug.Log($"[SlowTrap - SERVER RPC] ✅ Triggering for player {player.OwnerClientId}");
        Trigger(ctx);
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        Debug.Log($"[SlowTrap] OnTriggerCore - IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[SlowTrap] OnTriggerCore called on client!");
            return;
        }

        var player = ctx.instigator.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("[SlowTrap] No Player in context!");
            return;
        }

        // SlowEffect
        StartCoroutine(ReducePlayerMovementSpeed(player));
        
        // Apply silhouette
        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
            Debug.Log($"[SlowTrap - SERVER] ✅ Silhouette applied");
        }
    }

    private IEnumerator ReducePlayerMovementSpeed(Player player)
    {
        player.TryGetComponent(out PlayerMovement playerMovement);
        playerMovement.SetMovementSpeedByModifier(0.5f);

        yield return new WaitForSeconds(duration);
        
        playerMovement.ResetMovementSpeed();
    }
    public override void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        base.Deploy(pos, rot, ownerGO);

        // Update color after deployment
        UpdateVisualColor();
        Debug.Log($"[SlowTrap] Deploy complete - IsArmed: {IsArmed}");
    }
}