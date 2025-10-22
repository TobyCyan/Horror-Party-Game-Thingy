using System;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private ISession activeSession;

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
