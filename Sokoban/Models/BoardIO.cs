using System;
using System.Drawing;

namespace Sokoban.Models;

internal partial class Board
{
    #region IO
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
    }

    static (int dx, int dy) DirectionDelta(Direction dir) => dir switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(dir)),
    };

    bool InBounds(Point pt) => pt.X >= 0 && pt.X < Width && pt.Y >= 0 && pt.Y < Height;

    // Attempt to move the player in the given direction, pushing a box if necessary.
    // Returns true if the player (and possibly a box) moved.
    public bool TryMove(Direction dir)
    {
        var (dx, dy) = DirectionDelta(dir);
        var newPlayerPt = new Point(Player.X + dx, Player.Y + dy);

        if (!InBounds(newPlayerPt) || this[newPlayerPt].HasFlag(CellType.Wall))
        {
            return false;
        }

        var playerPosBefore = Player;
        var ballId = -1;
        var ballPosAfter = default(Point);

        if (this[newPlayerPt].HasFlag(CellType.Box))
        {
            var newBoxPt = new Point(newPlayerPt.X + dx, newPlayerPt.Y + dy);
            if (!InBounds(newBoxPt) || this[newBoxPt].HasFlag(CellType.Wall) || this[newBoxPt].HasFlag(CellType.Box))
            {
                return false;
            }

            this[newPlayerPt] &= ~CellType.Box;
            this[newBoxPt] |= CellType.Box;

            ballId = _boxIds[newPlayerPt];
            _boxIds.Remove(newPlayerPt);
            _boxIds[newBoxPt] = ballId;
            ballPosAfter = newBoxPt;

            // The cell the box moves onto becomes blocked; the cell it vacates becomes open again.
            // Remove before add so the newly-opened vertex's edges don't include the now-blocked cell.
            RemoveVertexAt(newBoxPt);
            AddVertexAt(newPlayerPt);
            this[newPlayerPt] |= CellType.Accessible;
        }

        this[Player] &= ~CellType.Player;
        this[newPlayerPt] |= CellType.Player;
        Player = newPlayerPt;

        PushUndo(new UndoMove(playerPosBefore, ballId, ballPosAfter));
        return true;
    }
    #endregion
}
