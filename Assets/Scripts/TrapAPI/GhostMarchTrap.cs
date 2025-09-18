using System.Collections;
using UnityEngine;

public class GhostMarchTrap : TrapBase
{
    [SerializeField] private GameObject marchObject;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float marchSpeed = 2.0f;
    [SerializeField] private LayerMask playerMask;   // set to "Player" in Inspector
    [SerializeField] private JumpScare jumpScareModel;

    private new void Start()
    {
        jumpScareModel.OnJumpScareStart += JumpScareStartHandler;
        jumpScareModel.OnJumpScareCleanUp += CleanUpHandler;
        jumpScareModel.gameObject.SetActive(false);

        OnArmed += PlaceAtStart;
        base.Start();
    }

    private void OnDestroy()
    {
        jumpScareModel.OnJumpScareStart -= JumpScareStartHandler;
        jumpScareModel.OnJumpScareCleanUp -= CleanUpHandler;
        OnArmed -= PlaceAtStart;
    }

    private void OnValidate()
    {
        if (!marchObject)
        {
            Debug.LogWarning($"March object is null on {name}!");
        }

        if (!jumpScareModel)
        {
            Debug.LogWarning($"Jumpscare model is null on {name}!");
        }
    }

    private void PlaceAtStart(ITrap _)
    {
        if (!marchObject)
        {
            return;
        }
        marchObject.transform.position = startPosition;
    }

    private IEnumerator MarchThenRearm()
    {
        if (!marchObject)
        {
            yield break;
        }

        float epsilon = 0.1f;
        // March across.
        while (true)
        {
            marchObject.transform.position = Vector3.MoveTowards(marchObject.transform.position,
                                                        endPosition,
                                                        marchSpeed * Time.deltaTime);
            if (Vector3.Distance(endPosition, marchObject.transform.position) < epsilon)
            {
                break;
            }
            yield return null;
        }
        marchObject.transform.position = endPosition;
        // Re-arm this
        Arm();
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        if (!marchObject)
        {
            return;
        }

        // Disarm so marching won't trigger repeatedly
        Disarm();
        StartCoroutine(MarchThenRearm());
    }

    private void JumpScareStartHandler()
    {
        // Disarm so jumpscare won't trigger repeatedly
        StopAllCoroutines();
        Disarm();
    }

    private void CleanUpHandler()
    {
        jumpScareModel.gameObject.SetActive(false);
        // Re-arm upon finishing jumpscare.
        Arm();
    }

    void OnTriggerEnter(Collider other)
    {
        bool isNotPlayer = (playerMask.value & (1 << other.gameObject.layer)) == 0;
        if (isNotPlayer)
        {
            return;
        }

        if (!other.TryGetComponent(out Player player))
        {
            return;
        }

        jumpScareModel.gameObject.SetActive(true);
        jumpScareModel.TriggerJumpScare(player.transform);
    }
}
