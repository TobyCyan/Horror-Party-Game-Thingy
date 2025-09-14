using System.Collections;
using UnityEngine;

public class GhostMarchTrap : TrapBase
{
    [SerializeField] private GameObject marchObject;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float marchSpeed = 2.0f;

    private new void Start()
    {
        OnArmed += PlaceAtStart;
        base.Start();
    }

    private void OnDestroy()
    {
        OnArmed -= PlaceAtStart;
    }

    private void OnValidate()
    {
        if (!marchObject)
        {
            Debug.LogWarning($"March object is null on {name}!");
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

        // Disarm so won't trigger repeatedly
        Disarm();
        StartCoroutine(MarchThenRearm());
    }
}
