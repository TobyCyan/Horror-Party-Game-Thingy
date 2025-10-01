using Unity.Netcode;
using UnityEngine;
public class MazeScoreManager : MonoBehaviour
{
    public static MazeScoreManager Instance { get; private set; }

    [Header("Multipliers")]
    [SerializeField] private int sabotageMultiplier = 100;
    [SerializeField] private int timeMultiplier = 10; // per remaining second
    [SerializeField] private int soloBonus = 100;
    [SerializeField] private int firstBonus = 150;

    private int currSaboScore;
    private float currTimeScore;
    private int currBonusScore;

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
        currSaboScore = 0;
        currBonusScore = 0;
        currTimeScore = 0;
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
        UpdateUi();
    }

    public void AddSabotageScore(int units)
    {
        currSaboScore += units * sabotageMultiplier;
    }

    // call this when player wins
    public void AddTimeScore()
    {
        currTimeScore += MazeGameManager.Instance.currPhase.timeRemaining; // for now i guess display remaining time to align with potato game until custom ui is done
    }

    // have to detect this and call..? i think maze goal should call this, track clients and their bonuses
    public void AddBonus(bool isFirst, bool isSolo)
    {
        int bonus = 0;
        if (isSolo) bonus += soloBonus;
        if (isFirst) bonus += firstBonus;
        currBonusScore += bonus;
    }

    private void UpdateUi()
    {
        ScoreUiManager.UpdateScore(clientId, currTimeScore, currSaboScore, currBonusScore);
    }

    public int TotalScore => currSaboScore + (int)currTimeScore + currBonusScore;


}
