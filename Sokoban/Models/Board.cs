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

    public CellType[] Cells { get; private set; }
    #endregion

    #region Cell access
    public CellType this[int index]
    {
        get => Cells[index];
        set => Cells[index] = value;
    }

    public CellType this[Point pt]
    {
        get => Cells[PtToIndex(pt)];
        set => Cells[PtToIndex(pt)] = value;
    }

    public CellType this[int x, int y]
    {
        get => Cells[y * Width + x];
        set => Cells[y * Width + x] = value;
    }
    Point Player { get; set; }

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
        }

        this[Player] &= ~CellType.Player;
        this[newPlayerPt] |= CellType.Player;
        Player = newPlayerPt;

        PushUndo(new UndoMove(playerPosBefore, ballId, ballPosAfter));
        return true;
    }

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
        }

        this[Player] &= ~CellType.Player;
        this[move.PlayerPos] |= CellType.Player;
        Player = move.PlayerPos;
        return true;
    }
    #endregion

    int PtToIndex(Point pt)
    {
        return pt.Y * Width + pt.X;
    }

    Point IndexToPt(int i)
    {
        return new Point(i % Width, i / Width);
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
    #endregion

    // Maps a box's current position to its stable identifier, assigned at load time.
    private Dictionary<Point, int> _boxIds = new();

    #region Constructors
    public Board(int width, int height)
    {
        Cells = new CellType[width * height];
        Width = width;
        Height = height;
        Cells = new CellType[width * height];
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
        var vertices = new Dictionary<Point, Vertex<VtxInfo, object>>();
        var processed = new HashSet<Point>();
        var queue = new Queue<Point>();

        vertices[Player] = InsertVtx(Player);
        queue.Enqueue(Player);
        while (queue.TryDequeue(out Point cur))
        {
            this[cur] |= CellType.Accessible;
            processed.Add(cur);
            foreach (Point pt in Neighbors(cur))
            {
                if (!vertices.TryGetValue(pt, out var vtx))
                {
                    vtx = InsertVtx(pt);
                    vertices[pt] = vtx;
                    queue.Enqueue(pt);
                }
                // Neighbors already processed have already connected to cur
                if (!processed.Contains(pt))
                {
                    _baseGraph.AddDoubleEdge(vertices[cur], vtx);
                }
            }
        }
    }

    private void Analyze()
    {
        IdentifyBaseAccessibility();
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
        Cells = new CellType[Width * Height];
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
                        Cells[ich] = CellType.Wall;
                        break;
                    case '$':
                        Cells[ich] = CellType.Box;
                        break;
                    case '.':
                        Cells[ich] = CellType.Goal;
                        break;
                    case '@':
                        if (foundPlayer)
                        {
                            throw new InvalidOperationException("Multiple players found in a map");
                        }
                        foundPlayer = true;
                        Player = IndexToPt(ich);
                        Cells[ich] = CellType.Player;
                        player = ich;
                        break;
                    case '*':
                        Cells[ich] = CellType.Box | CellType.Goal;
                        break;
                    case '+':
                        if (foundPlayer)
                        {
                            throw new InvalidOperationException("Multiple players found in a map");
                        }
                        foundPlayer = true;
                        Player = IndexToPt(ich);
                        Cells[ich] = CellType.Player | CellType.Goal;
                        player = ich;
                        break;
                    default:
                        Cells[ich] = CellType.Empty;
                        break;
                }
            }
        }

        _boxIds = new Dictionary<Point, int>();
        var nextBoxId = 0;
        for (int i = 0; i < Cells.Length; i++)
        {
            if (Cells[i].HasFlag(CellType.Box))
            {
                _boxIds[IndexToPt(i)] = nextBoxId++;
            }
        }
        ClearUndo();

        Analyze();
    }
    #endregion
}
