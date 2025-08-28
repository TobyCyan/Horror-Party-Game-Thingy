using UnityEngine;


public static class Cell
{
    // Wall directions as indices LURD LURD
    public const int Left = 0;
    public const int Up = 1;
    public const int Right = 2;
    public const int Down = 3;

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


    /*
    public static int SetRoom(int cell, bool isRoom)
    {
        if (isRoom) return cell | IsRoom; // cell | 10000
        else return cell & ~IsRoom; // cell & 01111
    }

    public static bool IsRoomCell(int cell)
    {
        return (cell & IsRoom) != 0; // check 5th bit
    }
    */

}
