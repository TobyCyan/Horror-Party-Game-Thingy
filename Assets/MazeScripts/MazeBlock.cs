using UnityEngine;
using System.Collections.Generic;
using static CellUtils;

/*
 * manages spawning of actual geometry from bitmask and maybe traps
 * traps use their registered mazeblocks as trigger check
 */
public class MazeBlock : MonoBehaviour
{
    [Header("Order must be Left, Up, Right, Down")]
    [Tooltip("Walls array should be assigned in L-U-R-D order")]
    public GameObject[] walls;
    public List<Trap> traps = new List<Trap>();

    public void RegisterTrap(Trap trapPrefab)
    {
        if (trapPrefab == null) return;

        Trap trapInstance = Instantiate(trapPrefab, transform.position, Quaternion.identity, transform);
        trapInstance.transform.localPosition += Vector3.up * 0.5f;
        if (traps == null) traps = new List<Trap>();
        traps.Add(trapInstance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateTraps();
        }
    }

    private void ActivateTraps()
    {
        StartCoroutine(ActivateTrapsWithDelay());
    }

    private System.Collections.IEnumerator ActivateTrapsWithDelay()
    {
        // gpt said do this in case traps are destroyed mid trigger (shouldnt happen)
        var activeTraps = traps.FindAll(t => t != null);

        foreach (Trap trap in activeTraps)
        {
            trap.Trigger();
            yield return new WaitForSeconds(Random.Range(0.2f, 0.6f));
        }
    }

    public void InitState(int state)
    {
        // for now just deactivate walls
        // forsee problems when we have actual geometry due to overlap situation
        foreach (int dir in Directions)
        {
            if (!HasWall(state, dir))
            {
                walls[dir].SetActive(false);
            }
        }
    }
    private void OnDestroy()
    {
        if (traps != null)
        {
            foreach (Trap trap in traps)
            {
                if (trap != null)
                    Destroy(trap.gameObject);
            }
            traps.Clear();
        }
    }

}
