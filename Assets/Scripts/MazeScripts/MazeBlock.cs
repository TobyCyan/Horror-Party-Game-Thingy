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
    // public List<Trap> traps = new List<Trap>();
    /*
     * removed my placeholder trap system, previously just had the blocks keep track of traps
     */

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
        // hello there
        // clean up traps assigned to it? maybe no need
    }

}
