using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public List<Player> players = new();
    public static event Action<Player> OnPlayerAdded;
    public static event Action<Player> OnPlayerRemoved;
    public Player localPlayer;
    private readonly List<Player> alivePlayers = new();
    public List<Player> AlivePlayers => alivePlayers;
    public static event Action OnAllPlayersEliminated;
    public static event Action OnLastPlayerStanding;
    public static event Action<Player> OnLocalPlayerSet;
    public static event Action OnAllPlayersLoaded;
    private int expectedPlayers = 100;

    private void Awake()
    {
        Instance = this;
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        if (IsServer)
            expectedPlayers = clientsCompleted.Count;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void AddPlayer(Player player)
    {
        if (players.Contains(player)) return;
        
        if (player.IsOwner)
        {
            localPlayer = player;
            OnLocalPlayerSet?.Invoke(localPlayer);
        }
        
        players.Add(player);
        alivePlayers.Add(player);
        player.OnPlayerEliminated += HandleOnPlayerEliminated;

        OnPlayerAdded?.Invoke(player);
        if (players.Count == expectedPlayers)
        {
            OnAllPlayersLoaded?.Invoke();
        }
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;
        
        players.Remove(player);
        alivePlayers.Remove(player);
        player.OnPlayerEliminated -= HandleOnPlayerEliminated;

        OnPlayerRemoved?.Invoke(player);
    }

    private void HandleOnPlayerEliminated()
    {
        EliminatePlayer(localPlayer);
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
    
    public Player FindPlayerByClientId(ulong id)
    {
        return players.Find(p => p.clientId == id);
    }
}