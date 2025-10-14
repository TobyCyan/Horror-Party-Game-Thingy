using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;


// server keeps track of all scores, clients just report changes
public class MazeScoreManager : NetworkBehaviour
{
    public static MazeScoreManager Instance { get; private set; }

    [Header("Multipliers")]
    [SerializeField] private int sabotageMultiplier = 100;
    [SerializeField] private int timeMultiplier = 10;
    [SerializeField] private int soloBonus = 100;
    [SerializeField] private int firstBonus = 150;

    // structure for cumulative player data
    public struct PlayerScore
    {
        public int sabotage;
        public float time;
        public int bonus;
    }

    private static readonly Dictionary<ulong, PlayerScore> cumScores = new();
    private readonly Dictionary<ulong, float> roundClearTimes = new(); // for calculating bonuses

    private ulong clientId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        clientId = NetworkManager.Singleton.LocalClientId;
    }

    private void OnEnable()
    {
        TrapBase.StaticOnTriggered += OnTrapTriggered;
        MazeBlock.OnPlayerWin += NotifyWin;
    }

    private void OnDisable()
    {
        TrapBase.StaticOnTriggered -= OnTrapTriggered;
        MazeBlock.OnPlayerWin -= NotifyWin;
    }

    private void OnTrapTriggered(ITrap trap, TrapTriggerContext ctx)
    {
        if (ctx.source != TrapTriggerSource.Player) return;

        Player instigator = ctx.instigator.GetComponent<Player>();
        if (instigator == null)
        {
            Debug.LogWarning("Trap triggered with Player source but no Player instigator!");
            return;
        }

        RequestAddSabotageServerRpc(1, clientId);
    }

    // client requests score addition
    [ServerRpc(RequireOwnership = false)]
    private void RequestAddSabotageServerRpc(int units, ulong playerId)
    {
        if (!cumScores.ContainsKey(playerId))
            cumScores[playerId] = new PlayerScore();

        PlayerScore ps = cumScores[playerId];
        ps.sabotage += units * sabotageMultiplier;
        cumScores[playerId] = ps;

        UpdateClientScoreClientRpc(playerId, ps.sabotage, ps.time, ps.bonus);
    }

    // client notifies server when it wins (server calculates time score)
    public void NotifyWin()
    {
        if (IsOwner)
        {
            float remaining = MazeGameManager.Instance.GetTimeRemaining();
            RequestAddTimeServerRpc(remaining, clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddTimeServerRpc(float remaining, ulong playerId)
    {
        if (!cumScores.ContainsKey(playerId))
            cumScores[playerId] = new PlayerScore();

        PlayerScore ps = cumScores[playerId];
        ps.time += remaining * timeMultiplier;
        cumScores[playerId] = ps;

        roundClearTimes[playerId] = remaining; // have to store this for bonus calculation later

        UpdateClientScoreClientRpc(playerId, ps.sabotage, ps.time, ps.bonus);
    }


    public void CalculateBonusesAndBroadcast()
    {
        if (!IsServer) return; // jic
        ulong firstClient = 0;
        float maxTime = float.MinValue;
        int clears = 0;

        foreach (var kvp in roundClearTimes)
        {
            if (kvp.Value > maxTime)
            {
                maxTime = kvp.Value;
                firstClient = kvp.Key;
            }
            if (kvp.Value > 0)
                clears++;
        }

        bool soloWin = clears == 1;
        if (clears > 0)
        {
            PlayerScore first = cumScores[firstClient];
            int bonus = firstBonus;
            if (soloWin) bonus += soloBonus;
            first.bonus += bonus;
            cumScores[firstClient] = first;
        }

        foreach (var kvp in cumScores)
        {
            UpdateClientScoreClientRpc(kvp.Key, kvp.Value.sabotage, kvp.Value.time, kvp.Value.bonus);
            Debug.Log($"Live broadcast for {kvp.Key}: sabotage={kvp.Value.sabotage}, time={kvp.Value.time}, bonus={kvp.Value.bonus}");
        }

        roundClearTimes.Clear();
    }

    [ClientRpc]
    private void UpdateClientScoreClientRpc(ulong playerId, int sabotage, float time, int bonus)
    {
        ScoreUiManager.UpdateScore(playerId, time, sabotage, bonus);
    }


    // debug
    #if UNITY_EDITOR
        [ContextMenu("Test Add Sabotage")]
        private void DebugAddSabotage()
        {
            RequestAddSabotageServerRpc(1, NetworkManager.Singleton.LocalClientId);
        }

        [ContextMenu("Test Add Time")]
        private void DebugAddTime()
        {
            RequestAddTimeServerRpc(10f, NetworkManager.Singleton.LocalClientId);
        }
    #endif

}
