using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor untuk Slope26Brush: menggambar preview dua sel yang akan diisi saat
/// melukis. Brush kustom tidak memakai preview Default Brush, jadi kotak ini
/// memperlihatkan letak setengah kiri (terang) dan setengah kanan (redup) pada
/// posisi kursor, termasuk setelah brush diputar atau dibalik.
/// </summary>
[CustomEditor(typeof(Slope26Brush))]
public class Slope26BrushEditor : GridBrushEditor
{
    #region Colors

    private static readonly Color FirstFace = new Color(1f, 0.86f, 0.25f, 0.22f);
    private static readonly Color FirstOutline = new Color(1f, 0.86f, 0.25f, 1f);
    private static readonly Color SecondFace = new Color(1f, 0.86f, 0.25f, 0.08f);
    private static readonly Color SecondOutline = new Color(1f, 0.86f, 0.25f, 0.55f);

    #endregion

    #region Preview

    public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget,
        BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        Slope26Brush brush = target as Slope26Brush;
        if (brush == null || !brush.TryResolvePair(out TileBase left, out TileBase right))
        {
            return;
        }

        Vector3Int cursor = position.position;
        Vector3Int firstCell = brush.GetFirstCell(cursor);
        Vector3Int secondCell = brush.GetSecondCell(cursor);

        DrawCell(grid, secondCell, SecondFace, SecondOutline);
        DrawCell(grid, firstCell, FirstFace, FirstOutline);
    }

    private static void DrawCell(GridLayout grid, Vector3Int cell, Color face, Color outline)
    {
        Vector3 corner = grid.CellToWorld(cell);
        Vector3 size = grid.cellSize;

        Vector3[] verts =
        {
            new Vector3(corner.x, corner.y, 0f),
            new Vector3(corner.x + size.x, corner.y, 0f),
            new Vector3(corner.x + size.x, corner.y + size.y, 0f),
            new Vector3(corner.x, corner.y + size.y, 0f),
        };

        Handles.DrawSolidRectangleWithOutline(verts, face, outline);
    }

    #endregion
}
