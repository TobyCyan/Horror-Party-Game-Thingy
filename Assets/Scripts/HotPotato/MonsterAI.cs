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
        if (_markManager != null && _markManager.currentMarkedPlayer != null)
        {
            _agent.SetDestination(_markManager.currentMarkedPlayer.transform.position);
        }
    }
}
