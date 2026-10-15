using System;
using System.Drawing;

namespace Sokoban.Models;

internal partial class Board
{
    #region Undo
    private readonly struct UndoMove
    {
        public UndoMove(Point playerPos, int ballId, Point ballPos)
        {
            PlayerPos = playerPos;
            BallId = ballId;
            BallPos = ballPos;
        }

        // Player's position prior to the move being undone.
        public Point PlayerPos { get; }
        // Identifier of the ball (box) that was pushed, or -1 if none.
        public int BallId { get; }
        // Ball's position after the move (i.e. its current position, to be reversed).
        public Point BallPos { get; }
    }

    private const int UndoCapacity = 100;
    private readonly UndoMove[] _undoBuffer = new UndoMove[UndoCapacity];
    private int _undoHead;
    private int _undoCount;

    private void PushUndo(UndoMove move)
    {
        _undoBuffer[_undoHead] = move;
        _undoHead = (_undoHead + 1) % UndoCapacity;
        if (_undoCount < UndoCapacity)
        {
            _undoCount++;
        }
    }

    private void ClearUndo()
    {
        _undoHead = 0;
        _undoCount = 0;
    }

    // Reverses the most recent move, restoring the player (and any pushed ball) to its prior position.
    public bool Undo()
    {
        if (_undoCount == 0)
        {
            return false;
        }

        _undoHead = (_undoHead - 1 + UndoCapacity) % UndoCapacity;
        _undoCount--;
        var move = _undoBuffer[_undoHead];

        if (move.BallId != -1)
        {
            this[move.BallPos] &= ~CellType.Box;
            this[Player] |= CellType.Box;
            _boxIds.Remove(move.BallPos);
            _boxIds[Player] = move.BallId;

            // The cell the box is leaving becomes open again; the cell it moves onto (the player's
            // current position) becomes blocked. Add before remove so the new vertex's edges are
            // computed while the soon-to-be-blocked cell is still open.
            AddVertexAt(move.BallPos);
            RemoveVertexAt(Player);
        }

        this[Player] &= ~CellType.Player;
        this[move.PlayerPos] |= CellType.Player;
        Player = move.PlayerPos;
        return true;
    }
    #endregion
}
