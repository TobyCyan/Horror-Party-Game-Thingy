using UnityEngine;

public class MarkManager : MonoBehaviour
{
    public static MarkManager Instance;

    public GameObject currentMarkedPlayer;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AssignRandomPlayerWithMark();
    }

    private void Update()
    {
        if (currentMarkedPlayer == null)
        {
            AssignRandomPlayerWithMark();
        }
    }

    public void AssignRandomPlayerWithMark()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            int randomIndex = Random.Range(0, players.Length);
            currentMarkedPlayer = players[randomIndex];
        }
    }

}
