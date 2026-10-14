using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Sokoban.Models;

namespace Sokoban.Views;

internal class BoardControl : Control
{
    public static readonly StyledProperty<Board?> BoardProperty =
        AvaloniaProperty.Register<BoardControl, Board?>(nameof(Board));

    public static readonly StyledProperty<double> CellSizeProperty =
        AvaloniaProperty.Register<BoardControl, double>(nameof(CellSize), 32.0);

    static BoardControl()
    {
        AffectsRender<BoardControl>(BoardProperty, CellSizeProperty);
        AffectsMeasure<BoardControl>(BoardProperty, CellSizeProperty);
    }

    public BoardControl()
    {
        Focusable = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var dir = e.Key switch
        {
            Key.Up or Key.W => Board.Direction.Up,
            Key.Down or Key.S => Board.Direction.Down,
            Key.Left or Key.A => Board.Direction.Left,
            Key.Right or Key.D => Board.Direction.Right,
            _ => (Board.Direction?)null,
        };

        if (dir is { } direction && Board is { } board)
        {
            if (board.TryMove(direction))
            {
                InvalidateVisual();
            }
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    public double CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    public Board? Board
    {
        get => GetValue(BoardProperty);
        set => SetValue(BoardProperty, value);
    }

    static readonly IBrush FloorBrush = new SolidColorBrush(Color.Parse("#C0C0C0"));
    static readonly IBrush Light = new SolidColorBrush(Color.Parse("#F4F4F4"));
    static readonly IBrush Dark = new SolidColorBrush(Color.Parse("#707070"));
    static readonly IBrush WallBrush = new SolidColorBrush(Color.Parse("#A0A0A0"));

    protected override Size MeasureOverride(Size availableSize)
    {
        var board = Board;
        if (board == null) return default;
        var w = board.Width;
        var h = board.Height;
        if (w == 0 || h == 0) return default;
        var cell = CellSize;
        return new Size(cell * w, cell * h);
    }

    public override void Render(DrawingContext context)
    {
        var board = Board;
        if (board == null || board.Width == 0 || board.Height == 0) return;

        var cell = CellSize;
        if (cell <= 0) return;
        var bevel = Math.Max(1, cell / 8);

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                var type = board.Cells[y * board.Width + x];
                var rect = new Rect(x * cell, y * cell, cell, cell);

                if (type.HasFlag(CellType.Wall))
                {
                    DrawWall(context, rect, bevel,
                        !IsWall(board, x, y - 1), !IsWall(board, x + 1, y),
                        !IsWall(board, x, y + 1), !IsWall(board, x - 1, y));
                    DrawInnerCorners(context, rect, bevel, board, x, y);
                    continue;
                }

                if (type == CellType.Empty)
                    continue;

                context.FillRectangle(FloorBrush, rect);
                bool goal = type.HasFlag(CellType.Goal);
                if (goal)
                    DrawBevel(context, rect.Deflate(rect.Width * 0.1), bevel, FloorBrush, Dark, Light);

                if (type.HasFlag(CellType.Box))
                    DrawBall(context, rect, goal ? Colors.LimeGreen : Colors.Gold);
                else if (type.HasFlag(CellType.Player))
                    DrawBall(context, rect, Colors.Red);
            }
        }
    }

    static void DrawInnerCorners(DrawingContext context, Rect r, double b, Board board, int x, int y)
    {
        bool up = IsWall(board, x, y - 1), down = IsWall(board, x, y + 1);
        bool left = IsWall(board, x - 1, y), right = IsWall(board, x + 1, y);

        if (up && right && !IsWall(board, x + 1, y - 1))
        {
            FillPoly(context, Dark, new Point(r.Right - b, r.Top), new Point(r.Right, r.Top),
                new Point(r.Right - b, r.Top + b), new Point(r.Right - b, r.Top));
            FillPoly(context, Light, new Point(r.Right, r.Top), new Point(r.Right, r.Top + b),
                new Point(r.Right - b, r.Top + b), new Point(r.Right, r.Top));
        }
        if (up && left && !IsWall(board, x - 1, y - 1))
            context.FillRectangle(Light, new Rect(r.Left, r.Top, b, b));
        if (down && right && !IsWall(board, x + 1, y + 1))
            context.FillRectangle(Dark, new Rect(r.Right - b, r.Bottom - b, b, b));
        if (down && left && !IsWall(board, x - 1, y + 1))
        {
            FillPoly(context, Dark, new Point(r.Left, r.Bottom - b), new Point(r.Left, r.Bottom),
                new Point(r.Left + b, r.Bottom - b), new Point(r.Left, r.Bottom - b));
            FillPoly(context, Light, new Point(r.Left + b, r.Bottom - b), new Point(r.Left + b, r.Bottom),
                new Point(r.Left, r.Bottom), new Point(r.Left + b, r.Bottom - b));
        }
    }

