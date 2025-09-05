using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private List<Player> players;
    public UnityEvent OnPlayerListChanged = new();
    public Player localPlayer;
    
    void Start()
    {
        if (Instance)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddPlayer(Player player)
    {
        if (players.Contains(player)) return;
        
        players.Add(player);
        OnPlayerListChanged.Invoke();
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        players.Remove(player);
        OnPlayerListChanged.Invoke();
    }
}
