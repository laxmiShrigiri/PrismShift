using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds connected groups of matching tiles or empty matching-color cells on the puzzle board.
/// </summary>
public static class PuzzleBoardSearch
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1,  0),                         new Vector2Int(1,  0),
        new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
    };

    public static List<BoardSlot> FindConnectedUnsolvedColorGroup(BoardSlot[,] grid, BoardSlot start)
    {
        List<BoardSlot> result = new List<BoardSlot>();
        if (grid == null || start == null || start.IsEmpty || start.IsSatisfied)
            return result;

        Color groupColor = start.CurrentTile.ColorValue;
        Traverse(grid, start, slot =>
            !slot.IsEmpty && !slot.IsSatisfied &&
            BoardSlot.ColorsMatch(slot.CurrentTile.ColorValue, groupColor), result);
        return result;
    }

    public static List<BoardSlot> FindConnectedEmptyTargetSlots(BoardSlot[,] grid, BoardSlot start, Color targetColor)
    {
        List<BoardSlot> result = new List<BoardSlot>();
        if (grid == null || start == null || !start.IsEmpty ||
            !BoardSlot.ColorsMatch(start.TargetFloorColor, targetColor))
            return result;

        Traverse(grid, start, slot =>
            slot.IsEmpty && BoardSlot.ColorsMatch(slot.TargetFloorColor, targetColor), result);
        return result;
    }

    private static void Traverse(BoardSlot[,] grid, BoardSlot start,
        System.Predicate<BoardSlot> canVisit, List<BoardSlot> result)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        bool[,] visited = new bool[width, height];
        Queue<BoardSlot> pending = new Queue<BoardSlot>();
        pending.Enqueue(start);
        visited[start.GridCoords.x, start.GridCoords.y] = true;

        while (pending.Count > 0)
        {
            BoardSlot current = pending.Dequeue();
            if (!canVisit(current))
                continue;

            result.Add(current);
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int nextCoords = current.GridCoords + Directions[i];
                if (nextCoords.x < 0 || nextCoords.x >= width ||
                    nextCoords.y < 0 || nextCoords.y >= height || visited[nextCoords.x, nextCoords.y])
                    continue;

                visited[nextCoords.x, nextCoords.y] = true;
                BoardSlot next = grid[nextCoords.x, nextCoords.y];
                if (next != null && canVisit(next))
                    pending.Enqueue(next);
            }
        }
    }
}
