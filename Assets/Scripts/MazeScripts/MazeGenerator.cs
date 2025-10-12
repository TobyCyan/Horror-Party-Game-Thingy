using UnityEngine;
using static CellUtils;
using System;
using System.Collections.Generic;
/*
* generate primitive maze structure using kruskal
* modified from https://github.com/martinopiaggi/Unity-Maze-generation-using-disjoint-sets
*/
public class MazeGenerator
{
    private int[] cells;
    private int size = 0; // square maze
    private float roomRate;
    System.Random random;
    public static MazeGenerator Instance { get; private set; }

    public MazeGenerator(int size, int seed, float roomRate)
    {
        this.size = size;
        cells = new int[size * size];
        this.roomRate = roomRate;
        for (int x = 0; x < size * size; x++)
        {
            cells[x] = All;
        }
        random = new System.Random(seed);
    }

    public int[] GenerateMaze()
    {
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
            int randomCell = random.Next(0, N);

            // direction to break wall in
            int dir = Directions[random.Next(0, Directions.Length)];

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

   
        // cells[0] = RemoveWall(cells[0], Left); // make entrance if desired
        cells[N-1] = RemoveWall(cells[N-1], Right); // exit always at top right

        int roomsToPick = (int)(N * roomRate);
        var chosen = new HashSet<int>();

        while (chosen.Count < roomsToPick)
        {
            int candidate = random.Next(0, N);
            //avoid start and end for now
            if (candidate == 0 || candidate == N - 1) continue;

            if (!chosen.Contains(candidate))
            {
                chosen.Add(candidate);
                cells[candidate] = MakeRoom(cells[candidate]);
            }
        }

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
