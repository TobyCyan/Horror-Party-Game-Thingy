using UnityEngine;

public class GhostMarchTrap : TrapBase
{
    [SerializeField] private GameObject marchObject;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float marchSpeed = 2.0f;

    private new void Start()
    {
        base.Start();
        OnArmed += PlaceAtStart;
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

    private void March()
    {
        if (!marchObject)
        {
            return;
        }

        float epsilon = 0.1f;
        // March across.
        while (true)
        {
            marchObject.transform.position = Vector3.Lerp(marchObject.transform.position,
                                                        endPosition,
                                                        marchSpeed);
            if (Vector3.Distance(endPosition, marchObject.transform.position) < epsilon)
            {
                break;
            }
        }
        marchObject.transform.position = endPosition;
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        if (!marchObject)
        {
            return;
        }

        // March across
        March();

        // Re-arm this
        Arm();
    }
}
