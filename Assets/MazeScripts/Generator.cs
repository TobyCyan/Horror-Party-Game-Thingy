using UnityEngine;
using static Cell;
using UnityEngine.InputSystem;
/*
 * idea is kruskal to get maze structure
 * designate certain parts as 'rooms'
 * markov chain/wave collapse to decide which cells get which walls
 */
public class Generator : MonoBehaviour
{
    int[] cells;
    public int size = 15;

    // disjoint set as array
    // every iter connect set to neighbouring cell and eat it

    private void Start()
    {
        RegenerateMazeFromScratch();
    }

    public void RegenerateMazeFromScratch()
    {
        GameObject[] existingBlocks = GameObject.FindGameObjectsWithTag("MazeBlock");
        foreach (var block in existingBlocks)
        {
            Destroy(block);
        }

        InitMaze();
        GenerateMaze();
        LogMaze();
        Builder.Instance?.BuildMaze(cells, size);
    }

   


    void InitMaze()
    {
        cells = new int[size * size];

        for (int x = 0; x < size * size; x++)
        {
            cells[x] = All;
        }
    }
    // modified from https://github.com/martinopiaggi/Unity-Maze-generation-using-disjoint-sets/blob/main/Assets/Scripts/MazeGenerator.cs
    void GenerateMaze()
    {
        System.Func<int, int, int> idx = (x, y) => x * size + y; // wow variable capture

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

        // assuming bottom left to top right here!!
        int start = 0;
        int end = idx(size - 1, size - 1);

        while (!set.IsFullyConnected())
        {
            // pick some random cell
            int randomCell = Random.Range(0, N);

            // move in random direction
            int dir = Directions[Random.Range(0, Directions.Length)];

            if (!HasWall(cells[randomCell], dir)) continue;

            var neighbourCell = -1;
            if (!IsMazeEdge(randomCell, dir))
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

        // at this point the generation is finished, now have to select some 'rooms'
    }

    private bool IsMazeEdge(int cellIndex, int wallIndex)
    {
        if (cellIndex % size == 0 && wallIndex == Left) return true;
        if (cellIndex % size == size - 1 && wallIndex == Right) return true;
        if ((cellIndex / size) == size - 1 && wallIndex == Up) return true;
        return (cellIndex / size) == 0 && wallIndex == Down;
    }

    void LogMaze()
    {
        string str = "";
        for (int i = 0; i < size * size; i++)
        {
            if (i % size == 0) str += '\n';
            str += cells[i].ToString() + " ";
        }
        Debug.Log(str);
    }
}
