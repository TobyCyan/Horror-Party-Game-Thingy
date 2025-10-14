using UnityEngine;

public class SkillUi : MonoBehaviour
{
    public enum SkillType
    {
        Hunter,
        Runner
    }

    private GameObject self;
    [SerializeField] private SkillCooldownUi skillCooldownUi;
    [SerializeField] private SkillType skillType;
    [SerializeField] private HPPlayerControlsAssigner playerControlsAssigner;
    private HPSkillInputManager skillInputManager;

    private void Awake()
    {
        playerControlsAssigner.OnControlsAssigned += BindControlsToUi;
        self = gameObject;
    }

    private void Start()
    {
        skillInputManager = PlayerManager.Instance.localPlayer.GetComponent<HPSkillInputManager>();

        if (skillCooldownUi == null)
        {
            Debug.LogError("SkillCooldownUi is not assigned in the inspector.");
            return;
        }

        ShowSkillUi(false);
    }

    private void ShowSkillUi(bool show)
    {
        self.SetActive(show);
        skillCooldownUi.gameObject.SetActive(show);
    }

    private void BindControlsToUi(HPSkillInputManager inputManager)
    {
        if (inputManager == null)
        {
            Debug.LogError("Input manager is null in BindControlsToUi.");
            return;
        }

        skillInputManager = inputManager;
        skillInputManager.OnHunterRoleSet += DetermineShowUi;
    }

    private void OnDestroy()
    {
        if (skillInputManager != null)
        {
            skillInputManager.OnHunterRoleSet -= DetermineShowUi;
        }

        if (playerControlsAssigner != null)
        {
            playerControlsAssigner.OnControlsAssigned -= BindControlsToUi;
        }
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
