using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public List<Player> players = new();
    public static event Action<Player> OnPlayerAdded;
    public static event Action<Player> OnPlayerRemoved;
    public Player localPlayer;
    public static event Action OnAllPlayersEliminated;
    public static event Action OnLastPlayerStanding;
    public static event Action<Player> OnLocalPlayerSet;
    public static event Action OnAllPlayersLoaded;

    // For tracking expected players after scene load
    private int expectedPlayers = 100;
    private readonly HashSet<ulong> readyClients = new();

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
        Debug.Log($"[PlayerManager] Client {player.clientId} added at frame {Time.frameCount}");

        OnPlayerAdded?.Invoke(player);
    }
    
    public void RemovePlayer(Player player)
    {
        if (!players.Contains(player)) return;

        // Invoke event first to allow cleanup before removal
        OnPlayerRemoved?.Invoke(player);
        players.Remove(player);
    }

    public List<Player> GetAlivePlayers()
    {
        return players.Where(p => !p.IsEliminated).ToList();
    }

    public Player GetRandomAlivePlayer()
    {
        List<Player> alivePlayers = GetAlivePlayers();
        if (alivePlayers.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, alivePlayers.Count);
        return alivePlayers[randomIndex];
    }

    public void NotifyPlayerReady(ulong clientId)
    {
        if (readyClients.Contains(clientId))
            return;

        readyClients.Add(clientId);
        Debug.Log($"[PlayerManager] Client {clientId} is ready ({readyClients.Count}/{expectedPlayers})");

        if (readyClients.Count >= expectedPlayers)
        {
            Debug.Log("[PlayerManager] All players reported ready!");
            OnAllPlayersLoaded?.Invoke();
        }
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