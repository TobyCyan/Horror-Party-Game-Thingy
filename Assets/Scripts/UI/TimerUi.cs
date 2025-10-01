using System;
using TMPro;
using UnityEngine;

public class TimerUi : MonoBehaviour
{
    private Timer timer;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Awake()
    {
        HotPotatoGameManager.Instance.timer.OnValueChanged += UpdateTimerText;
    }

    private void UpdateTimerText(float previousvalue, float newvalue)
    {
        timerText.text = Mathf.Max(0, newvalue).ToString("0.00");
    }

    // private void Start()
    // {
    //     timer = FindAnyObjectByType<Timer>();
    //     if (timer != null )
    //     {
    //         timer.OnTimeTick += UpdateTimer;
    //     }
    // }
    //
    // private void OnDestroy()
    // {
    //     if ( timer != null )
    //     {
    //         timer.OnTimeTick -= UpdateTimer;
    //     }
    // }
    //
    // private void UpdateTimer()
    // {
    //     timerText.text = timer.GetCurrentTimeAsString();
    // }
    //
    // private void OnValidate()
    // {
    //     if (timer == null)
    //     {
    //         Debug.LogWarning($"Timer is null on {name}!");
    //     }
    //
    //     if (timerText == null)
    //     {
    //         Debug.LogWarning($"Timer text is null on {name}"!);
    //     }
    // }
}
