using TMPro;
using UnityEngine;

public class TimerUi : MonoBehaviour
{
    private Timer timer;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        timer = FindAnyObjectByType<Timer>();
        if (timer != null )
        {
            timer.OnTimeTick += UpdateTimer;
        }
    }

    private void OnDestroy()
    {
        if ( timer != null )
        {
            timer.OnTimeTick -= UpdateTimer;
        }
    }

    private void UpdateTimer()
    {
        timerText.text = timer.GetCurrentTimeAsString();
    }

    private void OnValidate()
    {
        if (timer == null)
        {
            Debug.LogWarning($"Timer is null on {name}!");
        }

        if (timerText == null)
        {
            Debug.LogWarning($"Timer text is null on {name}"!);
        }
    }
}
