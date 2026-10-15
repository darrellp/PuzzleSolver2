using System;
using System.Collections.Generic;
using System.Drawing;
using Combinatorial_Algorithms;
using Combinatorial_Algorithms.Graph_Structures;

namespace Sokoban.Models;

internal partial class Board
{
    #region Graph theory
    public class VtxInfo
    {
        Point ptCell;

        public VtxInfo(Point ptCell)
        {
            this.ptCell = ptCell;
        }
    }

    Vertex<VtxInfo, object> InsertVtx(Point pt)
    {
        return _baseGraph.InsertVertex(attr: new VtxInfo(pt));
    }

    private Graph<VtxInfo, object> _baseGraph = new Graph<VtxInfo, object>();

    // Removes the vertex at every cell currently holding a box, since boxes block movement.
    // Call after the base graph has been fully built (end of IdentifyBaseAccessibility).
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

    // Removes the vertex (if any) at pt from the base graph, along with its edges.
    // Used when a box moves onto a previously open cell, turning it into an effective wall.
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
}
