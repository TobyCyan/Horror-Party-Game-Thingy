using UnityEngine;


public static class CellUtils
{
    // Wall directions as indices LURD LURD
    public const int Left = 0;
    public const int Up = 1;
    public const int Right = 2;
    public const int Down = 3;
    private const int Room = 1 << 4;

    public static readonly int[] Directions = { Left, Up, Right, Down };

    public const int All = (1 << 4) - 1; // 0b1111, all 4 walls

    public static int RemoveWall(int cell, int wallIndex)
    {
        return cell & ~(1 << wallIndex);
    }

    public static int AddWall(int cell, int wallIndex)
    {
        return cell | (1 << wallIndex);
    }

    public static bool HasWall(int cell, int wallIndex)
    {
        return (cell & (1 << wallIndex)) != 0;
    }

    public static int MakeRoom(int cell)
    {
        return cell | Room;
    }

    public static int ClearRoom(int cell)
    {
        return cell & ~Room;
    }

    public static bool IsRoom(int cell)
    {
        return (cell & Room) != 0;
    }

}
