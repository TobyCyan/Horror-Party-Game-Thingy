using System;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MySessionManager : MonoBehaviour
{
    public static MySessionManager Instance;

    public ISession activeSession;

    private void Start()
    {
        if (!Instance)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void OnSessionStarted(ISession session)
    {
        activeSession = session;
    }

    public string GetJoinCode()
    {
        return activeSession.Code;
    }
}
