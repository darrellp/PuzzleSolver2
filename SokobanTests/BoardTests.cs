using Sokoban.Models;

namespace SokobanTests;

[TestClass]
public class BoardTests
{
    [TestMethod]
    public void Constructor_SetsDimensionsAndEmptyCells()
    {
        var board = new Board(4, 3);

        Assert.AreEqual(4, board.Width);
        Assert.AreEqual(3, board.Height);
        Assert.HasCount(12, board.Cells);
        Assert.IsTrue(board.Cells.All(c => c.type == CellType.Empty));
    }

    [TestMethod]
    public void Load_ParsesEachCellType()
    {
        var board = new Board("#$.@*+ ");

        Assert.AreEqual(CellType.Wall, board.Cells[0].type);
        Assert.AreEqual(CellType.Box, board.Cells[1].type);
        Assert.AreEqual(CellType.Goal, board.Cells[2].type);
        Assert.AreEqual(CellType.Player, board.Cells[3].type);
        Assert.AreEqual(CellType.Box | CellType.Goal, board.Cells[4].type);
        Assert.AreEqual(CellType.Player | CellType.Goal, board.Cells[5].type);
        Assert.AreEqual(CellType.Empty, board.Cells[6].type);
    }

    [TestMethod]
    public void Load_MultipleLines_SetsDimensions()
    {
        var board = new Board("####\n#@ #\n####");

        Assert.AreEqual(4, board.Width);
        Assert.AreEqual(3, board.Height);
        Assert.AreEqual(CellType.Player, board.Cells[5].type);
    }

    [TestMethod]
    public void Load_IgnoresCarriageReturns()
    {
        var board = new Board("###\r\n#.#\r\n###");

        Assert.AreEqual(3, board.Width);
        Assert.AreEqual(3, board.Height);
        Assert.AreEqual(CellType.Goal, board.Cells[4].type);
    }

    [TestMethod]
    public void Load_RaggedLines_UsesLongestLineForWidth()    {
        var board = new Board("#####\n#@#\n#####");

        Assert.AreEqual(5, board.Width);
        Assert.AreEqual(3, board.Height);
    }

    [TestMethod]
    public void Load_Default()
    {
        var board = new Board();
        Assert.AreEqual(11, board.Height);
        Assert.AreEqual(14, board.Width);
    }
}
