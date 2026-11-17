using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Partikel kecil yang menempel di permukaan tilemap overlay: es memakai bentuk salju
/// berwarna putih, oil memakai bulatan hitam. Efek ini murni visual (tanpa collider).
///
/// Komponen dipasang otomatis saat scene dimuat ke tilemap bernama "Tilemap_Surface_Ice"
/// dan "Tilemap_Surface_Oil", jadi tiap level ikut tanpa perlu disetel di scene. Bentuk
/// partikelnya dibuat dari kode, jadi tidak ada file gambar baru di project.
/// </summary>
[DisallowMultipleComponent]
public class TilemapSurfaceMotes : MonoBehaviour
{
    #region Constants

    private const string IceTilemapName = "Tilemap_Surface_Ice";
    private const string OilTilemapName = "Tilemap_Surface_Oil";
    private const int SpriteSize = 96;

    #endregion

    #region Nested Types

    /// <summary>Bentuk partikel yang digambar dari kode.</summary>
    public enum MoteShape
    {
        Snowflake,
        Round
    }

    private struct Mote
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 origin;
        public Vector3 up;
        public Vector3 right;
        public float timer;
        public float life;
        public float size;
        public float angle;
        public float drift;
    }

    #endregion

    #region Inspector Fields

    [Header("References")]
    [Tooltip("Tilemap overlay yang partikelnya ditampilkan. Kosong = tilemap pada objek ini.")]
    public Tilemap targetTilemap;

    [Tooltip("Material opsional untuk partikel. Kosong = material unlit otomatis supaya tetap terang.")]
    public Material customMaterial;

    [Header("Visuals")]
    [Tooltip("Bentuk partikel: salju (bintang enam) atau bulat.")]
    public MoteShape shape = MoteShape.Snowflake;

    [Tooltip("Warna partikel.")]
    public Color moteColor = Color.white;

    [Tooltip("Jumlah partikel per sel tilemap yang berisi tile.")]
    [Min(0f)] public float motesPerCell = 1.5f;

    [Tooltip("Batas jumlah partikel untuk satu tilemap.")]
    [Min(1)] public int maxMotes = 24;

    [Tooltip("Ukuran satu partikel dalam unit dunia. Tidak ikut skala induk, jadi tetap sama di tiap level.")]
    [Min(0.01f)] public float worldSize = 0.5f;

    [Tooltip("Rentang pengali ukuran tiap partikel (x = paling kecil, y = paling besar).")]
    public Vector2 sizeRange = new Vector2(0.7f, 1.2f);

    [Tooltip("Selisih sorting order dari tilemap supaya partikel tampil di atas permukaan.")]
    public int sortingOrderOffset = 5;

    [Header("Motion")]
    [Tooltip("Lama satu siklus partikel muncul lalu memudar (detik).")]
    [Min(0.1f)] public float lifeDuration = 2.4f;

    [Tooltip("Jarak naik partikel selama satu siklus, dalam unit dunia.")]
    public float riseDistance = 0.4f;

    [Tooltip("Jarak geser samping acak partikel selama satu siklus, dalam unit dunia.")]
    public float driftDistance = 0.22f;

    [Tooltip("Kecepatan putar partikel (derajat per detik). 0 = tidak berputar.")]
    public float spinSpeed = 45f;

    [Tooltip("Jeda acak sebelum siklus pertama tiap partikel (detik), supaya tidak muncul serentak.")]
    public Vector2 startDelayRange = new Vector2(0f, 1.8f);

    [Tooltip("Jika aktif, partikel tetap hidup saat game di-pause (Time.timeScale = 0).")]
    public bool useUnscaledTime = false;

    #endregion

    #region Runtime State

    private static readonly Dictionary<MoteShape, Sprite> shapeSprites = new Dictionary<MoteShape, Sprite>();
    private static Material generatedMaterial;

    private readonly List<Vector3Int> cells = new List<Vector3Int>();
    private Mote[] motes;
    private float spriteWorldSize = 1f;
    private bool needsBuild;

    #endregion

    #region Play Mode Bootstrapping

    /// <summary>
    /// Memasang efek ke tilemap overlay es dan oil di setiap scene level. Dengan cara ini
    /// level lama maupun level baru langsung mendapat partikelnya tanpa diatur manual.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        AttachToScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToScene(scene);
    }

    private static void AttachToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || tilemap.gameObject.scene != scene)
            {
                continue;
            }

            bool isIce = tilemap.name == IceTilemapName;
            bool isOil = tilemap.name == OilTilemapName;
            if (!isIce && !isOil)
            {
                continue;
            }

            if (tilemap.GetComponent<TilemapSurfaceMotes>() != null)
            {
                continue;
            }

            TilemapSurfaceMotes effect = tilemap.gameObject.AddComponent<TilemapSurfaceMotes>();
            effect.targetTilemap = tilemap;
            effect.ApplyPreset(isIce ? MoteShape.Snowflake : MoteShape.Round);
        }
    }

    #endregion

    #region Unity Events

    private void Awake()
    {
        if (targetTilemap == null)
        {
            targetTilemap = GetComponent<Tilemap>();
        }

        // Dibangun di Update pertama supaya preset dari pemasangan otomatis sempat masuk dulu.
        needsBuild = true;
    }

    private void Update()
    {
        if (needsBuild)
        {
            needsBuild = false;
            BuildMotes();
        }

        if (motes == null || motes.Length == 0)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));

        for (int i = 0; i < motes.Length; i++)
        {
            Mote mote = motes[i];
            mote.timer += deltaTime;

            if (mote.timer >= mote.life)
            {
                mote.timer = 0f;
                mote.life = RandomLife();
                mote.size = RandomSize();
                mote.angle = Random.Range(0f, 360f);
                PlaceMote(ref mote);
            }

            float progress = Mathf.Clamp01(mote.timer / Mathf.Max(0.01f, mote.life));

            // Muncul lalu memudar halus di kedua ujung siklus.
            float fade = Mathf.Sin(progress * Mathf.PI);
            Color color = moteColor;
            color.a *= fade;
            mote.renderer.color = color;

            mote.transform.localPosition = mote.origin +
                mote.up * (riseDistance * progress) +
                mote.right * (mote.drift * progress);

            float worldScale = worldSize * mote.size * Mathf.Lerp(0.85f, 1f, fade);
            float localScale = worldScale / (parentScale * spriteWorldSize);
            mote.transform.localScale = new Vector3(localScale, localScale, 1f);

            mote.angle += spinSpeed * deltaTime;
            mote.transform.localRotation = Quaternion.Euler(0f, 0f, mote.angle);

            motes[i] = mote;
        }
    }

    private void OnDestroy()
    {
        motes = null;
        cells.Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>Menyetel bentuk dan rasa partikel untuk satu jenis permukaan.</summary>
    public void ApplyPreset(MoteShape presetShape)
    {
        shape = presetShape;

        if (presetShape == MoteShape.Snowflake)
        {
            moteColor = new Color(0.96f, 0.99f, 1f, 1f);
            worldSize = 0.52f;
            lifeDuration = 2.4f;
            riseDistance = 0.4f;
            driftDistance = 0.22f;
            spinSpeed = 45f;
            motesPerCell = 1.5f;
        }
        else
        {
            moteColor = new Color(0.07f, 0.06f, 0.08f, 0.92f);
            worldSize = 0.46f;
            lifeDuration = 2f;
            riseDistance = 0.3f;
            driftDistance = 0.12f;
            spinSpeed = 0f;
            motesPerCell = 1.2f;
        }

        needsBuild = true;
    }

    /// <summary>Membangun ulang partikel dengan pengaturan yang sedang dipakai.</summary>
    [ContextMenu("Buat Ulang Partikel")]
    public void BuildMotes()
    {
        needsBuild = false;
        ClearMotes();

        if (targetTilemap == null)
        {
            targetTilemap = GetComponent<Tilemap>();
        }

        if (targetTilemap == null || !Application.isPlaying)
        {
            return;
        }

        CollectCells();
        if (cells.Count == 0)
        {
            return;
        }

        Sprite sprite = GetShapeSprite(shape);
        spriteWorldSize = sprite != null ? Mathf.Max(0.0001f, sprite.bounds.size.x) : 1f;

        TilemapRenderer tilemapRenderer = targetTilemap.GetComponent<TilemapRenderer>();
        int sortingOrder = (tilemapRenderer != null ? tilemapRenderer.sortingOrder : 0) +
            Mathf.Max(1, sortingOrderOffset);
        int sortingLayerId = tilemapRenderer != null ? tilemapRenderer.sortingLayerID : 0;
        Material material = customMaterial != null ? customMaterial : GetMoteMaterial();

        int count = Mathf.Clamp(Mathf.RoundToInt(cells.Count * motesPerCell), 1, Mathf.Max(1, maxMotes));
        motes = new Mote[count];

        for (int i = 0; i < motes.Length; i++)
        {
            GameObject go = new GameObject("Mote_" + i);
            go.transform.SetParent(transform, false);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = moteColor;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            Mote mote = new Mote
            {
                transform = go.transform,
                renderer = renderer,
                life = RandomLife(),
                size = RandomSize(),
                angle = Random.Range(0f, 360f),
            };

            PlaceMote(ref mote);

            // Partikel pertama ikut jadwal acak supaya awalnya sudah terlihat tersebar.
            mote.timer = Random.Range(0f, mote.life);
            float progress = Mathf.Clamp01(mote.timer / Mathf.Max(0.01f, mote.life));
            Color color = moteColor;
            color.a *= Mathf.Sin(progress * Mathf.PI);
            renderer.color = color;

            motes[i] = mote;
        }
    }

    #endregion

    #region Internal Helpers

    private void CollectCells()
    {
        cells.Clear();

        BoundsInt bounds = targetTilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (targetTilemap.HasTile(cell))
            {
                cells.Add(cell);
            }
        }
    }

    /// <summary>Letakkan satu partikel di titik acak dalam sel tilemap yang berisi tile.</summary>
    private void PlaceMote(ref Mote mote)
    {
        if (cells.Count == 0 || targetTilemap == null)
        {
            mote.origin = Vector3.zero;
            mote.up = Vector3.up;
            mote.right = Vector3.right;
            mote.drift = 0f;
            return;
        }

        Vector3Int cell = cells[Random.Range(0, cells.Count)];
        Vector3 center = transform.InverseTransformPoint(targetTilemap.GetCellCenterWorld(cell));
        Vector3 cellSize = targetTilemap.cellSize;
        float halfX = Mathf.Max(0.01f, cellSize.x * 0.42f);
        float halfY = Mathf.Max(0.01f, cellSize.y * 0.42f);

        Vector3 localOffset = new Vector3(
            Random.Range(-halfX, halfX),
            Random.Range(-halfY, halfY),
            0f
        );

        // Tile yang dilukis dengan flip X/Y menyimpan matriks transform per sel. Partikel ikut
        // matriks itu supaya arah naik dan geserannya sejajar dengan permukaan yang digambar,
        // bukan selalu mengikuti sumbu dunia (dulu terlihat aneh di tile yang di-flip).
        Matrix4x4 cellMatrix = targetTilemap.GetTransformMatrix(cell);
        Vector3 up = cellMatrix.MultiplyVector(Vector3.up);
        Vector3 right = cellMatrix.MultiplyVector(Vector3.right);
        Vector3 offset = cellMatrix.MultiplyVector(localOffset);

        if (up.sqrMagnitude < 0.0001f)
        {
            up = Vector3.up;
        }

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        if (offset.sqrMagnitude < 0.0000001f && localOffset.sqrMagnitude > 0f)
        {
            offset = localOffset;
        }

        mote.origin = center + offset;
        mote.up = up.normalized;
        mote.right = right.normalized;
        mote.drift = Random.Range(-1f, 1f) * Mathf.Max(0f, driftDistance);
    }

    private float RandomLife()
    {
        float baseLife = Mathf.Max(0.1f, lifeDuration);
        return Random.Range(baseLife * 0.75f, baseLife * 1.25f);
    }

    private float RandomSize()
    {
        float min = Mathf.Max(0.01f, Mathf.Min(sizeRange.x, sizeRange.y));
        float max = Mathf.Max(min, Mathf.Max(sizeRange.x, sizeRange.y));
        return Random.Range(min, max);
    }

    private void ClearMotes()
    {
        if (motes != null)
        {
            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i].renderer != null)
                {
                    Destroy(motes[i].renderer.gameObject);
                }
            }
        }

        motes = null;
    }

    /// <summary>Material unlit supaya partikel tetap terbaca di atas permukaan yang gelap.</summary>
    private static Material GetMoteMaterial()
    {
        if (generatedMaterial != null)
        {
            return generatedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        generatedMaterial = new Material(shader);
        generatedMaterial.name = "TilemapSurfaceMote";
        return generatedMaterial;
    }

    private static Sprite GetShapeSprite(MoteShape moteShape)
    {
        if (shapeSprites.TryGetValue(moteShape, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite created = moteShape == MoteShape.Snowflake
            ? CreateSnowflakeSprite()
            : CreateRoundSprite();
        shapeSprites[moteShape] = created;
        return created;
    }

    /// <summary>Bintang salju enam lengan dengan inti padat, digambar per piksel.</summary>
    private static Sprite CreateSnowflakeSprite()
    {
        Texture2D texture = NewTexture("TilemapSurfaceMoteSnowflake");
        Color[] pixels = new Color[SpriteSize * SpriteSize];
        float center = (SpriteSize - 1) * 0.5f;
        float radius = SpriteSize * 0.5f;

        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float u = (x - center) / radius;
                float v = (y - center) / radius;
                float distance = Mathf.Sqrt(u * u + v * v);
                float angle = Mathf.Atan2(v, u);

                // Tiga garis yang saling menyilang 60 derajat membentuk enam lengan.
                float arm = 0f;
                for (int i = 0; i < 3; i++)
                {
                    float lineAngle = angle - i * Mathf.PI / 3f;
                    float lineDistance = Mathf.Abs(Mathf.Sin(lineAngle)) * distance;
                    float thickness = Mathf.Lerp(0.05f, 0.015f, Mathf.Clamp01(distance));
                    arm = Mathf.Max(arm, Mathf.Clamp01(1f - lineDistance / Mathf.Max(0.001f, thickness)));
                    // Cabang kecil di ujung lengan supaya bentuknya terbaca sebagai salju.
                    float branchDistance = Mathf.Abs(distance - 0.55f) + Mathf.Abs(lineDistance) * 0.6f;
                    arm = Mathf.Max(arm, Mathf.Clamp01(1f - branchDistance / 0.08f) * 0.85f);
                }

                float core = Mathf.Clamp01((0.3f - distance) / 0.12f);
                float alpha = Mathf.Clamp01(arm + core) * Mathf.Clamp01((1f - distance) * 6f);
                pixels[y * SpriteSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        return FinishSprite(texture, pixels, "TilemapSurfaceMoteSnowflake");
    }

    /// <summary>Bulatan dengan tepi lembut, dipakai untuk partikel oil.</summary>
    private static Sprite CreateRoundSprite()
    {
        Texture2D texture = NewTexture("TilemapSurfaceMoteRound");
        Color[] pixels = new Color[SpriteSize * SpriteSize];
        float center = (SpriteSize - 1) * 0.5f;
        float radius = SpriteSize * 0.5f;

        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float u = (x - center) / radius;
                float v = (y - center) / radius;
                float distance = Mathf.Sqrt(u * u + v * v);
                float alpha = Mathf.Clamp01((0.86f - distance) * 9f);

                // Sedikit kilau di kiri atas supaya bulatannya tidak terlihat seperti titik datar.
                float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(-0.34f, 0.34f)) / 0.26f);
                Color color = Color.Lerp(new Color(1f, 1f, 1f, alpha), new Color(1f, 1f, 1f, alpha * 0.45f), highlight);
                pixels[y * SpriteSize + x] = color;
            }
        }

        return FinishSprite(texture, pixels, "TilemapSurfaceMoteRound");
    }

    private static Texture2D NewTexture(string textureName)
    {
        Texture2D texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        return texture;
    }

    private static Sprite FinishSprite(Texture2D texture, Color[] pixels, string spriteName)
    {
        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, SpriteSize, SpriteSize),
            new Vector2(0.5f, 0.5f),
            SpriteSize
        );
        sprite.name = spriteName;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    #endregion
}
