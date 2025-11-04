using System.Collections.Generic;
using UnityEngine;
using System;

public class SkillRegistry : MonoBehaviour
{
    public static SkillRegistry Instance { get; private set; }
    private readonly Dictionary<string, PlayerSkill> skillsMap = new();
    public static Action<string> OnSkillRegistered;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Registers a skill in the registry.
    /// Uses the skill's class name as the key.
    /// </summary>
    /// <param name="skill"></param>
    public void RegisterSkill(PlayerSkill skill)
    {
        string key = skill.GetType().Name;
        Debug.Log($"Registering skill: {key}");
        skillsMap[key] = skill;
        OnSkillRegistered?.Invoke(key);
    }

    public float GetSkillCooldown(string skillName)
    {
        if (!skillsMap.TryGetValue(skillName, out PlayerSkill skill))
        {
            Debug.LogWarning($"Skill {skillName} not found in registry.");
            return 0f;
        }
        return skill.Cooldown;
    }

    public void SubscribeSkillCooldownTimer(string skillName, Action<float> timeTickAction, Action timeUpAction)
    {
        if (!skillsMap.TryGetValue(skillName, out PlayerSkill skill))
        {
            Debug.LogWarning($"Skill {skillName} not found in registry.");
            return;
        }
        Timer timer = skill.CoolDownTimer;
        timer.OnTimeTick += timeTickAction;
        timer.OnTimeUp += timeUpAction;
    }

    public void UnsubscribeSkillCooldownTimer(string skillName, Action<float> timeTickAction, Action timeUpAction)
    {
        if (!skillsMap.TryGetValue(skillName, out PlayerSkill skill))
        {
            Debug.LogWarning($"Skill {skillName} not found in registry.");
            return;
        }
        Timer timer = skill.CoolDownTimer;
        timer.OnTimeTick -= timeTickAction;
        timer.OnTimeUp -= timeUpAction;
    }
}
