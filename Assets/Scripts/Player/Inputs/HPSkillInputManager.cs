using UnityEngine;

public class HPSkillInputManager : InputManager
{
    // Toggle depending on whether the player is a hunter
    public bool CanUseHunterSkill { get; set; } = false;

    // Hunter's skill
    private PlayerSkill huntersSightSkill;

    private void Awake()
    {
        if (inputAction != null)
        {
            inputAction.Enable();
            inputAction.FindAction("HP_Hunter_Skill/Use", true).performed += _ => UseHunterSkill();
        }

        if (huntersSightSkill == null)
        {
            huntersSightSkill = gameObject.AddComponent<HuntersSightSkill>();
        }
    }

    private void OnDestroy()
    {
        if (inputAction != null)
        {
            inputAction.FindAction("HP_Hunter_Skill/Use", true).performed -= _ => UseHunterSkill();
            inputAction.Disable();
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

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
