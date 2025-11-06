using UnityEngine;

public class CooldownSkillUi : SkillUi
{
    [SerializeField] private SkillCooldownUi skillCooldownUi;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        if (skillCooldownUi == null)
        {
            Debug.LogError("SkillCooldownUi is not assigned in the inspector.");
        }
    }

    protected override void ShowSkillUi(bool show)
    {
        base.ShowSkillUi(show);
        skillCooldownUi.gameObject.SetActive(show);
    }
}
