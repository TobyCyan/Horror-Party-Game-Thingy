using System.Collections;
using UnityEngine;

public class GhostMarchTrap : TrapBase
{
    [SerializeField] private GameObject marchObject;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float marchSpeed = 2.0f;
    [SerializeField] private JumpScare jumpScareModel;
    private bool isJumpScaring = false;

    private new void Start()
    {
        jumpScareModel.OnJumpScareStart += JumpScareStartHandler;
        jumpScareModel.OnJumpScareCleanUp += CleanUpHandler;
        jumpScareModel.AfterJumpScarePlayer += AfterJumpScarePlayerHandler;
        jumpScareModel.gameObject.SetActive(false);

        OnArmed += PlaceAtStart;
        base.Start();
    }

    private void OnDestroy()
    {
        jumpScareModel.OnJumpScareStart -= JumpScareStartHandler;
        jumpScareModel.OnJumpScareCleanUp -= CleanUpHandler;
        jumpScareModel.AfterJumpScarePlayer -= AfterJumpScarePlayerHandler;
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

    private void AfterJumpScarePlayerHandler(Player player)
    {
        EliminatePlayer(player);
    }

    private void EliminatePlayer(Player player)
    {
        if (!player.IsOwner)
        {
            return;
        }
        player.EliminatePlayer();
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
        isJumpScaring = false;
        // Re-arm upon finishing jumpscare.
        Arm();
    }

    void OnTriggerEnter(Collider other)
    {
        // Only allow one jumpscare at a time.
        if (isJumpScaring)
        {
            return;
        }

        if (!other.TryGetComponent(out Player player))
        {
            return;
        }

        isJumpScaring = true;
        jumpScareModel.gameObject.SetActive(true);
        jumpScareModel.TriggerJumpScare(player);
    }
}
