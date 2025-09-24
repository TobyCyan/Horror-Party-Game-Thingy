using System;
using System.Collections;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float duration;
    private float timer;
    public float CurrentTime => timer;
    public bool IsComplete => timer <= 0;
    public event Action OnTimeUp;
    public event Action OnTimeTick;

    public void StartTimer(float duration)
    {
        this.duration = duration;
        StopTimer();
        StartCoroutine(Tick());
    }

    private void OnDestroy()
    {
        StopTimer();
    }

    public void StopTimer()
    {
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
        while (true)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                OnTimeUp?.Invoke();
                yield break;
            }
            OnTimeTick?.Invoke();
            yield return null;
        }
    }
}
