using UnityEngine;

public class Timer
{
    private float duration;
    private float currentTime;

    private bool isRunning;

    public bool IsComplete => !isRunning && currentTime >= duration;

    public void StartTimer(float duration)
    {
        this.duration = duration;
        currentTime = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void RunTimer()
    {
        if (!isRunning)
        {
            return;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= duration)
        {
            Debug.Log("Timer completed after " + duration + " seconds.");
            StopTimer();
        }
    }
}
