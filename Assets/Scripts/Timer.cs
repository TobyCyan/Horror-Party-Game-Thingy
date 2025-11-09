using System;
using System.Collections;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float duration;
    private float timer;
    // Only use this property to get the current time
    public float CurrentTime => timer;
    public bool IsComplete => timer <= 0;
    public event Action OnTimeUp;
    public event Action<float> OnTimeTick;

    private bool isRunning = false;
    public bool IsRunning => isRunning;

    public void StartTimer(float duration)
    {
        if (isRunning)
        {
            Debug.LogWarning("Timer is already running. Restarting with new duration.");
        }

        StopTimer();

        this.duration = duration;
        isRunning = true;
        StartCoroutine(Tick());
    }

    private void OnDestroy()
    {
        // StopTimer();
    }

    public void StopTimer()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    public string GetCurrentTimeAsString()
    {
        TimeSpan time = TimeSpan.FromSeconds(timer);
        string formattedTime = time.ToString(@"mm\:ss");

        return formattedTime;
    }

    private IEnumerator Tick()
    {
        timer = duration;
        while (isRunning)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                if (isRunning)
                {
                    Debug.Log($"Timer finished at {name}.");
                    OnTimeUp?.Invoke();
                    StopTimer();
                }
                yield break;
            }
            OnTimeTick?.Invoke(timer);
            yield return null;
        }
    }
}
