using UnityEngine;

public class SkillUi : MonoBehaviour
{
    public enum SkillType
    {
        Hunter,
        Runner
    }

    private GameObject self;
    [SerializeField] private SkillType skillType;
    [SerializeField] private HPPlayerControlsAssigner playerControlsAssigner;
    private HPSkillInputManager skillInputManager;

    protected virtual void Awake()
    {
        playerControlsAssigner.OnControlsAssigned += BindControlsToUi;
        self = gameObject;
    }

    protected virtual void Start()
    {
        skillInputManager = PlayerManager.Instance.localPlayer.GetComponent<HPSkillInputManager>();
    }

    protected virtual void ShowSkillUi(bool show)
    {
        self.SetActive(show);
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
