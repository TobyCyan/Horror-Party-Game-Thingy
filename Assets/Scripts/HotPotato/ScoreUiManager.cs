using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ScoreUiManager : MonoBehaviour
{
    public static ScoreUiManager Instance;

    [SerializeField] private GameObject scoreBoardGameObject;
    [SerializeField] private GameObject scorePanelParent;
    [SerializeField] private HPScoreUiCardPrefab playerCardPrefab;
    private Dictionary<ulong, HPScoreUiCardPrefab> playerCards = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        
        Instance = this;
    }
    
    public static void UpdateScore(ulong playerId, float time, int trapCount, int sabotageCount)
    {
        UpdateScoreServerRpc(playerId, time, trapCount, sabotageCount);
    }

    [Rpc(SendTo.Server)]
    private static void UpdateScoreServerRpc(ulong clientId, float time, int trapCount, int sabotageCount)
    {
        UpdateScoreEveryoneRpc(clientId, time, trapCount, sabotageCount);
    }

    [Rpc(SendTo.Everyone)]
    private static void UpdateScoreEveryoneRpc(ulong clientId, float time, int trapCount, int sabotageCount)
    {
        Instance.playerCards.TryGetValue(clientId, out var playerCard);
        if (playerCard)
        {
            playerCard.UpdateTimeAsHp(time);
            playerCard.UpdateTrapCount(trapCount);
            playerCard.UpdateSabotage(sabotageCount);
        }
    }

    public void PlayerJoined(ulong clientId)
    {
        HPScoreUiCardPrefab playerCard = Instantiate(playerCardPrefab, scorePanelParent.transform);
        playerCards.Add(clientId, playerCard);
        playerCard.Initialize(clientId.ToString()); // Change to name eventually
    }

    public void PlayerLeft(ulong clientId)
    {
        if (playerCards.TryGetValue(clientId, out HPScoreUiCardPrefab playerCard))
        {
            Destroy(playerCard.gameObject);
            playerCards.Remove(clientId);
        }
    }

    public void ShowFinalScore()
    {
        scoreBoardGameObject.SetActive(true);
    }
    void Update()
    {
        scoreBoardGameObject.SetActive(Input.GetKey(KeyCode.Tab));
    }
}