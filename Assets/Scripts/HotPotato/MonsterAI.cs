using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    private NavMeshAgent _agent;
    private MarkManager _markManager;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _markManager = MarkManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        Player markedPlayer = _markManager.GetMarkedPlayer();
        if (_markManager != null && markedPlayer != null)
        {
            _agent.SetDestination(markedPlayer.transform.position);
        }
    }
}
