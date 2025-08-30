using UnityEngine;

public class Reveal : MonoBehaviour
{
    [SerializeField] private float revealRadius = 4.0f;
    [SerializeField] private bool shouldRevealFollowOwner = false;
    private Vector3 revealPosition = Vector3.zero;
    private Vector3 hiddenPosition = new(0, -500, 0);
    private PlayerRadar playerRadar;
    private bool isRevealed = false;

    public void Initialize(Vector3 revealPosition, Vector3 hiddenPosition)
    {
        this.revealPosition = revealPosition;
        this.hiddenPosition = hiddenPosition;
    }

    private void OnValidate()
    {
        playerRadar ??= new PlayerRadar(revealRadius);
    }

    void Update()
    {
        Vector3 radarOrigin = shouldRevealFollowOwner ? transform.position : revealPosition;
        bool shouldReveal = playerRadar.IsPlayerInRange(radarOrigin, out Transform _);
        if (shouldReveal)
        {
            RevealSelf();
        }
        else
        {
            HideSelf();
        }
    }

    public void RevealSelf()
    {
        // Do nothing if already revealed
        if (isRevealed)
        {
            return;
        }

        isRevealed = true;
        transform.position = revealPosition;
    }

    public void HideSelf()
    {
        // Do nothing if already hidden
        if (!isRevealed)
        {
            return;
        }

        isRevealed = false;
        // Cache the current position before hiding
        revealPosition = transform.position;
        transform.position = hiddenPosition;
    }
}
