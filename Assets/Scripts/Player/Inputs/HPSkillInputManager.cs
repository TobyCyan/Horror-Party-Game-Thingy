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

    private void SetAsHunter(ulong newHunterId)
    {
        if (!TryGetComponent(out Player owner))
        {
            Debug.LogWarning($"HPSkillInputManager could not find Player component on the same GameObject: {name}.");
            return;
        }
        bool isHunter = owner.Id == newHunterId;
        SetCanUseHunterSkill(isHunter);
        OnHunterRoleSet?.Invoke(isHunter);
    }

    private void SetCanUseHunterSkill(bool canUse)
    {
        CanUseHunterSkill = canUse;
    }

    private void OnValidate()
    {
        if (markManager == null)
        {
            markManager = FindFirstObjectByType<MarkManager>();
        }
    }
}
