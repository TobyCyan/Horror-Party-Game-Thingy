using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// implementation modified from https://github.com/martinopiaggi/Unity-Maze-generation-using-disjoint-sets
public class DisjointSet
{
    private int[] _set;
    private int[] _rank;

    public DisjointSet(int size)
    {
        _set = new int[size];
        _rank = new int[size];
    }

    public void MakeSet(int x)
    {
        _set[x] = x;
        _rank[x] = 0;
    }

    public int FindSet(int x)
    {
        if (x != _set[x]) return FindSet(_set[x]);
        return x;
    }

    public void UnionSet(int x, int y)
    {
        var parentX = FindSet(x);
        var parentY = FindSet(y);
        if (_rank[parentX] > _rank[parentY]) _set[parentY] = parentX;
        else
        {
            _set[parentX] = parentY;
            if (_rank[parentX] == _rank[parentY]) _rank[parentY]++;
        }
    }

    //slow bandaid, likely not an issue with small maze size
    public bool IsFullyConnected()
    {
        if (_set.Length == 0) return true;

        int rep = FindSet(0);
        for (int i = 1; i < _set.Length; i++)
        {
            if (FindSet(i) != rep) return false;
        }
        return true;
    }
}