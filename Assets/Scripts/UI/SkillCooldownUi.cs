using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private string skillName;
    private float skillCooldown;

    private void Start()
    {
        if (SkillRegistry.Instance == null)
        {
            Debug.LogError("SkillRegistry instance not found in the scene.");
            return;
        }

        SkillRegistry.Instance.OnSkillRegistered += OnSkillRegistered;
    }

    private void OnDestroy()
    {
        if (SkillRegistry.Instance != null)
        {
            SkillRegistry.Instance.OnSkillRegistered += OnSkillRegistered;
            SkillRegistry.Instance.UnsubscribeSkillCooldownTimer(skillName,
                (cooldown) => UpdateUi(cooldown),
                () => ShowCooldown(false));
        }
    }

    private void OnSkillRegistered(string registeredSkillName)
    {
        if (registeredSkillName == skillName)
        {
            TrySubscribe();
        }
    }

    private void TrySubscribe()
    {
        if (SkillRegistry.Instance == null)
        {
            Debug.LogError("SkillRegistry instance not found in the scene.");
            return;
        }

        skillCooldown = SkillRegistry.Instance.GetSkillCooldown(skillName);
        SkillRegistry.Instance.SubscribeSkillCooldownTimer(skillName,
            (cooldown) => UpdateUi(cooldown),
            () => ShowCooldown(false));
    }

    private void UpdateUi(float cooldown)
    {
        ShowCooldown(cooldown > 0);
        cooldownText.text = GetCooldownText(cooldown);
        cooldownFill.fillAmount = GetCooldownFillAmount(cooldown, skillCooldown);
    }

    private string GetCooldownText(float cooldown)
    {
        return string.Format("{0:0.0}", cooldown);
    }

    private float GetCooldownFillAmount(float cooldown, float maxCooldown)
    {
        if (maxCooldown == 0) return 0;
        return cooldown / maxCooldown;
    }

    private void ShowCooldown(bool isShow)
    {
        cooldownText.enabled = isShow;
        cooldownFill.enabled = isShow;
    }
}
