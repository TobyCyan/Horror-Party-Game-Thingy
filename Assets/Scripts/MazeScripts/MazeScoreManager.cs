using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// for now only updated between rounds
public class MazeScoreManager : NetworkBehaviour
{
    public static MazeScoreManager Instance { get; private set; }

    [Header("Multipliers")]
    [SerializeField] private int sabotageMultiplier = 100;
    [SerializeField] private int timeMultiplier = 10; // per remaining second
    [SerializeField] private int soloBonus = 100;
    [SerializeField] private int firstBonus = 150;

    private int roundSaboScore = 0;
    private float roundTimeScore = 0;

    private ulong clientId = NetworkManager.Singleton.LocalClientId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetScores()
    {
        roundSaboScore = 0;
        roundTimeScore = 0;
    }

    private void OnEnable()
    {
        TrapBase.StaticOnTriggered += OnTrapTriggered;
    }

    private void OnDisable()
    {
        TrapBase.StaticOnTriggered -= OnTrapTriggered;
    }

    private void OnTrapTriggered(ITrap trap, TrapTriggerContext ctx)
    {
        if (ctx.source != TrapTriggerSource.Player) return; // only player-triggered traps

        Player instigator = ctx.instigator.GetComponent<Player>();
        if (instigator == null)
        {
            Debug.LogWarning("Trap triggered with Player source but no Player instigator!");
            return;
        }

        AddSabotageScore(1); // base scoire 1 for now
    }

    public void AddSabotageScore(int units)
    {
        roundSaboScore += units * sabotageMultiplier;
    }

    // call this when player wins
    public void AddTimeScore()
    {
        roundTimeScore = MazeGameManager.Instance.currPhase.timeRemaining * timeMultiplier; // for now i guess display remaining time to align with potato game until custom ui is done
    }

    public int TotalScore => roundSaboScore + (int)roundTimeScore;

    // for tallying all players' scores
    public struct PlayerScore
    {
        public int sabotage;
        public float time;
        public int bonus;
    }


    private static Dictionary<ulong, PlayerScore> submittedRawScores = new Dictionary<ulong, PlayerScore>();
    private Dictionary<ulong, PlayerScore> roundSubmissions = new Dictionary<ulong, PlayerScore>(); // per round

    public void SubmitRawScore()
    {
        if (!IsOwner) return;

        SubmitRawScoreServerRpc(roundSaboScore, roundTimeScore);
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitRawScoreServerRpc(int sabotage, float time, ServerRpcParams rpcParams = default)
    {
        ulong client = rpcParams.Receive.SenderClientId;

        Debug.Log($"Server Received raw score from client {client}: sabotage={sabotage}, time={time}");

        // accumulate cumulative scores
        if (!submittedRawScores.ContainsKey(client))
            submittedRawScores[client] = new PlayerScore { sabotage = sabotage, time = time, bonus = 0 };
        else
        {
            PlayerScore cumulative = submittedRawScores[client];
            cumulative.sabotage += sabotage;
            cumulative.time += time;
            submittedRawScores[client] = cumulative;
        }

        roundSubmissions[client] = new PlayerScore { sabotage = sabotage, time = time, bonus = 0 };

        // if all submit calc bonus
        if (roundSubmissions.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
        {

            Debug.Log("All clients submitted for this round, calculating bonuses..."); 
            CalculateBonusesAndBroadcast();
        }
    }

    private void CalculateBonusesAndBroadcast()
    {
        ulong firstClient = 0;
        float maxTime = float.MinValue;
        int clears = 0;

        foreach (var kvp in roundSubmissions)
        {
            if (kvp.Value.time > maxTime)
            {
                maxTime = kvp.Value.time;
                firstClient = kvp.Key;
            }

            if (kvp.Value.time > 0)
                clears++;
        }

        bool soloWin = clears == 1;

        Debug.Log($"Round clears={clears}, firstClient={firstClient}, soloWin={soloWin}");
        if (clears > 0)
        {
            PlayerScore cumulative = submittedRawScores[firstClient];
            int bonus = firstBonus;
            if (soloWin) bonus += soloBonus;
            cumulative.bonus += bonus;
            submittedRawScores[firstClient] = cumulative;
        }

        foreach (var kvp in submittedRawScores)
        {
            UpdateClientScoreClientRpc(kvp.Key, kvp.Value.sabotage, kvp.Value.time, kvp.Value.bonus);
            Debug.Log($"Broadcasting cumulative score for client {kvp.Key}: sabotage={kvp.Value.sabotage}, time={kvp.Value.time}, bonus={kvp.Value.bonus}");
        }

        // for next round
        roundSubmissions.Clear();
    }

    [ClientRpc]
    private void UpdateClientScoreClientRpc(ulong client, int sabotage, float time, int bonus)
    {
        ScoreUiManager.UpdateScore(client, time, sabotage, bonus);
    }

}
