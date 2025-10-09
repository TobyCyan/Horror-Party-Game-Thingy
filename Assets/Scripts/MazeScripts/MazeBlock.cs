using UnityEngine;
using System;
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

    private bool isGoal = false;
    public static event Action OnPlayerWin;
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

    public void SetAsGoal()
    {
        isGoal = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGoal) return;
        Debug.Log("Maze clear!");
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        OnPlayerWin?.Invoke(); // for local player

        isGoal = false; // locally so no scams
    }
}
