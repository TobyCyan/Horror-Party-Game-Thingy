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
        self = gameObject;

        // Hide UI by default until role is determined
        self.SetActive(false);

        if (playerControlsAssigner != null)
        {
            playerControlsAssigner.OnControlsAssigned += BindControlsToUi;
        }
        else
        {
            Debug.LogWarning("SkillUi: playerControlsAssigner is not assigned on " + name);
        }
    }

    protected virtual void Start()
    {
        // Try to grab any existing input manager on the local player (covers cases where the input
        // manager was added before this UI initialized).
        if (PlayerManager.Instance != null && PlayerManager.Instance.localPlayer != null)
        {
            if (PlayerManager.Instance.localPlayer.TryGetComponent<HPSkillInputManager>(out var existing))
            {
                BindControlsToUi(existing);
            }
        }
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

        // Avoid double-subscribe if we re-bind
        if (skillInputManager != null)
        {
            skillInputManager.OnHunterRoleSet -= DetermineShowUi;
        }

        skillInputManager = inputManager;
        skillInputManager.OnHunterRoleSet += DetermineShowUi;

        // Immediately set UI to current role
        DetermineShowUi(skillInputManager.CanUseHunterSkill);
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
