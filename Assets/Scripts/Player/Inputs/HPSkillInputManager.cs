using System;
using UnityEngine;

public class HPSkillInputManager : MonoBehaviour
{
    protected HPSkillInput inputActions;

    // Toggle depending on whether the player is a hunter
    public bool CanUseHunterSkill { get; set; } = false;

    // Hunter's skill
    private PlayerSkill huntersSightSkill;
    private MarkManager markManager;

    // References to interaction components (auto-discovered)
    private PlayerPickup playerPickup;
    private TrapPlacer trapPlacer;

    public Action<bool> OnHunterRoleSet;

    private void Awake()
    {
        inputActions = new HPSkillInput();
        inputActions.Enable();
        inputActions.FindAction("HP_Hunter_Skill/Use", true).performed += _ => UseHunterSkill();

        if (huntersSightSkill == null)
        {
            huntersSightSkill = gameObject.AddComponent<HuntersSightSkill>();
        }

        if (markManager == null)
        {
            markManager = FindFirstObjectByType<MarkManager>();
        }

        if (markManager != null)
        {
            markManager.OnMarkPassed += SetAsHunter;
        }

        // Auto-discover interaction components
        FindInteractionComponents();
    }

    private void FindInteractionComponents()
    {
        // Search for PlayerPickup component
        playerPickup = GetComponent<PlayerPickup>();
        if (playerPickup == null)
        {
            playerPickup = GetComponentInChildren<PlayerPickup>();
        }
        if (playerPickup == null)
        {
            playerPickup = GetComponentInParent<PlayerPickup>();
        }

        // Search for TrapPlacer component
        trapPlacer = GetComponent<TrapPlacer>();
        if (trapPlacer == null)
        {
            trapPlacer = GetComponentInChildren<TrapPlacer>();
        }
        if (trapPlacer == null)
        {
            trapPlacer = GetComponentInParent<TrapPlacer>();
        }

        // Log findings
        if (playerPickup == null)
        {
            Debug.LogWarning($"[HPSkillInputManager] Could not find PlayerPickup component on {name} or its hierarchy");
        }

        if (trapPlacer == null)
        {
            Debug.LogWarning($"[HPSkillInputManager] Could not find TrapPlacer component on {name} or its hierarchy");
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.FindAction("HP_Hunter_Skill/Use", true).performed -= _ => UseHunterSkill();
            inputActions.Disable();
        }

        if (markManager != null)
        {
            markManager.OnMarkPassed -= SetAsHunter;
        }
    }

    private void UseHunterSkill()
    {
        if (!CanUseHunterSkill)
        {
            Debug.LogWarning($"Non-hunter attempted to use hunter skill.");
            return;
        }

        Debug.Log("Hunter skill used!");
        huntersSightSkill.UseSkill();
    }

    private void SetAsHunter(ulong newHunterClientId)
    {
        if (!TryGetComponent(out Player owner))
        {
            Debug.LogWarning($"HPSkillInputManager could not find Player component on the same GameObject: {name}.");
            return;
        }

        bool isHunter = owner.clientId == newHunterClientId;
        SetCanUseHunterSkill(isHunter);

        // Enable/disable interactions based on hunter status
        UpdateInteractionAbilities(isHunter);

        OnHunterRoleSet?.Invoke(isHunter);
    }

    private void SetCanUseHunterSkill(bool canUse)
    {
        CanUseHunterSkill = canUse;
    }

    /// <summary>
    /// Updates pickup and placement abilities based on hunter status
    /// </summary>
    /// <param name="isHunter">True if player is hunter, false otherwise</param>
    private void UpdateInteractionAbilities(bool isHunter)
    {
        // Try to find components again if they weren't found initially
        if (playerPickup == null || trapPlacer == null)
        {
            FindInteractionComponents();
        }

        // Hunters cannot pick up items or place traps
        // Non-hunters can do both

        if (playerPickup != null)
        {
            playerPickup.IsPickupEnabled = !isHunter;
            playerPickup.ClearNearestItem();
            Debug.Log($"[HPSkillInputManager] Pickup {(isHunter ? "disabled" : "enabled")} - player is {(isHunter ? "hunter" : "prey")}");
        }

        if (trapPlacer != null)
        {
            trapPlacer.IsPlacementEnabled = !isHunter;
            Debug.Log($"[HPSkillInputManager] Placement {(isHunter ? "disabled" : "enabled")} - player is {(isHunter ? "hunter" : "prey")}");
        }
    }

    /// <summary>
    /// Manually set interaction abilities (useful for testing or special cases)
    /// </summary>
    public void SetInteractionAbilities(bool canPickup, bool canPlace)
    {
        if (playerPickup != null)
        {
            playerPickup.IsPickupEnabled = canPickup;
        }

        if (trapPlacer != null)
        {
            trapPlacer.IsPlacementEnabled = canPlace;
        }
    }

    private void OnValidate()
    {
        if (markManager == null)
        {
            markManager = FindFirstObjectByType<MarkManager>();
        }
    }
}