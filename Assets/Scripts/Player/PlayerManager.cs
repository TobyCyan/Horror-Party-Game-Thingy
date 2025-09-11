using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public List<Player> players = new List<Player>();
    public UnityEvent OnPlayerListChanged = new();
    public Player localPlayer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!Instance)
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
        
        if (player.IsOwner) localPlayer = player;
        players.Add(player);
        OnPlayerListChanged.Invoke();
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        players.Remove(player);
        OnPlayerListChanged.Invoke();
    }

    public Player FindPlayerByNetId(ulong id)
    {
        return players.Find(p => p.Id == id);
    }
}