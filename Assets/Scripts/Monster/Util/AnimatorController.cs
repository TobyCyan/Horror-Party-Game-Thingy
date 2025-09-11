using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    private Animator animator;

    public Animator Animator => animator;

    public void Initialize()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"Animator component is missing from the GameObject: {name}!");
        }
    }

    private void OnValidate()
    {
        if (animator == null)
        {
            Initialize();
        }
    }

    public void SetAnimatorBool(string paramName, bool value)
    {
        animator.SetBool(paramName, value);
    }

    public void SetAnimatorTrigger(string paramName)
    {
        animator.SetTrigger(paramName);
    }

    public void PlayAnimatorState(string stateName, int layer, float normalizedTime)
    {
        animator.Play(stateName, layer, normalizedTime);
    }

    public void ResetAnimator()
    {
        ResetAnimatorParams();
    }

    private void ResetAnimatorParams()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.name, 0f);
                    break;

                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.name, 0);
                    break;

                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(param.name, false);
                    break;

                case AnimatorControllerParameterType.Trigger:
                    animator.ResetTrigger(param.name);
                    break;
            }
        }
    }

    public bool IsAnimationCompleted(string animationName)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);

            // JumpScare finished and transitioning out
            if (stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1.0f)
            {
                return true;
            }

            // Already moved to a non-JumpScare state
            if (!nextInfo.IsName(animationName))
            {
                return true;
            }

            return false;
        }

        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1.0f;
    }
}
