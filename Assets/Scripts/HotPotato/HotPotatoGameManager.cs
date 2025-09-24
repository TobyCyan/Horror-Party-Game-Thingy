using System;
using Unity.Netcode;
using UnityEngine;

public class HotPotatoGameManager : NetworkBehaviour
{
    [SerializeField] private MarkManager markManager;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;


        markManager.StartHPGame();
    }

    private void OnValidate()
    {
        if (markManager == null)
        {
            Debug.LogWarning($"MarkManager reference is missing in HotPotatoGameManager: {name}");
        }
    }
}


