using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sokoban.Models;

[Flags]
internal enum CellType
{
    Empty = 0,
    Wall = 1 << 0,
    Box = 1 << 1,
    Goal = 1 << 2,
    Player = 1 << 3
}

internal class Board
{
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

    public int Width { get; private set; }
    public int Height { get; private set; }

    public CellType[] Cells { get; private set; }

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

    public void Load(String map)
    {
        var lines = map.Replace("\r", "").Split('\n');
        Height = lines.Length;
        Width = lines.Max(line => line.Length);
        Cells = new CellType[Width * Height];

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
                        Cells[ich] = CellType.Player;
                        player = ich;
                        break;
                    case '*':
                        Cells[ich] = CellType.Box | CellType.Goal;
                        break;
                    case '+':
                        Cells[ich] = CellType.Player | CellType.Goal;
                        player = ich;
                        break;
                    default:
                        Cells[ich] = CellType.Empty;
                        break;
                }
            }
        }
    }
}
