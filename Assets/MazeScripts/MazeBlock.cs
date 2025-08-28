using UnityEngine;
using static Cell;

public class MazeBlock : MonoBehaviour
{

    [Header("Order must be Left, Up, Right, Down")]
    [Tooltip("Walls array should be assigned in L-U-R-D order")]
    public GameObject[] walls;


    public void InitState(int state)
    {
        foreach (int dir in Directions)
        {
            if (!HasWall(state, dir))
            {
                walls[dir].SetActive(false);
            }
        }
    }
}
