using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Brush Tile Palette untuk slope 26 derajat. Satu klik mengisi dua sel sekaligus:
/// setengah kiri (_L) di sel yang diklik dan setengah kanan (_R) di sel sebelahnya,
/// seperti slope panjang di Geometry Dash. Rotasi dan flip brush ikut dihormati:
/// letak sel kedua dan urutan setengahnya dihitung dari geometri kedua setengah
/// (lihat ResolvePlacement), jadi slope hasil flip tetap bersambung, tidak bergeser
/// satu sel seperti kalau arahnya diambil langsung dari arah kanan matrix.
/// Box Fill juga mengisi per pasangan (hanya untuk orientasi mendatar tanpa flip),
/// jadi area miring panjang bisa dibuat sekali drag. Tile selain slope 26 dilukis
/// seperti Default Brush, jadi brush ini aman dipakai untuk semua tile.
///
/// Dipakai dengan memilih tile slope 26 di Tile Palette, lalu mengganti brush ke
/// "Slope 26 Brush" pada dropdown brush. Preview dua sel yang akan diisi digambar
/// oleh Slope26BrushEditor.
/// </summary>
[CustomGridBrush(true, false, false, "Slope 26 Brush")]
public class Slope26Brush : GridBrush
{
    #region Constants

    // Semua tile slope 26 memakai penanda ini di nama asetnya.
    private const string Slope26Marker = "_26";
    private const string LeftSuffix = "_L";
    private const string RightSuffix = "_R";
    private const string AssetExtension = ".asset";

    #endregion

    #region Brush State

    /// <summary>Matrix brush aktif; matrix kosong dari palette diganti identity.</summary>
    public Matrix4x4 BrushMatrix
    {
        get
        {
            BrushCell[] brushCells = cells;
            Matrix4x4 matrix = brushCells != null && brushCells.Length > 0
                ? brushCells[0].matrix
                : Matrix4x4.identity;

            // Tile yang belum pernah diputar/dibalik bisa membawa matrix nol.
            return Mathf.Approximately(matrix.determinant, 0f) ? Matrix4x4.identity : matrix;
        }
    }

    /// <summary>
    /// Arah sel kedua pasangan relatif terhadap sel yang diklik, hasil geometri setelah matrix
    /// brush dipakai. Flip tidak membalik arah pasangan; yang ditukar hanya setengah mana yang
    /// menempati sel pertama.
    /// </summary>
    public Vector3Int PairDirection
    {
        get
        {
            Vector3Int direction;
            bool reversed;
            ResolvePlacement(out direction, out reversed);
            return direction;
        }
    }

    /// <summary>True kalau setengah kanan (_R) menempati sel pertama, yaitu saat tile dicerminkan.</summary>
    public bool IsMirrored
    {
        get
        {
            Vector3Int direction;
            bool reversed;
            ResolvePlacement(out direction, out reversed);
            return reversed;
        }
    }

    #endregion

    #region Painting

    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        Tilemap tilemap = brushTarget != null ? brushTarget.GetComponent<Tilemap>() : null;

        if (tilemap != null && TryResolvePair(out TileBase left, out TileBase right))
        {
            Vector3Int second = position + PairDirection;

            // Sel yang sudah berisi bagian dari pasangan sebelumnya tidak ditimpa.
            // Dengan begitu drag mendatar mengisi tiap dua sel, bukan menumpuk
            // pasangan baru di tengah pasangan lama.
            if (tilemap.GetTile(position) == null && tilemap.GetTile(second) == null)
            {
                TileBase first = IsMirrored ? right : left;
                TileBase last = IsMirrored ? left : right;
                ApplyPair(tilemap, position, second, first, last);
                return;
            }
        }

        // Bukan tile slope 26 (atau selnya sudah terisi): lukis seperti brush biasa.
        base.Paint(gridLayout, brushTarget, position);
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        Tilemap tilemap = brushTarget != null ? brushTarget.GetComponent<Tilemap>() : null;

        if (tilemap != null && ClearPair(tilemap, position))
        {
            return;
        }

