using UnityEngine;

public class HPSkillInputManager : InputManager
{
    // Toggle depending on whether the player is a hunter
    public bool CanUseHunterSkill { get; set; } = false;

    // Hunter's skill
    [SerializeField] private PlayerSkill huntersSightSkill;

    private void Awake()
    {
        if (playerInput != null)
        {
            playerInput.actions["HP_Hunter_Skill"].performed += ctx => UseHunterSkill();
        }

        if (huntersSightSkill == null)
        {
            Debug.LogWarning($"No PlayerSkill component found on {name}."); 
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
