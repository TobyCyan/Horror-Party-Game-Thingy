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
        if (_markManager != null && MarkManager.currentMarkedPlayer != null)
        {
            _agent.SetDestination(MarkManager.currentMarkedPlayer.transform.position);
        }
    }
}
