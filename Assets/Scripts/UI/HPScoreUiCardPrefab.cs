using TMPro;
using UnityEngine;

public class HPScoreUiCardPrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI trapCountText;
    [SerializeField] private TextMeshProUGUI sabotageText;

    public void Initialize(string name)
    {
        playerText.text = name;
        UpdateTimeAsHp(0);
        UpdateTrapCount(0);
        UpdateSabotage(0);
    }
    public void UpdateTimeAsHp(float time)
    {
        timeText.text = time.ToString("0.00");
    }

    public void UpdateTrapCount(int trapCount)
    {
        trapCountText.text = trapCount.ToString();
    }

    public void UpdateSabotage(int sabotage)
    {
        sabotageText.text = sabotage.ToString();
    }
}
