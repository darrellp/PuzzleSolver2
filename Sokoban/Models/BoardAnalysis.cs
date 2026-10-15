using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sokoban.Models;

internal partial class Board
{
    #region Analysis
    // find all cells accessible by player and create BaseGraph
    private void IdentifyBaseAccessibility()
    {
        // Breadth first search on the cells starting from the player.  Every reachable cell becomes a
        // vertex and every pair of adjacent reachable cells is joined by edges in both directions.
        // Cells occupied by a box are treated as walls and excluded from the graph entirely.
        var processed = new HashSet<Point>();
        var queue = new Queue<Point>();

        AddVertexAt(Player);
        queue.Enqueue(Player);
        while (queue.TryDequeue(out Point cur))
        {
            this[cur] |= CellType.Accessible;
            AddVertexAt(cur);

            processed.Add(cur);
            foreach (Point pt in OpenNeighbors(cur))
            {
                var index = PtToIndex(pt);
                if (Cells[index].vtx == null)
                {
                    queue.Enqueue(pt);
                }
            }
        }
        RemoveBoxVertices();
    }

    void LocateRestrictions()
    {
        // A cell is restricted if it is a dead end (only one neighbor) or if it is a corner (two neighbors that are walls).
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pt = new Point(x, y);
                if (!this[pt].HasFlag(CellType.Accessible))
                {
                    continue;
                }
                var neighbors = Neighbors(pt).ToList();
                if (neighbors.Count == 1)
                {
                    this[pt] |= CellType.Restricted;
                    continue;
                }
                if (neighbors.Count == 2)
                {
                    var n1 = neighbors[0];
                    var n2 = neighbors[1];
                    if ((n1.X == x && n2.Y == y) || (n1.Y == y && n2.X == x))
                    {
                        this[pt] |= CellType.Restricted;
                    }
                }
            }
        }
        // All empty cells next to a wall with a protruding wall at each end are restricted.
        // This is a special case of a dead end that is not detected by the previous logic.
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pt = new Point(x, y);
                if (!this[pt].HasFlag(CellType.Accessible))
                {
                    continue;
                }
                if (this[pt].HasFlag(CellType.Restricted))
                {
                    continue;
                }
                if (this[x, y].HasFlag(CellType.Wall))
                {
                    continue;
                }
                // Check horizontal
                if (x > 0 && x < Width - 1 && this[x - 1, y].HasFlag(CellType.Wall) && this[x + 1, y].HasFlag(CellType.Wall))
                {
                    if ((y > 0 && this[x - 1, y - 1].HasFlag(CellType.Wall) && this[x + 1, y - 1].HasFlag(CellType.Wall)) ||
                        (y < Height - 1 && this[x - 1, y + 1].HasFlag(CellType.Wall) && this[x + 1, y + 1].HasFlag(CellType.Wall)))
                    {
                        this[pt] |= CellType.Restricted;
                        continue;
                    }
                }
                // Check vertical
                if (y > 0 && y < Height - 1 && this[x, y - 1].HasFlag(CellType.Wall) && this[x, y + 1].HasFlag(CellType.Wall))
                {
                    if ((x > 0 && this[x - 1, y - 1].HasFlag(CellType.Wall) && this[x - 1, y + 1].HasFlag(CellType.Wall)) ||
                        (x < Width - 1 && this[x + 1, y - 1].HasFlag(CellType.Wall) && this[x + 1, y + 1].HasFlag(CellType.Wall)))
                    {
                        this[pt] |= CellType.Restricted;
                        continue;
                    }
                }
            }
        }
    }

    private void Analyze()
    {
        IdentifyBaseAccessibility();
        LocateRestrictions();
    }

    IEnumerable<Point> Neighbors(Point pt)
    {
        (int dx, int dy)[] deltas = { (0, -1), (1, 0), (0, 1), (-1, 0) };
        foreach (var (dx, dy) in deltas)
        {
            var newx = pt.X + dx;
            var newy = pt.Y + dy;
            if (newx < 0 || newx >= Width || newy < 0 || newy >= Height || this[newx, newy].HasFlag(CellType.Wall))
            {
                continue;
            }
            yield return new Point(newx, newy);
        }
    }

    // Like Neighbors, but also excludes cells currently occupied by a box, which are treated as
    // walls for the purposes of the base accessibility graph.
    IEnumerable<Point> OpenNeighbors(Point pt)
    {
        foreach (var neighbor in Neighbors(pt))
        {
            if (!this[neighbor].HasFlag(CellType.Box))
            {
                yield return neighbor;
            }
        }
    }
    #endregion
}
