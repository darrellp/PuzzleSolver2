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

internal partial class Board
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
