using System;
using System.Collections.Generic;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public List<Player> players = new();
    public event Action<Player> OnPlayerAdded;
    public event Action<Player> OnPlayerRemoved;
    public Player localPlayer;
    private readonly List<Player> alivePlayers = new();
    public List<Player> AlivePlayers => alivePlayers;
    public event Action OnAllPlayersEliminated;
    public event Action OnLastPlayerStanding;

    private void Awake()
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void AddPlayer(Player player)
    {
        if (players.Contains(player)) return;
        
        if (player.IsOwner) localPlayer = player;

        players.Add(player);
        alivePlayers.Add(player);
        player.OnPlayerEliminated += () => EliminatePlayer(player);

        OnPlayerAdded?.Invoke(player);
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        players.Remove(player);
        alivePlayers.Remove(player);
        player.OnPlayerEliminated -= () => EliminatePlayer(player);

        OnPlayerRemoved?.Invoke(player);
    }

    /// <summary>
    /// Eliminates a player from the alive players list.
    /// </summary>
    /// <param name="player"></param>
    public void EliminatePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        if (alivePlayers.Contains(player))
        {
            alivePlayers.Remove(player);
        }

        if (alivePlayers.Count <= 1)
        {
            OnLastPlayerStanding?.Invoke();
        }

        if (alivePlayers.Count == 0)
        {
            OnAllPlayersEliminated?.Invoke();
        }
    }

    public bool IsPlayerAlive(Player player)
    {
        return alivePlayers.Contains(player);
    }

    public Player GetRandomAlivePlayer()
    {
        if (alivePlayers.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, alivePlayers.Count);
        return alivePlayers[randomIndex];
    }

    public Player FindPlayerByNetId(ulong id)
    {
        return players.Find(p => p.Id == id);
    }

    public Player FindPlayerByClientId(ulong clientId)
    {
        return players.Find(p => p.clientId == clientId);
    }
}