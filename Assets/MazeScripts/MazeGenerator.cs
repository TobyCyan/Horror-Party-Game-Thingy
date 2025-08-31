using UnityEngine;
using static CellUtils;
using UnityEngine.InputSystem;
/*
 * generate primitive maze structure using kruskal
 * modified from https://github.com/martinopiaggi/Unity-Maze-generation-using-disjoint-sets
 */
public class MazeGenerator : MonoBehaviour
{
    int[] cells;
    public static MazeGenerator Instance { get; private set; }
    private void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject); 
    }

    void InitMaze(int size)
    {
        cells = new int[size * size];

        for (int x = 0; x < size * size; x++)
        {
            cells[x] = All;
        }
    }

    public int[] GenerateMaze(int size)
    {
        InitMaze(size);
        int idx(int x, int y) => x * size + y;  // wow variable capture

        int N = size * size;
        var set = new DisjointSet(N);
        // make set for each cell
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                set.MakeSet(idx(x, y));
            }
        }

        while (!set.IsFullyConnected())
        {
            // pick some random cell
            int randomCell = Random.Range(0, N);

            // direction to break wall in
            int dir = Directions[Random.Range(0, Directions.Length)];

            // if already broken choose another random cell, the cell and neighbour in question are already connected
            if (!HasWall(cells[randomCell], dir)) continue;

            var neighbourCell = -1;
            if (!IsMazeEdge(randomCell, dir, size))
            {
                if (dir == Left) neighbourCell = randomCell - 1; // Left
                if (dir == Up) neighbourCell = randomCell + size; // Up
                if (dir == Right) neighbourCell = randomCell + 1; // Right
                if (dir == Down) neighbourCell = randomCell - size; // Down
            }

            if (neighbourCell >= 0 && neighbourCell < N)
            {
                if (set.FindSet(neighbourCell) != set.FindSet(randomCell))
                {

                    cells[randomCell] = RemoveWall(cells[randomCell], dir);
                    cells[neighbourCell] = RemoveWall(cells[neighbourCell], (dir + 2)%4);

                    set.UnionSet(neighbourCell, randomCell);
                }
            }

        }

        cells[0] = RemoveWall(cells[0], Left);
        cells[N-1] = RemoveWall(cells[N-1], Right);

        #region debug
        string str = "";
        for (int i = 0; i < size * size; i++)
        {
            if (i % size == 0) str += '\n';
            str += cells[i].ToString() + " ";
        }
        Debug.Log(str);
        #endregion

        return cells;
    }

    private bool IsMazeEdge(int cellIndex, int wallIndex, int size)
    {
        if (cellIndex % size == 0 && wallIndex == Left) return true;
        if (cellIndex % size == size - 1 && wallIndex == Right) return true;
        if ((cellIndex / size) == size - 1 && wallIndex == Up) return true;
        return (cellIndex / size) == 0 && wallIndex == Down;
    }

}