    static bool IsWall(Board board, int x, int y) =>        x >= 0 && y >= 0 && x < board.Width && y < board.Height &&
        board.Cells[y * board.Width + x].HasFlag(CellType.Wall);

    static void DrawWall(DrawingContext context, Rect r, double b, bool top, bool right, bool bottom, bool left)
    {
        context.FillRectangle(WallBrush, r);

        if (top)
            FillPoly(context, Light, r.TopLeft, r.TopRight,
                new Point(right ? r.Right - b : r.Right, r.Top + b),
                new Point(left ? r.Left + b : r.Left, r.Top + b));
        if (left)
            FillPoly(context, Light, r.TopLeft, r.BottomLeft,
                new Point(r.Left + b, bottom ? r.Bottom - b : r.Bottom),
                new Point(r.Left + b, top ? r.Top + b : r.Top));
        if (bottom)
            FillPoly(context, Dark, r.BottomLeft, r.BottomRight,
                new Point(right ? r.Right - b : r.Right, r.Bottom - b),
                new Point(left ? r.Left + b : r.Left, r.Bottom - b));
        if (right)
            FillPoly(context, Dark, r.TopRight, r.BottomRight,
                new Point(r.Right - b, bottom ? r.Bottom - b : r.Bottom),
                new Point(r.Right - b, top ? r.Top + b : r.Top));
    }

    static void FillPoly(DrawingContext context, IBrush brush, Point a, Point b, Point c, Point d)
    {
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(a, true);
            g.LineTo(b);
            g.LineTo(c);
            g.LineTo(d);
            g.EndFigure(true);
        }
        context.DrawGeometry(brush, null, geo);
    }

    static void DrawBevel(DrawingContext context, Rect r, double b, IBrush face, IBrush topLeft, IBrush bottomRight)
    {
        context.FillRectangle(face, r);
        var tl = new StreamGeometry();
        using (var g = tl.Open())
        {
            g.BeginFigure(r.TopLeft, true);
            g.LineTo(r.TopRight);
            g.LineTo(new Point(r.Right - b, r.Top + b));
            g.LineTo(new Point(r.Left + b, r.Top + b));
            g.LineTo(new Point(r.Left + b, r.Bottom - b));
            g.LineTo(r.BottomLeft);
            g.EndFigure(true);
        }
        var br = new StreamGeometry();
        using (var g = br.Open())
        {
            g.BeginFigure(r.BottomRight, true);
            g.LineTo(r.BottomLeft);
            g.LineTo(new Point(r.Left + b, r.Bottom - b));
            g.LineTo(new Point(r.Right - b, r.Bottom - b));
            g.LineTo(new Point(r.Right - b, r.Top + b));
            g.LineTo(r.TopRight);
            g.EndFigure(true);
        }
        context.DrawGeometry(topLeft, null, tl);
        context.DrawGeometry(bottomRight, null, br);
    }

    static void DrawBall(DrawingContext context, Rect r, Color color)
    {
        var inner = r.Deflate(r.Width * 0.15);
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.35, 0.35, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.35, 0.35, RelativeUnit.Relative),
            RadiusX = RelativeScalar.Parse("70%"),
            RadiusY = RelativeScalar.Parse("70%"),
            GradientStops =
            {
                new GradientStop(Colors.White, 0),
                new GradientStop(color, 0.45),
                new GradientStop(Color.FromRgb((byte)(color.R / 3), (byte)(color.G / 3), (byte)(color.B / 3)), 1)
            }
        };
        context.DrawEllipse(brush, null, inner.Center, inner.Width / 2, inner.Height / 2);
    }
}
