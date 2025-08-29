using UnityEngine;

public class Timer
{
    private float duration;
    private float currentTime;

    private bool isRunning;

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

    public bool RunTimer()
    {
        if (!isRunning)
        {
            return false;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= duration)
        {
            StopTimer();
            return true; // Timer finished
        }

        return false; // Timer still running
    }
}
