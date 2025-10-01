using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;

public struct TrapInfo : INetworkSerializable
{
    public float x;
    public float z;
    public int index; // type
    public int cost;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref x);
        serializer.SerializeValue(ref z);
        serializer.SerializeValue(ref index);
        serializer.SerializeValue(ref cost);
    }
}

// kind of stuffed this class full
// keep track of client's desired trap placements, support undo operations
// server will spawn traps in from the info collected from clients on phase end
public class MazeTrapManager : NetworkBehaviour
{
    public static MazeTrapManager Instance;

    [SerializeField] private LayerMask gridLayer;
    [SerializeField] public GameObject[] trapPrefabs; // should be TrapBase?

    private Stack<TrapInfo> trapsToBePlaced = new();

    private int selectedTrapIndex = -1;
    private bool active = false;

    private int money = 20;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        enabled = false; // start disabled
    }

    public void EnablePlacing(bool value)
    {
        active = value;
        enabled = value;
    }

    public void EnablePlacing(bool value, int initMoney)
    {
        active = value;
        enabled = value;
        this.money = initMoney;
    }

    public void SelectTrap(int index)
    {
        selectedTrapIndex = index;
    }

    void Update()
    {
        if (!active || selectedTrapIndex < 0)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, gridLayer))
            {
                var cell = hit.collider.gameObject; 
                Debug.Log("Hit " + cell);

                if (null == cell.GetComponent<MazeBlock>())
                {
                    return;
                }

                Vector3 pos = cell.transform.position;

                PlaceTrap(pos, selectedTrapIndex);
            }
        }
    }

    void PlaceTrap(Vector3 pos, int index)
    {

        int cost = 5;
        
        if (money < cost)
        {
            // tell player they are broke
            Debug.Log("u are broke");
            return;
        }
        money -= cost;

        var trapInfo = new TrapInfo
        {
            x = pos.x,
            z = pos.z,
            index = index,
            cost = cost
        };

        trapsToBePlaced.Push(trapInfo);
        Debug.Log($"Planned trap at {pos} type {index}");

    }

    public void Undo()
    {
        if (trapsToBePlaced.Count <= 0)
        {
            Debug.Log("Nothing to undo!");
            return;
        }

        var last = trapsToBePlaced.Pop();
        money += last.cost;
        Debug.Log("Undo last trap");
    }

    // to send to server called from trapphase
    public void FinaliseTraps()
    {
        if (trapsToBePlaced.Count == 0) return;
        SubmitTrapsServerRpc(trapsToBePlaced.ToArray());
        trapsToBePlaced.Clear();
    }

    // clients send trapinfo list and server will spawn all traps over network from that, support undo easily and less complicated (oneshot sync)
    [ServerRpc(RequireOwnership = false)]
    private void SubmitTrapsServerRpc(TrapInfo[] traps, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Server received {traps.Length} traps from client {rpcParams.Receive.SenderClientId}");

        for (int i = 0; i < traps.Length; i++)
        {
            var t = traps[i];
            Debug.Log($"Trap {i}: x={t.x}, z={t.z}, index={t.index}, cost={t.cost}");
        }

        ulong senderId = rpcParams.Receive.SenderClientId;

        GameObject owner = PlayerManager.Instance.FindPlayerByClientId(senderId).gameObject;

        foreach (var t in traps)
        {
            if (t.index < 0 || t.index >= trapPrefabs.Length)
            {
                Debug.LogWarning($"Invalid trap index {t.index} from client {senderId}");
                continue;
            }

            Vector3 pos = new (t.x, 0f, t.z);
            Quaternion rot = Quaternion.identity;

            var trap = Instantiate(trapPrefabs[t.index], pos, rot);
            var trapBase = trap.GetComponent<TrapBase>();

            // follow interface
            trapBase.Deploy(pos, rot, owner);

            trap.GetComponent<NetworkObject>().Spawn(true);
        }
    }

}
