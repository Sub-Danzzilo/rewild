using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Membangun Tilemap platform sebuah level dari daftar blok segi empat di Inspector.
/// Tile tanah (fill) diisi ke Tilemap_Ground, sedangkan tepi dan sudut permukaan
/// (rumput / es / oli) dipasang otomatis ke Tilemap_Surface_* sesuai bentuk blok,
/// lengkap dengan rotasi dan sudut luar / dalam.
///
/// Skrip berjalan juga di Edit Mode supaya hasilnya langsung terlihat di Scene view
/// dan ikut tersimpan bersama scene. Setelah mulai melukis tile secara manual,
/// matikan Build On Enable agar hasil lukisan tidak ditimpa.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class LevelTilemapBuilder : MonoBehaviour
{
    #region Inspector Fields

    [Header("Surface Materials")]
    [Tooltip("Daftar material permukaan (rumput, es, oli). Index 0 dipakai sebagai material default.")]
    [SerializeField] private SurfaceSet[] surfaces = new SurfaceSet[0];

    [Header("Level Shape")]
    [Tooltip("Balok penyusun level dalam satuan tile (1 tile = 1 unit). origin = sel kiri-bawah.")]
    [SerializeField] private Block[] blocks = new Block[0];

    [Header("Tilemap")]
    [Tooltip("Layer Unity untuk tilemap. 3 = Platform, sesuai deteksi pijakan PlayerController.")]
    [SerializeField] private int platformLayer = 3;

    [Tooltip("Ukuran satu tile dalam unit dunia. 4 = sprite 100px digambar 4x lebih besar.")]
    [SerializeField] private float tileSize = 4f;

    [Tooltip("Sorting order Tilemap_Ground (tanah).")]
    [SerializeField] private int fillSortingOrder = 0;

    [Tooltip("Sorting order Tilemap_Surface (lapisan permukaan), harus lebih besar dari tanah.")]
    [SerializeField] private int surfaceSortingOrder = 1;

    [Tooltip("Bangun otomatis saat scene dibuka, hanya jika tilemap masih kosong.")]
    [SerializeField] private bool buildOnEnable = true;

    // Ukuran tile saat build terakhir; dipakai untuk mendeteksi perubahan tileSize.
    [SerializeField, HideInInspector] private float lastBuiltTileSize;

    #endregion

    #region Nested Types

    /// <summary>Satu set sprite untuk satu material permukaan.</summary>
    [System.Serializable]
    public class SurfaceSet
    {
        public string name = "Grass";
        public Sprite fill;
        public Sprite edge;
        public Sprite cornerOuter;
        public Sprite cornerInner;

        [Tooltip("Buat lapisan permukaan kedua (mis. Tilemap_Surface_Grass_2) supaya satu sel bisa " +
                 "memakai fringe atas dan fringe bawah sekaligus.")]
        public bool stackable = true;
    }

    /// <summary>Satu balok platform dalam satuan tile.</summary>
    [System.Serializable]
    public class Block
    {
        public Vector2Int origin = Vector2Int.zero;
        public Vector2Int size = new Vector2Int(4, 1);

        [Tooltip("Index material pada daftar Material Permukaan.")]
        public int surface = 0;
    }

    /// <summary>Sisi sel yang terbuka (tetangga platform tidak ada di sisi itu).</summary>
    [System.Flags]
    public enum OpenSide
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8
    }

    /// <summary>Tile permukaan yang dipilih beserta sisi yang sudah tertutup olehnya.</summary>
    public struct SurfaceChoice
    {
        public Sprite sprite;
        public float rotation;
        public OpenSide covered;

        public bool IsValid
        {
            get { return sprite != null; }
        }
    }

    #endregion

    #region Constants

    private const string GroundName = "Tilemap_Ground";
    private const string SurfaceNamePrefix = "Tilemap_Surface_";

    // Lapisan permukaan kedua untuk material yang sama, misalnya
    // "Tilemap_Surface_Grass_2". Satu sel tilemap hanya bisa menyimpan satu tile,
    // jadi platform setinggi satu sel tidak bisa punya fringe atas dan fringe bawah
    // sekaligus. Dengan dua lapis permukaan, sisi atas dan sisi bawah bisa
    // ditumpuk di sel yang sama.
    private const string StackedSurfaceSuffix = "_2";

    // Tile slope tidak boleh ikut dirapikan skalanya. Tile 45° mengisi satu sel; tile 26° juga
    // tepat satu sel (gambar baji, sprite 2 x 2 unit, pivot tengah). Collider-nya hanya mengisi
    // bagian baji di dalam sel, jadi batas sel juga tidak bisa dipakai sebagai batas collider.
    private const string SlopeTileNamePrefix = "Slope_";

    // Rotasi tile dalam derajat, berlawanan arah jarum jam.
    // Sprite asli: material di sisi ATAS (edge), material di ATAS + KIRI (corner).
    private const float RotTop = 0f;
    private const float RotLeft = 90f;
    private const float RotBottom = 180f;
    private const float RotRight = 270f;

    #endregion

    #region Runtime State

    private Tilemap groundTilemap;
    private readonly Dictionary<int, Tilemap> surfaceTilemaps = new Dictionary<int, Tilemap>();
    private readonly Dictionary<int, Tilemap> stackedSurfaceTilemaps = new Dictionary<int, Tilemap>();
    private readonly HashSet<Vector3Int> shape = new HashSet<Vector3Int>();
    private readonly Dictionary<Vector3Int, int> cellSurface = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();
    private readonly List<Tile> generatedTiles = new List<Tile>();
    private bool building;

    #endregion

    #region Unity Callbacks

    private void OnEnable()
    {
        if (!buildOnEnable || building)
        {
            return;
        }

        // Jangan timpa tile yang sudah ada (misalnya hasil lukis manual).
        if (!NeedsRebuild())
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Lapisan permukaan kedua tetap disiapkan supaya Tile Palette selalu
                // punya target untuk menumpuk fringe, tanpa menimpa lukisan manual.
                UnityEditor.EditorApplication.delayCall += EnsureStackedSurfaceLayers;
                UnityEditor.EditorApplication.delayCall += VerifyPlatformCollider;
                return;
            }
