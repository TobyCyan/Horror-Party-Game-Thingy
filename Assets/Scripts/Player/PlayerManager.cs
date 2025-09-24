using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public List<Player> players = new();
    public UnityEvent OnPlayerListChanged = new();
    public Player localPlayer;
    private List<Player> alivePlayers = new();
    public List<Player> AlivePlayers => alivePlayers;

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
        alivePlayers.Add(player);
        OnPlayerListChanged.Invoke();
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        players.Remove(player);
        alivePlayers.Remove(player);
        OnPlayerListChanged.Invoke();
    }

    public void EliminatePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        if (alivePlayers.Contains(player))
        {
            alivePlayers.Remove(player);
        }
    }

    public bool IsPlayerAlive(Player player)
    {
        return alivePlayers.Contains(player);
    }

    public Player GetRandomAlivePlayer()
    {
        if (alivePlayers.Count == 0) return null;
        int randomIndex = Random.Range(0, alivePlayers.Count);
        return alivePlayers[randomIndex];
    }

    public Player FindPlayerByNetId(ulong id)
    {
        return players.Find(p => p.Id == id);
    }
}