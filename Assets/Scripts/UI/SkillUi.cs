using UnityEngine;

public class SkillUi : MonoBehaviour
{
    public enum SkillType
    {
        Hunter,
        Runner
    }

    [SerializeField] private GameObject skillIcon;
    [SerializeField] private SkillCooldownUi skillCooldownUi;
    [SerializeField] private SkillType skillType;
    private HPSkillInputManager skillInputManager;

    private void Awake()
    {
        PlayerManager.OnLocalPlayerSet += BindPlayerToUi;
    }

    private void Start()
    {
        skillInputManager = PlayerManager.Instance.localPlayer.GetComponent<HPSkillInputManager>();

        if (skillIcon == null || skillCooldownUi == null)
        {
            Debug.LogError("SkillIcon or SkillCooldownUi is not assigned in the inspector.");
            return;
        }

        ShowSkillUi(false);
    }

    public void ShowSkillUi(bool show)
    {
        skillIcon.SetActive(show);
        skillCooldownUi.gameObject.SetActive(show);
    }

    private void BindPlayerToUi(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Player is null in BindPlayerToUi.");
            return;
        }

        skillInputManager = player.GetComponent<HPSkillInputManager>();
        if (skillInputManager != null)
        {
            skillInputManager.OnHunterRoleSet += DetermineShowUi;
        }
    }

    private void OnDestroy()
    {
        if (skillInputManager != null)
        {
            skillInputManager.OnHunterRoleSet -= DetermineShowUi;
        }
        PlayerManager.OnLocalPlayerSet -= BindPlayerToUi;
    }

    private void DetermineShowUi(bool isHunter)
    {
        bool shouldShow = ShouldShowUi(isHunter);
        ShowSkillUi(shouldShow);
    }

    private bool ShouldShowUi(bool isHunter)
    {
        bool shouldShowHunterSkill = isHunter && skillType == SkillType.Hunter;
        bool shouldShowRunnerSkill = !isHunter && skillType == SkillType.Runner;
        return shouldShowHunterSkill || shouldShowRunnerSkill;
    }
}