        base.Erase(gridLayout, brushTarget, position);
    }

    public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
    {
        Tilemap tilemap = brushTarget != null ? brushTarget.GetComponent<Tilemap>() : null;

        // Hanya orientasi mendatar tanpa flip yang punya arti "per pasangan";
        // orientasi lain jatuh ke Box Fill biasa (satu tile per sel).
        bool pairable = tilemap != null && !IsMirrored && PairDirection == Vector3Int.right;

        if (!pairable || !TryResolvePair(out TileBase left, out TileBase right))
        {
            base.BoxFill(gridLayout, brushTarget, position);
            return;
        }

        // Pasangan dimulai dari tepi kiri kotak yang di-drag, lalu bergeser dua sel.
        // Kotak selebar ganjil menyisakan satu kolom di kanan; kolom itu diisi
        // setengah kiri saja supaya area tidak berlubang.
        for (int y = position.yMin; y < position.yMax; y++)
        {
            for (int z = position.zMin; z < position.zMax; z++)
            {
                for (int x = position.xMin; x < position.xMax; x += 2)
                {
                    Vector3Int first = new Vector3Int(x, y, z);
                    tilemap.SetTile(first, left);
                    tilemap.SetTransformMatrix(first, Matrix4x4.identity);

                    if (x + 1 < position.xMax)
                    {
                        Vector3Int second = new Vector3Int(x + 1, y, z);
                        tilemap.SetTile(second, right);
                        tilemap.SetTransformMatrix(second, Matrix4x4.identity);
                    }
                }
            }
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Cari pasangan setengah kiri dan kanan dari tile yang dipilih. True kalau
    /// tile yang dipilih memang bagian dari slope 26.
    /// </summary>
    public bool TryResolvePair(out TileBase left, out TileBase right)
    {
        left = null;
        right = null;

        BrushCell[] brushCells = cells;
        TileBase picked = brushCells != null && brushCells.Length > 0 ? brushCells[0].tile : null;
        if (picked == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(picked);
        if (string.IsNullOrEmpty(path) ||
            path.IndexOf(Slope26Marker, System.StringComparison.Ordinal) < 0)
        {
            return false;
        }

        if (path.EndsWith(LeftSuffix + AssetExtension, System.StringComparison.Ordinal))
        {
            left = picked;
            right = LoadTile(ReplaceSuffix(path, LeftSuffix + AssetExtension, RightSuffix + AssetExtension));
        }
        else if (path.EndsWith(RightSuffix + AssetExtension, System.StringComparison.Ordinal))
        {
            right = picked;
            left = LoadTile(ReplaceSuffix(path, RightSuffix + AssetExtension, LeftSuffix + AssetExtension));
        }
        else
        {
            string withoutExtension = path.Substring(0, path.Length - AssetExtension.Length);
            left = LoadTile(withoutExtension + LeftSuffix + AssetExtension);
            right = LoadTile(withoutExtension + RightSuffix + AssetExtension);
        }

        return left != null && right != null;
    }

    /// <summary>Cell pertama pasangan untuk satu klik di <paramref name="position"/>.</summary>
    public Vector3Int GetFirstCell(Vector3Int position)
    {
        return position;
    }

    /// <summary>Cell kedua pasangan untuk satu klik di <paramref name="position"/>.</summary>
    public Vector3Int GetSecondCell(Vector3Int position)
    {
        return position + PairDirection;
    }

    // Ujung garis slope 26 di dalam satu sel, relatif terhadap tengah sel. Satu sel dihitung
    // sebagai 1 satuan karena nilainya hanya dipakai membandingkan kedua setengah.
    private static readonly Vector2 LeftHalfStart = new Vector2(-0.5f, -0.5f);
    private static readonly Vector2 LeftHalfEnd = new Vector2(0.5f, 0f);
    private static readonly Vector2 RightHalfStart = new Vector2(-0.5f, 0f);
    private static readonly Vector2 RightHalfEnd = new Vector2(0.5f, 0.5f);

    private static readonly Vector3Int[] CandidateDirections =
    {
        Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down
    };

    /// <summary>
    /// Pilih letak pasangan yang membuat garis kedua setengah tetap bersambung setelah brush
    /// diputar atau dicerminkan. Empat arah sel kedua dicoba berpasangan dengan dua urutan
    /// setengah; susunan dengan jarak ujung garis paling kecil (0 = bersambung tepat) yang dipakai.
    /// Dengan begitu flip X tidak lagi menaruh sel kedua di kiri sel yang diklik.
    /// </summary>
    private bool ResolvePlacement(out Vector3Int direction, out bool reversed)
    {
        direction = Vector3Int.right;
        reversed = false;

        if (!TryResolvePair(out _, out _))
        {
            return false;
        }

        Matrix4x4 matrix = BrushMatrix;
        float best = float.MaxValue;

        for (int d = 0; d < CandidateDirections.Length; d++)
        {
            Vector3Int candidate = CandidateDirections[d];

            for (int order = 0; order < 2; order++)
            {
                bool swapped = order == 1;

                Vector2 firstStart;
                Vector2 firstEnd;
                Vector2 secondStart;
                Vector2 secondEnd;

                EdgeOf(!swapped, Vector2.zero, matrix, out firstStart, out firstEnd);
                EdgeOf(swapped, new Vector2(candidate.x, candidate.y), matrix,
                    out secondStart, out secondEnd);

                float score = Mathf.Min(
                    Mathf.Min(Vector2.Distance(firstStart, secondStart), Vector2.Distance(firstStart, secondEnd)),
                    Mathf.Min(Vector2.Distance(firstEnd, secondStart), Vector2.Distance(firstEnd, secondEnd)));

                if (score < best - 0.001f)
                {
                    best = score;
                    direction = candidate;
                    reversed = swapped;
                }
            }
        }

        return true;
    }

    /// <summary>Ujung garis satu setengah tile setelah matrix brush dipakai, dalam satuan sel.</summary>
    private static void EdgeOf(bool isLeft, Vector2 cellOffset, Matrix4x4 matrix,
        out Vector2 start, out Vector2 end)
    {
        Vector2 localStart = isLeft ? LeftHalfStart : RightHalfStart;
        Vector2 localEnd = isLeft ? LeftHalfEnd : RightHalfEnd;

        Vector3 transformedStart = matrix.MultiplyVector(localStart);
        Vector3 transformedEnd = matrix.MultiplyVector(localEnd);

        start = cellOffset + new Vector2(transformedStart.x, transformedStart.y);
        end = cellOffset + new Vector2(transformedEnd.x, transformedEnd.y);
    }

    private void ApplyPair(Tilemap tilemap, Vector3Int first, Vector3Int second, TileBase firstTile, TileBase secondTile)
    {
        // Matrix brush dipakai di kedua sel, sama seperti Default Brush menaruh
        // satu tile: jadi slope yang diputar/dibalik ikut berputar.
        Matrix4x4 matrix = BrushMatrix;

        tilemap.SetTile(first, firstTile);
        tilemap.SetTransformMatrix(first, matrix);

        tilemap.SetTile(second, secondTile);
        tilemap.SetTransformMatrix(second, matrix);
    }

    /// <summary>
    /// Hapus semua setengah slope 26 di sekitar sel yang diklik (pasangan sebelum,
    /// di, dan sesudahnya). True kalau ada yang terhapus.
    /// </summary>
    private bool ClearPair(Tilemap tilemap, Vector3Int position)
    {
        Vector3Int direction = PairDirection;
        Vector3Int[] neighbours = { position - direction, position, position + direction };

        bool cleared = false;
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (IsHalf(tilemap.GetTile(neighbours[i])))
            {
                tilemap.SetTile(neighbours[i], null);
                cleared = true;
            }
        }

        return cleared;
    }

    private static bool IsHalf(TileBase tile)
    {
        if (tile == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(tile);
        if (string.IsNullOrEmpty(path) ||
            path.IndexOf(Slope26Marker, System.StringComparison.Ordinal) < 0)
        {
            return false;
        }

        return path.EndsWith(LeftSuffix + AssetExtension, System.StringComparison.Ordinal) ||
            path.EndsWith(RightSuffix + AssetExtension, System.StringComparison.Ordinal);
    }

    private static TileBase LoadTile(string path)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(path);
    }

    private static string ReplaceSuffix(string path, string oldSuffix, string newSuffix)
    {
        return path.Substring(0, path.Length - oldSuffix.Length) + newSuffix;
    }

    #endregion
}