#endif
            EnsureStackedSurfaceLayers();
            EnsurePlatformCollider();
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Tunda satu frame supaya aman saat scene baru selesai dibuka.
            UnityEditor.EditorApplication.delayCall += BuildIfStillNeeded;
            return;
        }
#endif
        Build();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= BuildIfStillNeeded;
        UnityEditor.EditorApplication.delayCall -= VerifyPlatformCollider;
        UnityEditor.EditorApplication.delayCall -= EnsureStackedSurfaceLayers;
#endif
    }

#if UNITY_EDITOR
    private void VerifyPlatformCollider()
    {
        UnityEditor.EditorApplication.delayCall -= VerifyPlatformCollider;

        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        EnsurePlatformCollider();
    }
#endif

#if UNITY_EDITOR
    private void BuildIfStillNeeded()
    {
        UnityEditor.EditorApplication.delayCall -= BuildIfStillNeeded;

        if (this == null || !isActiveAndEnabled || building || !NeedsRebuild())
        {
            return;
        }

        Build();
    }
#endif

    #endregion

    #region Public Methods

    /// <summary>Bangun ulang seluruh tilemap dari daftar blok. Tile lama dihapus dulu.</summary>
    [ContextMenu("Bangun Ulang Tilemap")]
    public void Build()
    {
        if (building)
        {
            return;
        }

        building = true;
        try
        {
            ReleaseGeneratedTiles();
            EnsureDefaults();
            EnsureStructure();
            CollectShape();
            PaintTiles();
            RebuildColliderGeometry();
            lastBuiltTileSize = Mathf.Max(0.01f, tileSize);
            MarkDirty();
        }
        finally
        {
            building = false;
        }
    }

    /// <summary>
    /// Perbaiki hanya collider platform, tanpa melukis ulang tile, supaya hasil lukisan manual aman.
    /// </summary>
    [ContextMenu("Perbaiki Collider Platform")]
    public void FixPlatformColliderMenu()
    {
        EnsurePlatformCollider();
        Debug.Log("[LevelTile] " + DescribeGroundBounds(), this);
    }

    /// <summary>
    /// Collider platform harus sama persis dengan tile yang dilukis. CompositeCollider2D
    /// menyimpan geometri hasil generate di dalam scene; kalau tile pernah dibuat dengan
    /// sprite atau ukuran tile lain, geometri itu bisa lebih besar dari gambar tile sehingga
    /// Webi berdiri di atas collider tak terlihat (melayang) dan menabrak dinding tak terlihat.
    /// </summary>
    public void EnsurePlatformCollider()
    {
        if (building)
        {
            return;
        }

        bool repaired = RepairPaintedTiles();
        bool scaleFixed = NormalizePaintedTileScales();
        if (!repaired && !scaleFixed && !PlatformColliderMismatch())
        {
            return;
        }

        building = true;
        try
        {
            RebuildColliderGeometry();
        }
        finally
        {
            building = false;
        }

        Debug.Log(
            "[LevelTile] " +
            (repaired
                ? "Tile tanpa sprite sudah diganti tile tanah standar; "
                : scaleFixed
                    ? "Skala tile yang bukan satu sel sudah dirapikan; "
                    : "Collider platform tidak cocok dengan tile yang dilukis; ") +
            "collider dibuat ulang dari tile yang benar-benar ada. " + DescribeGroundBounds(),
            this
        );
        MarkDirty();
    }

    /// <summary>
    /// Tile lama yang sprite-nya sudah tidak ada tetap menyimpan collider (bahkan dua kali sel),
    /// jadi platform punya lantai / dinding tak terlihat: Webi tampak melayang dan tidak bisa
    /// berjalan. Sel tanah seperti itu diganti tile tanah standar dengan collider per sel,
    /// sedangkan sel lapisan permukaan yang sprite-nya hilang dibuang.
    /// </summary>
    public bool RepairPaintedTiles()
    {
        bool changed = false;
        Sprite fillSprite = surfaces != null && surfaces.Length > 0 ? surfaces[0].fill : null;

        Transform ground = transform.Find(GroundName);
        Tilemap groundTilemap = ground != null ? ground.GetComponent<Tilemap>() : null;
        if (groundTilemap != null && fillSprite != null)
        {
            Tile fill = GetTile(fillSprite, RotTop, true);
            if (fill != null)
            {
                foreach (Vector3Int cell in groundTilemap.cellBounds.allPositionsWithin)
                {
                    if (groundTilemap.GetTile(cell) == null || groundTilemap.GetSprite(cell) != null)
                    {
                        continue;
                    }

                    groundTilemap.SetTile(cell, fill);
                    changed = true;
                }
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(SurfaceNamePrefix))
            {
                continue;
            }

            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                continue;
            }

            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.GetTile(cell) == null || tilemap.GetSprite(cell) != null)
                {
                    continue;
                }

                tilemap.SetTile(cell, null);
                changed = true;
            }
        }

        if (changed)
        {
            Debug.Log(
                "[LevelTile] Tile tanpa sprite diperbaiki: tanah diisi tile standar, " +
                "lapisan permukaan yang rusak dibuang.",
                this
            );
        }

        return changed;
    }

    /// <summary>
    /// Satu tile harus selebar satu sel grid. Tile sisa ukuran lama bisa tersimpan dengan
    /// skala dua kali sel, sehingga gambar dan collider-nya menggelembung satu unit ke
    /// setiap arah (Webi jadi berdiri di atas collider tak terlihat). Skala tiap sel
    /// dikembalikan ke ukuran sel grid tanpa mengubah tile mana yang dilukis.
    /// </summary>
    public bool NormalizePaintedTileScales()
    {
        float cellSize = Mathf.Max(0.01f, tileSize);
        bool changed = NormalizeTilemapTileScales(transform.Find(GroundName), cellSize);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(SurfaceNamePrefix))
            {
                continue;
            }

            changed |= NormalizeTilemapTileScales(child, cellSize);
        }

        if (changed)
        {
            Debug.Log("[LevelTile] Skala tile disamakan dengan satu sel grid (1 tile = 1 sel).", this);
        }

        return changed;
    }

    private bool NormalizeTilemapTileScales(Transform child, float cellSize)
    {
        if (child == null)
        {
            return false;
        }

        Tilemap tilemap = child.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            return false;
        }

        bool changed = false;

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cell) == null)
            {
                continue;
            }

            // Tile slope sudah tepat satu sel (gambar baji dengan pivot tengah), jadi skalanya jangan diubah.
            if (IsSlopeTile(tilemap.GetTile(cell)))
            {
                continue;
            }

            // Ukuran asli sprite menentukan skala yang benar; tanpa sprite, tile dianggap 1:1.
            Sprite sprite = tilemap.GetSprite(cell);
            float expected = 1f;
            if (sprite != null)
            {
                float spriteSize = sprite.rect.width / Mathf.Max(1f, sprite.pixelsPerUnit);
                expected = spriteSize > 0.0001f ? cellSize / spriteSize : 1f;
            }

            Matrix4x4 current = tilemap.GetTransformMatrix(cell);
            float currentScale = new Vector2(current.m00, current.m10).magnitude;
            if (Mathf.Abs(currentScale - expected) < 0.01f)
            {
                continue;
            }

            // Rotasi tile (0/90/180/270 untuk tepi dan sudut permukaan) tetap dipertahankan.
            float angle = Mathf.Atan2(current.m10, current.m00) * Mathf.Rad2Deg;
            tilemap.SetTransformMatrix(
                cell,
                Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angle), Vector3.one * expected)
            );
            changed = true;
        }

        if (changed)
        {
            tilemap.RefreshAllTiles();
        }

        return changed;
    }

    /// <summary>Ringkasan batas tile dan batas collider, untuk memeriksa hasil perbaikan di Console.</summary>
    public string DescribeGroundBounds()
    {
        Transform child = transform.Find(GroundName);
        if (child == null)
        {
            return "Tilemap_Ground tidak ada.";
        }

        Tilemap tilemap = child.GetComponent<Tilemap>();
        CompositeCollider2D composite = child.GetComponent<CompositeCollider2D>();
        string tiles = "tile tidak terbaca";
        if (tilemap != null && TryGetPaintedCellBounds(tilemap, out Bounds painted))
        {
            tiles = string.Format(
                "tile x {0:0.##}..{1:0.##}, y {2:0.##}..{3:0.##}",
                painted.min.x,
                painted.max.x,
                painted.min.y,
                painted.max.y
            );
        }
        string collider = composite != null
            ? string.Format(
                "collider x {0:0.##}..{1:0.##}, y {2:0.##}..{3:0.##}",
                composite.bounds.min.x,
                composite.bounds.max.x,
                composite.bounds.min.y,
                composite.bounds.max.y
            )
            : "collider tidak ada";

        return tiles + "; " + collider + ".";
    }

    /// <summary>Batas collider platform tidak sama dengan batas tile yang dilukis.</summary>
    private bool PlatformColliderMismatch()
    {
        Transform child = transform.Find(GroundName);
        if (child == null)
        {
            return false;
        }

        Tilemap tilemap = child.GetComponent<Tilemap>();
        CompositeCollider2D composite = child.GetComponent<CompositeCollider2D>();
        if (tilemap == null || composite == null)
        {
            return false;
        }

        if (!TryGetPaintedCellBounds(tilemap, out Bounds painted))
        {
            return false;
        }

        // Collider tile slope hanya mengisi bagian baji di dalam sel (segitiga / segi empat),
        // jadi batas collider tidak sama dengan batas sel yang dilukis.
        if (HasSlopeTiles(tilemap))
        {
            return false;
        }

        Bounds colliderBounds = composite.bounds;
        const float tolerance = 0.1f;

        return Mathf.Abs(painted.min.x - colliderBounds.min.x) > tolerance ||
               Mathf.Abs(painted.max.x - colliderBounds.max.x) > tolerance ||
               Mathf.Abs(painted.min.y - colliderBounds.min.y) > tolerance ||
               Mathf.Abs(painted.max.y - colliderBounds.max.y) > tolerance;
    }

    /// <summary>Tile slope (mis. Slope_Ground_45_0 atau Slope_Ground_26_L): gambar baji di dalam satu sel, skalanya jangan dinormalkan.</summary>
    private static bool IsSlopeTile(TileBase tile)
    {
        return tile != null &&
            tile.name.StartsWith(SlopeTileNamePrefix, System.StringComparison.Ordinal);
    }

    private static bool HasSlopeTiles(Tilemap tilemap)
    {
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (IsSlopeTile(tilemap.GetTile(cell)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Batas dunia dari sel-sel yang benar-benar terisi tile, bukan seluruh area tilemap.</summary>
    private static bool TryGetPaintedCellBounds(Tilemap tilemap, out Bounds worldBounds)
    {
        worldBounds = default;

        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);
        bool any = false;

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cell) == null)
            {
                continue;
            }

            any = true;
            min = Vector3Int.Min(min, cell);
            max = Vector3Int.Max(max, cell);
        }

        if (!any)
        {
            return false;
        }

        Vector3 cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;
        Vector3 low = tilemap.transform.TransformPoint(
            new Vector3(min.x * cellSize.x, min.y * cellSize.y, 0f));
        Vector3 high = tilemap.transform.TransformPoint(
            new Vector3((max.x + 1) * cellSize.x, (max.y + 1) * cellSize.y, 0f));

        worldBounds = new Bounds(
            (low + high) * 0.5f,
            new Vector3(Mathf.Abs(high.x - low.x), Mathf.Abs(high.y - low.y), 0f)
        );
        return true;
    }

    /// <summary>
    /// Composite dibangun ulang dari bentuk collider yang sedang tersimpan di tiap tile.
    /// Ini TIDAK memperbarui bentuk per tile kalau sprite tile-nya berubah: cache
    /// TilemapCollider2D hanya terbuang kalau komponennya dibuat ulang
    /// (lihat AGENTS.md, "Jebakan engine: collider tilemap").
    /// </summary>
    private void RebuildColliderGeometry()
    {
        Transform child = transform.Find(GroundName);
        if (child == null)
        {
            return;
        }

        Tilemap tilemap = child.GetComponent<Tilemap>();
        TilemapCollider2D tilemapCollider = child.GetComponent<TilemapCollider2D>();
        CompositeCollider2D composite = child.GetComponent<CompositeCollider2D>();
        if (tilemapCollider == null || composite == null)
        {
            return;
        }

        // Toggle enabled memaksa composite mengambil ulang data tile saat ini. Catatan: ini tidak
        // membuang cache bentuk per tile kalau bentuk sprite-nya yang berubah.
        bool wasEnabled = tilemapCollider.enabled;
        tilemapCollider.enabled = false;
        if (tilemap != null)
        {
            tilemap.RefreshAllTiles();
        }

        tilemapCollider.enabled = wasEnabled;
        tilemapCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();
        Physics2D.SyncTransforms();
    }

    /// <summary>Kosongkan semua tile tanpa menghapus struktur Grid / Tilemap.</summary>
    [ContextMenu("Hapus Semua Tile")]
    public void ClearTiles()
    {
        // Referensi groundTilemap hanya terisi setelah Build() jalan. Kalau menu ini dipakai
        // tanpa build (mis. setelah domain reload atau Build On Enable dimatikan), referensinya
        // kosong, jadi cari ulang lewat nama child supaya tile tanah ikut terhapus.
        Tilemap ground = groundTilemap != null ? groundTilemap : FindGroundTilemap();
        if (ground != null)
        {
            ground.ClearAllTiles();
        }

        ClearSurfaceTilemaps();
        MarkDirty();
    }

    private Tilemap FindGroundTilemap()
    {
        Transform child = transform.Find(GroundName);
        if (child == null)
        {
            return null;
        }

        groundTilemap = child.GetComponent<Tilemap>();
        return groundTilemap;
    }

    #endregion

    #region Defaults

    private static readonly string[] SpriteFolders =
    {
        "Assets/Art/Tiles/Source",
    };

    private static readonly string[] DefaultMaterials = { "Grass", "Oil", "Ice" };

    /// <summary>
    /// Isi material / bentuk level dengan nilai default kalau di Inspector masih kosong.
    /// Nilai yang sudah diisi (lewat Inspector atau tersimpan di scene) tidak disentuh.
    /// </summary>
    private void EnsureDefaults()
    {
        if (surfaces == null || surfaces.Length == 0)
        {
            surfaces = BuildDefaultSurfaces();
        }

        if (blocks == null || blocks.Length == 0)
        {
            blocks = BuildDefaultBlocks();
        }
    }

    private static SurfaceSet[] BuildDefaultSurfaces()
    {
        SurfaceSet[] defaults = new SurfaceSet[DefaultMaterials.Length];
        for (int i = 0; i < DefaultMaterials.Length; i++)
        {
            string material = DefaultMaterials[i];
            string key = material.ToLowerInvariant();
            defaults[i] = new SurfaceSet
            {
                name = material,
                fill = LoadSprite("tile_ground_fill"),
                edge = LoadSprite("tile_" + key + "_edge"),
                cornerOuter = LoadSprite("tile_" + key + "_corner_outer"),
                cornerInner = LoadSprite("tile_" + key + "_corner_inner"),
            };
        }

        return defaults;
    }

    /// <summary>Bentuk level default: tanah + dua anak tangga, satu platform melayang, satu oli, satu es.</summary>
    private static Block[] BuildDefaultBlocks()
    {
        return new[]
        {
            NewBlock(-8, -3, 16, 2, 0),
            NewBlock(-5, -1, 3, 1, 0),
            NewBlock(-3, 0, 2, 1, 0),
            NewBlock(1, 1, 4, 1, 0),
            NewBlock(-7, 2, 2, 1, 1),
            NewBlock(4, -1, 2, 1, 2),
        };
    }

    private static Block NewBlock(int x, int y, int width, int height, int surface)
    {
        return new Block
        {
            origin = new Vector2Int(x, y),
            size = new Vector2Int(width, height),
            surface = surface,
        };
    }

    private static Sprite LoadSprite(string spriteName)
    {
#if UNITY_EDITOR
        foreach (string folder in SpriteFolders)
        {
            string path = folder + "/" + spriteName + ".png";
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
        }
#endif
        return null;
    }

    #endregion

    #region Structure

    /// <summary>Pastikan Grid dan Tilemap dasar sudah ada di bawah objek ini.</summary>
    private void EnsureStructure()
    {
        gameObject.layer = platformLayer;

        Grid grid = GetComponent<Grid>();
        if (grid == null)
        {
            grid = gameObject.AddComponent<Grid>();
        }

        float size = Mathf.Max(0.01f, tileSize);
        grid.cellSize = new Vector3(size, size, 1f);

        groundTilemap = EnsureTilemap(GroundName, fillSortingOrder, true);
        surfaceTilemaps.Clear();
        stackedSurfaceTilemaps.Clear();

        // Lapisan permukaan kedua dibuat supaya fringe atas dan fringe bawah bisa
        // ditumpuk pada sel yang sama.
        EnsureStackedSurfaceLayers();
    }

    private Tilemap EnsureTilemap(string childName, int sortingOrder, bool withCollider)
    {
        Transform child = transform.Find(childName);
        GameObject go;
        if (child == null)
        {
            go = new GameObject(childName);
            go.transform.SetParent(transform, false);
        }
        else
        {
            go = child.gameObject;
        }

        go.layer = platformLayer;

        Tilemap tilemap = go.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            tilemap = go.AddComponent<Tilemap>();
        }

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = go.AddComponent<TilemapRenderer>();
        }

        renderer.sortingOrder = sortingOrder;

        if (withCollider)
        {
            EnsureCollider(go);
        }

        return tilemap;
    }

    /// <summary>Collider platform: TilemapCollider2D yang digabung lewat CompositeCollider2D.</summary>
    private void EnsureCollider(GameObject go)
    {
        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = go.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D tilemapCollider = go.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = go.AddComponent<TilemapCollider2D>();
        }

        CompositeCollider2D composite = go.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = go.AddComponent<CompositeCollider2D>();
        }

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        // Padanan modern dari "usedByComposite = true": Merge = gabung geometri (Boolean OR).
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    private Tilemap EnsureSurfaceTilemap(int surfaceIndex)
    {
        if (surfaceTilemaps.TryGetValue(surfaceIndex, out Tilemap existing) && existing != null)
        {
            return existing;
        }

        SurfaceSet set = GetSurface(surfaceIndex);
        string surfaceName = set != null && !string.IsNullOrEmpty(set.name)
            ? set.name
            : surfaceIndex.ToString();

        Tilemap tilemap = EnsureTilemap(SurfaceNamePrefix + surfaceName, surfaceSortingOrder, false);
        surfaceTilemaps[surfaceIndex] = tilemap;
        return tilemap;
    }

    /// <summary>
    /// Lapisan permukaan kedua dari material yang sama, misalnya
    /// "Tilemap_Surface_Grass_2". Satu sel tilemap hanya bisa menyimpan satu tile,
    /// jadi platform setinggi satu sel memakai lapisan ini untuk fringe bawahnya.
    /// </summary>
    private Tilemap EnsureStackedSurfaceTilemap(int surfaceIndex)
    {
        if (stackedSurfaceTilemaps.TryGetValue(surfaceIndex, out Tilemap existing) && existing != null)
        {
            return existing;
        }

        SurfaceSet set = GetSurface(surfaceIndex);
        string surfaceName = set != null && !string.IsNullOrEmpty(set.name)
            ? set.name
            : surfaceIndex.ToString();

        Tilemap tilemap = EnsureTilemap(
            SurfaceNamePrefix + surfaceName + StackedSurfaceSuffix,
            surfaceSortingOrder,
            false
        );
        stackedSurfaceTilemaps[surfaceIndex] = tilemap;
        return tilemap;
    }

    /// <summary>
    /// Pastikan lapisan permukaan kedua tersedia untuk setiap material yang boleh ditumpuk.
    /// Layer ini dibuat lebih awal supaya langsung muncul di pilihan Active Target pada
    /// Tile Palette, walau belum ada tile yang perlu ditumpuk.
    /// </summary>
    [ContextMenu("Buat Layer Permukaan Stack")]
    public void EnsureStackedSurfaceLayers()
    {
        if (surfaces == null)
        {
            return;
        }

        bool changed = false;

        for (int i = 0; i < surfaces.Length; i++)
        {
            SurfaceSet set = surfaces[i];
            if (set == null || !set.stackable)
            {
                continue;
            }

            string surfaceName = !string.IsNullOrEmpty(set.name) ? set.name : i.ToString();
            if (transform.Find(SurfaceNamePrefix + surfaceName + StackedSurfaceSuffix) == null)
            {
                EnsureStackedSurfaceTilemap(i);
                changed = true;
            }
        }

        if (changed)
        {
            MarkDirty();
        }
    }

    /// <summary>Bangun ulang kalau tilemap masih kosong atau ukuran tile berubah.</summary>
    private bool NeedsRebuild()
    {
        if (!Mathf.Approximately(lastBuiltTileSize, Mathf.Max(0.01f, tileSize)))
        {
            return true;
        }

        return !HasTiles();
    }

    private bool HasTiles()
    {
        Transform child = transform.Find(GroundName);
        if (child == null)
        {
            return false;
        }

        Tilemap existing = child.GetComponent<Tilemap>();
        return existing != null && existing.size.sqrMagnitude > 0;
    }

    #endregion

    #region Painting

    private void CollectShape()
    {
        shape.Clear();
        cellSurface.Clear();

        if (blocks == null)
        {
            return;
        }

        int lastSurface = Mathf.Max(0, (surfaces == null ? 0 : surfaces.Length) - 1);

        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            for (int x = 0; x < block.size.x; x++)
            {
                for (int y = 0; y < block.size.y; y++)
                {
                    Vector3Int cell = new Vector3Int(block.origin.x + x, block.origin.y + y, 0);
                    if (shape.Add(cell))
                    {
                        cellSurface[cell] = Mathf.Clamp(block.surface, 0, lastSurface);
                    }
                }
            }
        }
    }

    private void PaintTiles()
    {
        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();
        }

        ClearSurfaceTilemaps();

        if (surfaces == null || surfaces.Length == 0)
        {
            return;
        }

        foreach (Vector3Int cell in shape)
        {
            int surfaceIndex = cellSurface[cell];
            SurfaceSet set = GetSurface(surfaceIndex);
            if (set == null)
            {
                continue;
            }

            Tile fill = GetTile(set.fill, RotTop, true);
            if (fill != null && groundTilemap != null)
            {
                groundTilemap.SetTile(cell, fill);
            }

            // Lapisan pertama: sisi atas / sisi yang menghadap camera.
            SurfaceChoice primary = PickPrimarySurface(cell, set);
            if (primary.IsValid)
            {
                Tile primaryTile = GetTile(primary.sprite, primary.rotation, false);
                if (primaryTile != null)
                {
                    EnsureSurfaceTilemap(surfaceIndex).SetTile(cell, primaryTile);
                }
            }

            // Lapisan kedua: sisi yang masih terbuka setelah lapisan pertama terisi.
            // Ini yang membuat platform setinggi satu sel bisa punya fringe atas
            // dan fringe bawah sekaligus.
            if (set.stackable)
            {
                SurfaceChoice secondary = PickSecondarySurface(cell, set, primary.covered);
                if (secondary.IsValid)
                {
                    Tile secondaryTile = GetTile(secondary.sprite, secondary.rotation, false);
                    if (secondaryTile != null)
                    {
                        EnsureStackedSurfaceTilemap(surfaceIndex).SetTile(cell, secondaryTile);
                    }
                }
            }
        }

        MarkDirty();
    }

    /// <summary>
    /// Sisi-sisi sel yang tidak tertutup platform lain (tempat fringe permukaan diletakkan).
    /// </summary>
    private OpenSide GetOpenSides(Vector3Int cell)
    {
        int x = cell.x;
        int y = cell.y;

        OpenSide sides = OpenSide.None;
        if (!IsFilled(x, y + 1))
        {
            sides |= OpenSide.Top;
        }

        if (!IsFilled(x + 1, y))
        {
            sides |= OpenSide.Right;
        }

        if (!IsFilled(x, y - 1))
        {
            sides |= OpenSide.Bottom;
        }

        if (!IsFilled(x - 1, y))
        {
            sides |= OpenSide.Left;
        }

        return sides;
    }

    /// <summary>
    /// Tentukan tile permukaan utama untuk satu sel: sudut luar pada tonjolan,
    /// sudut dalam pada cekungan, tepi pada sisi yang terbuka.
    /// </summary>
    private SurfaceChoice PickPrimarySurface(Vector3Int cell, SurfaceSet set)
    {
        int x = cell.x;
        int y = cell.y;

        bool top = !IsFilled(x, y + 1);
        bool bottom = !IsFilled(x, y - 1);
        bool left = !IsFilled(x - 1, y);
        bool right = !IsFilled(x + 1, y);

        // Sudut luar: dua sisi bersebelahan terbuka sekaligus.
        if (top && left)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotTop,
                covered = OpenSide.Top | OpenSide.Left
            };
        }

        if (top && right)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotRight,
                covered = OpenSide.Top | OpenSide.Right
            };
        }

        if (bottom && left)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotLeft,
                covered = OpenSide.Bottom | OpenSide.Left
            };
        }

        if (bottom && right)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotBottom,
                covered = OpenSide.Bottom | OpenSide.Right
            };
        }

        // Sudut dalam: permukaan atas bertemu dinding vertikal tetangga.
        if (top && IsFilled(x - 1, y) && IsFilled(x - 1, y + 1))
        {
            return new SurfaceChoice
            {
                sprite = set.cornerInner,
                rotation = RotTop,
                covered = OpenSide.Top
            };
        }

        if (top && IsFilled(x + 1, y) && IsFilled(x + 1, y + 1))
        {
            return new SurfaceChoice
            {
                sprite = set.cornerInner,
                rotation = RotRight,
                covered = OpenSide.Top
            };
        }

        // Sisi tunggal yang terbuka.
        if (top)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotTop, covered = OpenSide.Top };
        }

        if (left)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotLeft, covered = OpenSide.Left };
        }

        if (right)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotRight, covered = OpenSide.Right };
        }

        if (bottom)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotBottom, covered = OpenSide.Bottom };
        }

        return default;
    }

    /// <summary>
    /// Tentukan tile lapisan kedua: sisi yang masih terbuka setelah lapisan utama
    /// terisi. Sisi bawah didahulukan supaya platform setinggi satu sel mendapat
    /// fringe atas (lapisan pertama) dan fringe bawah (lapisan kedua).
    /// </summary>
    private SurfaceChoice PickSecondarySurface(Vector3Int cell, SurfaceSet set, OpenSide alreadyCovered)
    {
        OpenSide open = GetOpenSides(cell) & ~alreadyCovered;
        if (open == OpenSide.None)
        {
            return default;
        }

        bool top = (open & OpenSide.Top) != 0;
        bool bottom = (open & OpenSide.Bottom) != 0;
        bool left = (open & OpenSide.Left) != 0;
        bool right = (open & OpenSide.Right) != 0;

        if (bottom && left)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotLeft,
                covered = OpenSide.Bottom | OpenSide.Left
            };
        }

        if (bottom && right)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotBottom,
                covered = OpenSide.Bottom | OpenSide.Right
            };
        }

        if (bottom)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotBottom, covered = OpenSide.Bottom };
        }

        if (top && left)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotTop,
                covered = OpenSide.Top | OpenSide.Left
            };
        }

        if (top && right)
        {
            return new SurfaceChoice
            {
                sprite = set.cornerOuter,
                rotation = RotRight,
                covered = OpenSide.Top | OpenSide.Right
            };
        }

        if (top)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotTop, covered = OpenSide.Top };
        }

        if (left)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotLeft, covered = OpenSide.Left };
        }

        if (right)
        {
            return new SurfaceChoice { sprite = set.edge, rotation = RotRight, covered = OpenSide.Right };
        }

        return default;
    }

    private bool IsFilled(int x, int y)
    {
        return shape.Contains(new Vector3Int(x, y, 0));
    }

    private void ClearSurfaceTilemaps()
    {
        surfaceTilemaps.Clear();
        stackedSurfaceTilemaps.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(SurfaceNamePrefix))
            {
                continue;
            }

            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                tilemap.ClearAllTiles();
            }
        }
    }

    #endregion

    #region Tile Cache

    private SurfaceSet GetSurface(int surfaceIndex)
    {
        if (surfaces == null || surfaces.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(surfaceIndex, 0, surfaces.Length - 1);
        return surfaces[index];
    }

    /// <summary>Buat (atau ambil dari cache) Tile dari sebuah sprite dengan rotasi tertentu.</summary>
    private Tile GetTile(Sprite sprite, float rotation, bool withCollider)
    {
        if (sprite == null)
        {
            return null;
        }

        string key = sprite.GetInstanceID() + "|" + rotation + "|" + (withCollider ? 1 : 0);
        if (tileCache.TryGetValue(key, out Tile cached) && cached != null)
        {
            return cached;
        }

        // Tile dibuat saat runtime / edit mode dan disimpan bersama scene oleh Unity,
        // jadi tidak perlu (dan tidak bisa) dijadikan asset terpisah.
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = sprite.name + "_" + (int)rotation;
        tile.sprite = sprite;
        tile.colliderType = withCollider ? Tile.ColliderType.Grid : Tile.ColliderType.None;

        // Scale dihitung dari ukuran sprite sebenarnya, jadi tileSize tetap benar
        // walau Pixels Per Unit sprite diubah (mis. 100 -> 25 untuk tile 4 unit).
        float spriteSize = sprite.rect.width / Mathf.Max(1f, sprite.pixelsPerUnit);
        float scale = spriteSize > 0.0001f ? Mathf.Max(0.01f, tileSize) / spriteSize : 1f;

        tile.transform = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(0f, 0f, rotation),
            Vector3.one * scale);

        tileCache[key] = tile;
        generatedTiles.Add(tile);
        return tile;
    }

    private void ReleaseGeneratedTiles()
    {
        foreach (Tile tile in generatedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(tile);
            }
            else
            {
                DestroyImmediate(tile);
            }
        }

        generatedTiles.Clear();
        tileCache.Clear();
    }

    #endregion

    #region Editor Helpers

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    #endregion
}
