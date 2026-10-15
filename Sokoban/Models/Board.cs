using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Combinatorial_Algorithms;
using Combinatorial_Algorithms.Graph_Structures;
using Vertex = Combinatorial_Algorithms.Graph_Structures.Vertex<Sokoban.Models.Board.VtxInfo, object>;

namespace Sokoban.Models;

[Flags]
internal enum CellType
{
    Empty = 0,
    Wall = 1 << 0,
    Box = 1 << 1,
    Goal = 1 << 2,
    Player = 1 << 3,
    Restricted = 1 << 4,
    Accessible = 1 << 5,
}

internal class Board
{
    #region Initial Board
    public const String InitialBoardPlan = """
           ###
          ## # ####
         ##  ###  #
        ## $      #
        #   @$ #  #
        ### $###  #
          #  #..  #
         ## ##.# ##
         #      ##
         #     ##
         #######
        """;
    #endregion

    #region public properties
    public int Width { get; private set; }
    public int Height { get; private set; }

    public struct CellInfo
    {
        public CellType type;

        // Vertex in the base graph for this cell; null for now.
        public Vertex? vtx;
    }

    public CellInfo[] Cells { get; private set; }
    #endregion

    #region Cell access
    public CellType this[int index]
    {
        get => Cells[index].type;
        set => Cells[index].type = value;
    }

    public CellType this[Point pt]
    {
        get => Cells[PtToIndex(pt)].type;
        set => Cells[PtToIndex(pt)].type = value;
    }

    public CellType this[int x, int y]
    {
        get => Cells[y * Width + x].type;
        set => Cells[y * Width + x].type = value;
    }
    Point Player { get; set; }

    int PtToIndex(Point pt)
    {
        return pt.Y * Width + pt.X;
    }

    Point IndexToPt(int i)
    {
        return new Point(i % Width, i / Width);
    }

    // Maps a box's current position to its stable identifier, assigned at load time.
    private Dictionary<Point, int> _boxIds = new();
    #endregion

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

    #region Graph theory
    public class VtxInfo
    {
        Point ptCell;

        public VtxInfo(Point ptCell)
        {
            this.ptCell=ptCell;
        }
    }
    Vertex<VtxInfo, object> InsertVtx(Point pt)
    {
        return _baseGraph.InsertVertex(attr: new VtxInfo(pt));
    }

    private Graph<VtxInfo, object> _baseGraph = new Graph<VtxInfo, object>();

    // Maps an open (non-wall, non-box) cell to its vertex in _baseGraph.
    //private Dictionary<Point, Vertex<VtxInfo, object>> _vertices = new();

    // Removes the vertex (if any) at pt from the base graph, along with its edges.
    // Used when a box moves onto a previously open cell, turning it into an effective wall.
    private void RemoveBoxVertices() 
    { 
        for (int i = 0; i < Cells.Length; i++) 
        { 
            if (Cells[i].type.HasFlag(CellType.Box)) 
            { 
                RemoveVertexAt(IndexToPt(i)); 
            } 
        } 
    }

    private void RemoveVertexAt(Point pt)
    {
        var index = PtToIndex(pt);
        var vtx = Cells[index].vtx;
        if (vtx != null)
        {
            _baseGraph.DeleteVertex(vtx.ID);
            Cells[index].vtx = null;
        }
    }

    // Adds a vertex at pt to the base graph, hooking it up to any already-present neighboring vertices.
    // Used when a box moves off of a cell, turning it back into an open, traversable cell.
    private void AddVertexAt(Point pt)
    {
        var vtx = InsertVtx(pt);
        Cells[PtToIndex(pt)].vtx = vtx;
        foreach (var neighbor in OpenNeighbors(pt))
        {
            var nvtx = Cells[PtToIndex(neighbor)].vtx;
            if (nvtx != null)
            {
                if (vtx.FindOutEdge(nvtx.ID) == null)
                {
                    _baseGraph.AddDoubleEdge(vtx, nvtx);
                }
            }
        }
    }
    #endregion

    #region Constructors
    public Board(int width, int height)
    {
        Cells = new CellInfo[width * height];
        Width = width;
        Height = height;
    }

    public Board(String map)
    {
        Load(map);
    }

    public Board() : this(InitialBoardPlan) { }
    #endregion

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

    #region Loading
    private String _initialMap = String.Empty;

    public void Reset()
    {
        Load(_initialMap);
    }

    public void Load(String map)
    {
        _initialMap = map;
        _baseGraph = new Graph<VtxInfo, object>();
        var lines = map.Replace("\r", "").Split('\n');
        Height = lines.Length;
        Width = lines.Max(line => line.Length);
        Cells = new CellInfo[Width * Height];
        var foundPlayer = false;

        int player;

        for (int y = 0; y < Height; y++)
        {
            var line = lines[y];
            for (int x = 0; x < line.Length; x++)
            {
                var ich = y * Width + x;
                var cell = line[x];
                switch (cell)
                {
                    case '#':
                        Cells[ich].type = CellType.Wall;
                        break;
                    case '$':
                        Cells[ich].type = CellType.Box;
                        break;
                    case '.':
                        Cells[ich].type = CellType.Goal;
                        break;
                    case '@':
                        if (foundPlayer)
                        {
                            throw new InvalidOperationException("Multiple players found in a map");
                        }
                        foundPlayer = true;
                        Player = IndexToPt(ich);
                        Cells[ich].type = CellType.Player;
                        player = ich;
                        break;
                    case '*':
                        Cells[ich].type = CellType.Box | CellType.Goal;
                        break;
                    case '+':
                        if (foundPlayer)
                        {
                            throw new InvalidOperationException("Multiple players found in a map");
                        }
                        foundPlayer = true;
                        Player = IndexToPt(ich);
                        Cells[ich].type = CellType.Player | CellType.Goal;
                        player = ich;
                        break;
                    default:
                        Cells[ich].type = CellType.Empty;
                        break;
                }
            }
        }

        _boxIds = new Dictionary<Point, int>();
        var nextBoxId = 0;
        for (int i = 0; i < Cells.Length; i++)
        {
            if (Cells[i].type.HasFlag(CellType.Box))
            {
                _boxIds[IndexToPt(i)] = nextBoxId++;
            }
        }
        ClearUndo();

        Analyze();
    }
    #endregion
}
