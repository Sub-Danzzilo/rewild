using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memperluas gambar latar level supaya seluruh area yang bisa dijangkau kamera selalu punya latar.
///
/// Caranya:
/// 1. Gambar latar asli TIDAK diubah skala maupun posisinya (framing desain tetap sama).
/// 2. Ke kiri dan ke kanan gambar diulang dengan pencerminan, jadi sambungan antar gambar tidak
///    terlihat. Ini dipakai kalau level memanjang melewati tepi gambar.
/// 3. Di belakang semuanya dipasang gradasi prosedural: langit di atas gambar, tanah/gelap di
///    bawah gambar, dan warna di tengah yang menyambung warna tepi gambar asli. Jadi level yang
///    tinggi (misalnya platform di atas puncak pohon) tetap terlihat wajar.
///
/// Komponen ini dibuat otomatis oleh <see cref="CameraDragController"/> saat runtime, jadi level
/// baru langsung ikut tanpa perlu disiapkan manual di scene.
/// </summary>
[DisallowMultipleComponent]
public class LevelBackgroundExtension : MonoBehaviour
{
    #region Inspector Fields

    [Header("Source")]
    [Tooltip("Gambar latar asli (Background (World)). Skala dan posisinya tidak diubah.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Horizontal Repeat")]
    [Tooltip("Jika aktif, gambar diulang ke kiri dan kanan kalau area level lebih lebar dari gambar.")]
    [SerializeField] private bool repeatHorizontally = true;

    [Tooltip("Jika aktif, gambar yang diulang dicerminkan (flip) supaya sambungannya menyatu.")]
    [SerializeField] private bool mirrorTiles = true;

    [Header("Procedural Sky & Ground")]
    [Tooltip("Jika aktif, area di atas dan di bawah gambar asli diisi gradasi langit dan tanah.")]
    [SerializeField] private bool fillGapsWithGradient = true;

    [Tooltip("Jika aktif, warna sambungan dicocokkan otomatis dengan varian background (lush, pine, bare, " +
             "dry, dusk, night). Matikan kalau ingin memakai warna di bawah apa adanya.")]
    [SerializeField] private bool autoDetectThemeColors = true;

    [Tooltip("Warna langit tepat di tepi atas gambar asli (harus sama dengan warna langit gambar).")]
    [SerializeField] private Color skyNearColor = new Color32(49, 196, 249, 255);

    [Tooltip("Warna langit paling jauh di atas gambar asli.")]
    [SerializeField] private Color skyFarColor = new Color32(33, 150, 216, 255);

    [Tooltip("Warna tepat di tepi bawah gambar asli (harus sama dengan warna tanah gambar).")]
    [SerializeField] private Color groundNearColor = new Color32(16, 119, 110, 255);

    [Tooltip("Warna paling gelap di bawah gambar asli.")]
    [SerializeField] private Color groundFarColor = new Color32(6, 46, 40, 255);

    [Tooltip("Jarak (unit dunia) dari tepi atas gambar sampai warna langit terjauh tercapai. " +
             "Makin besar, makin lambat perubahannya.")]
    [SerializeField] private float skyTransitionDistance = 45f;

    [Tooltip("Jarak (unit dunia) dari tepi bawah gambar sampai warna tanah tergelap tercapai.")]
    [SerializeField] private float groundTransitionDistance = 16f;

    [Tooltip("Jumlah baris piksel tekstur gradasi. Makin tinggi, makin halus perubahannya.")]
    [SerializeField, Range(32, 1024)] private int gradientResolution = 256;

    [Header("Safety Margin")]
    [Tooltip("Tambahan area (unit dunia) di luar jangkauan kamera, supaya tidak ada celah saat " +
             "kamera bergerak cepat atau layar berubah ukuran.")]
    [SerializeField] private float overscan = 4f;

    #endregion

    #region Runtime State

    private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
    private Transform visualsRoot;
    private SpriteRenderer gradientRenderer;
    private Sprite gradientSprite;
    private Texture2D gradientTexture;

    private Rect area;
    private Rect coverage;
    private Camera viewCamera;
    private Vector3 authoredPosition;
    private float viewOrthoSize = 12f;
    private int builtScreenWidth = -1;
    private int builtScreenHeight = -1;
    private float builtAspect = -1f;
    private bool built;

    #endregion

    #region Public Methods

    /// <summary>Menentukan gambar latar yang dipakai sebagai sumber pola dan warna.</summary>
    public void Configure(SpriteRenderer source, bool useMirrorTiles)
    {
        backgroundRenderer = source;
        mirrorTiles = useMirrorTiles;

        if (autoDetectThemeColors)
        {
            ApplyThemeColors(source);
        }

        built = false;
    }

    /// <summary>
    /// Mengatur area level yang harus selalu tertutup latar. Gambar asli tetap di posisinya,
    /// hanya bagian di luar gambar yang ditambah. <paramref name="freezeAnchor"/> adalah posisi
    /// kamera desain awal, dipakai untuk sumbu yang isinya lebih kecil dari layar.
    /// </summary>
    public void SetArea(Rect contentBounds, Camera camera, Vector3 freezeAnchor)
    {
        area = contentBounds;
        viewCamera = camera != null ? camera : GetComponent<Camera>();
        viewOrthoSize = viewCamera != null ? viewCamera.orthographicSize : 12f;
        authoredPosition = freezeAnchor;
        Rebuild();
    }

    /// <summary>Apakah renderer ini bagian dari objek bantuan yang dibuat komponen ini?</summary>
    public bool ContainsRenderer(Renderer renderer)
    {
        return renderer != null && visualsRoot != null && renderer.transform.IsChildOf(visualsRoot);
    }

    #endregion

    #region Theme Colors

    /// <summary>
    /// Menyamakan warna sambungan dengan varian background yang dipakai, supaya gradasi langit/tanah
    /// menyatu dengan gambar aslinya. Varian yang tidak dikenal tetap memakai warna di Inspector.
    /// </summary>
    private void ApplyThemeColors(SpriteRenderer source)
    {
        if (source == null || source.sprite == null || source.sprite.texture == null)
        {
            return;
        }

        Color sky;
        Color ground;
        if (!TryGetThemeColors(source.sprite.texture.name, out sky, out ground))
        {
            return;
        }

        skyNearColor = sky;
        groundNearColor = ground;
        skyFarColor = Color.Lerp(sky, new Color(0.08f, 0.22f, 0.45f), 0.6f);
        groundFarColor = Color.Lerp(ground, Color.black, 0.65f);
    }

    private static bool TryGetThemeColors(string textureName, out Color sky, out Color ground)
    {
        sky = default;
        ground = default;

        if (string.IsNullOrEmpty(textureName))
        {
            return false;
        }

        string name = textureName.ToLowerInvariant();

        if (name.Contains("01_lush"))
        {
            sky = new Color32(53, 199, 248, 255);
            ground = new Color32(16, 124, 110, 255);
        }
        else if (name.Contains("02_pine"))
        {
            sky = new Color32(49, 196, 249, 255);
            ground = new Color32(16, 119, 110, 255);
        }
        else if (name.Contains("03_bare"))
        {
            sky = new Color32(172, 204, 188, 255);
            ground = new Color32(82, 94, 57, 255);
        }
        else if (name.Contains("04_dry"))
        {
            sky = new Color32(232, 145, 126, 255);
            ground = new Color32(129, 70, 55, 255);
        }
        else if (name.Contains("05_dusk"))
        {
            sky = new Color32(165, 119, 159, 255);
            ground = new Color32(16, 85, 104, 255);
        }
        else if (name.Contains("06_night"))
        {
            sky = new Color32(21, 51, 112, 255);
            ground = new Color32(5, 56, 94, 255);
        }
        else
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Unity Events

    private void LateUpdate()
    {
        if (!built || viewCamera == null)
        {
            return;
        }

        // Kamera boleh di-zoom in saat fokus (ukuran ortografis mengecil). Kalau ukurannya justru
        // membesar (misalnya layar berubah), area yang perlu ditutup ikut membesar.
        bool needsRebuild = false;
        if (viewCamera.orthographicSize > viewOrthoSize + 0.001f)
        {
            viewOrthoSize = viewCamera.orthographicSize;
            needsRebuild = true;
        }

        if (Screen.width != builtScreenWidth ||
            Screen.height != builtScreenHeight ||
            !Mathf.Approximately(viewCamera.aspect, builtAspect))
        {
            needsRebuild = true;
        }

        // Kamera bisa keluar dari area yang sudah ditutup latar (misalnya setelah teleport
        // memperluas batas level). Kalau itu terjadi, latar diperluas lagi supaya tidak ada
        // bagian layar yang memperlihatkan warna clear kamera.
        if (!CoverageContains(GetCameraViewRect()))
        {
            needsRebuild = true;
        }

        if (needsRebuild)
        {
            Rebuild();
        }
    }

    private void OnDestroy()
    {
        ReleaseGradient();

        // Wadah gambar bantuan bukan anak kamera, jadi dibersihkan sendiri saat komponen hilang.
        if (visualsRoot != null)
        {
            Destroy(visualsRoot.gameObject);
            visualsRoot = null;
        }

        tiles.Clear();
    }

    #endregion

    #region Build

    private void Rebuild()
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        if (area.width <= 0.001f || area.height <= 0.001f)
        {
            return;
        }

        EnsureVisualsRoot();

        float aspect = viewCamera != null ? Mathf.Max(0.01f, viewCamera.aspect) : 16f / 9f;
        Rect visibleArea = GetVisibleArea(aspect);

        // Tampilan kamera yang sedang berjalan selalu ikut ditutup, termasuk saat kamera berada
        // di luar area level (mis. kamera menyusul Webi setelah teleport).
        visibleArea = Union(visibleArea, Expand(GetCameraViewRect(), Mathf.Max(0f, overscan)));

        Bounds artBounds = backgroundRenderer.bounds;
        BuildTiles(visibleArea, artBounds);
        BuildGradient(visibleArea, artBounds);

        coverage = visibleArea;
        built = true;
        builtScreenWidth = Screen.width;
        builtScreenHeight = Screen.height;
        builtAspect = aspect;
    }

    /// <summary>
    /// Area dunia yang mungkin terlihat kamera saat digeser di dalam area level. Kalau area level
    /// lebih kecil dari layar pada satu sumbu, kamera tidak bergeser di sumbu itu, jadi tampilan
    /// dihitung dari posisi kamera desain awal pada sumbu tersebut.
    /// </summary>
    private Rect GetVisibleArea(float aspect)
    {
        float halfHeight = viewOrthoSize;
        float halfWidth = halfHeight * aspect;
        float margin = Mathf.Max(0f, overscan);

        float xMin;
        float xMax;
        if (area.width >= halfWidth * 2f)
        {
            xMin = area.xMin;
            xMax = area.xMax;
        }
        else
        {
            xMin = authoredPosition.x - halfWidth;
            xMax = authoredPosition.x + halfWidth;
        }

        float yMin;
        float yMax;
        if (area.height >= halfHeight * 2f)
        {
            yMin = area.yMin;
            yMax = area.yMax;
        }
        else
        {
            yMin = authoredPosition.y - halfHeight;
            yMax = authoredPosition.y + halfHeight;
        }

        return new Rect(
            xMin - margin,
            yMin - margin,
            (xMax - xMin) + margin * 2f,
            (yMax - yMin) + margin * 2f
        );
    }

    private void EnsureVisualsRoot()
    {
        if (visualsRoot != null)
        {
            return;
        }

        // Wadah sengaja tidak ikut parent background supaya posisi anak-anaknya bisa memakai
        // koordinat dunia langsung, termasuk kalau background berada di bawah objek lain.
        GameObject rootObject = new GameObject("Background Extension (Auto)");
        visualsRoot = rootObject.transform;
        visualsRoot.position = Vector3.zero;
        visualsRoot.rotation = Quaternion.identity;
        visualsRoot.localScale = Vector3.one;
    }

    /// <summary>Area dunia yang sedang terlihat oleh kamera sekarang.</summary>
    private Rect GetCameraViewRect()
    {
        if (viewCamera == null)
        {
            return new Rect(transform.position.x, transform.position.y, 0f, 0f);
        }

        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;
        Vector3 position = viewCamera.transform.position;
        return new Rect(
            position.x - halfWidth,
            position.y - halfHeight,
            halfWidth * 2f,
            halfHeight * 2f
        );
    }

    private bool CoverageContains(Rect rect)
    {
        if (coverage.width <= 0f || coverage.height <= 0f)
        {
            return false;
        }

        return rect.xMin >= coverage.xMin &&
               rect.xMax <= coverage.xMax &&
               rect.yMin >= coverage.yMin &&
               rect.yMax <= coverage.yMax;
    }

    private static Rect Union(Rect a, Rect b)
    {
        float xMin = Mathf.Min(a.xMin, b.xMin);
        float yMin = Mathf.Min(a.yMin, b.yMin);
        float xMax = Mathf.Max(a.xMax, b.xMax);
        float yMax = Mathf.Max(a.yMax, b.yMax);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static Rect Expand(Rect rect, float margin)
    {
        return new Rect(
            rect.xMin - margin,
            rect.yMin - margin,
            rect.width + margin * 2f,
            rect.height + margin * 2f
        );
    }

    private void BuildTiles(Rect visibleArea, Bounds artBounds)
    {
        foreach (SpriteRenderer tile in tiles)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }
        tiles.Clear();

        if (!repeatHorizontally)
        {
            return;
        }

        float artWidth = artBounds.size.x;
        if (artWidth <= 0.01f)
        {
            return;
        }

        float centerX = artBounds.center.x;
        int leftCount = Mathf.Max(0, Mathf.CeilToInt((centerX - visibleArea.xMin) / artWidth - 0.5f));
        int rightCount = Mathf.Max(0, Mathf.CeilToInt((visibleArea.xMax - centerX) / artWidth - 0.5f));

        for (int n = -leftCount; n <= rightCount; n++)
        {
            if (n == 0)
            {
                continue;
            }

            tiles.Add(CreateTile(n, artWidth));
        }
    }

    private SpriteRenderer CreateTile(int index, float artWidth)
    {
        GameObject tileObject = new GameObject(
            backgroundRenderer.gameObject.name + " (Extension " + index + ")"
        );
        tileObject.transform.SetParent(visualsRoot, false);

        // Salin properti gambar asli satu per satu (bukan Instantiate) supaya komponen lain yang
        // mungkin menempel di background tidak ikut terduplikasi.
        SpriteRenderer tile = tileObject.AddComponent<SpriteRenderer>();
        tile.sprite = backgroundRenderer.sprite;
        tile.sharedMaterial = backgroundRenderer.sharedMaterial;
        tile.sortingLayerID = backgroundRenderer.sortingLayerID;
        tile.sortingOrder = backgroundRenderer.sortingOrder;
        tile.color = backgroundRenderer.color;
        tile.flipX = backgroundRenderer.flipX;
        tile.flipY = backgroundRenderer.flipY;
        tile.drawMode = backgroundRenderer.drawMode;
        tile.size = backgroundRenderer.size;
        tile.tileMode = backgroundRenderer.tileMode;
        tile.adaptiveModeThreshold = backgroundRenderer.adaptiveModeThreshold;
        tile.spriteSortPoint = backgroundRenderer.spriteSortPoint;
        tile.maskInteraction = backgroundRenderer.maskInteraction;

        Vector3 position = backgroundRenderer.transform.position;
        Vector3 axis = backgroundRenderer.transform.right;
        Transform tileTransform = tileObject.transform;
        tileTransform.position = position + axis * (index * artWidth);
        tileTransform.rotation = backgroundRenderer.transform.rotation;
        tileTransform.localScale = backgroundRenderer.transform.lossyScale;

        // Pola pencerminan: tile bersebelahan selalu berlawanan orientasi, jadi tepi gambar
        // yang bertemu selalu sama dan sambungannya tidak terlihat.
        if (mirrorTiles && Mathf.Abs(index) % 2 == 1)
        {
            tile.flipX = !tile.flipX;
        }

        // Tiles dibuat runtime, jadi jangan disimpan atau dianggap objek scene.
        tileObject.hideFlags = HideFlags.DontSave;
        return tile;
    }

    private void BuildGradient(Rect visibleArea, Bounds artBounds)
    {
        if (!fillGapsWithGradient)
        {
            if (gradientRenderer != null)
            {
                gradientRenderer.enabled = false;
            }
            return;
        }

        if (gradientRenderer == null)
        {
            GameObject gradientObject = new GameObject("Background Gradient (Auto)");
            gradientObject.transform.SetParent(visualsRoot, false);
            gradientObject.hideFlags = HideFlags.DontSave;
            gradientRenderer = gradientObject.AddComponent<SpriteRenderer>();
            gradientRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            gradientRenderer.sortingOrder = backgroundRenderer.sortingOrder - 1;
        }

        gradientRenderer.enabled = true;
        gradientRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
        gradientRenderer.sortingOrder = backgroundRenderer.sortingOrder - 1;

        ReleaseGradient();

        int height = Mathf.Max(2, gradientResolution);
        gradientTexture = new Texture2D(1, height, TextureFormat.RGBA32, false, false)
        {
            name = "Background Gradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        Color[] pixelColors = new Color[height];
        for (int i = 0; i < height; i++)
        {
            float worldY = Mathf.Lerp(visibleArea.yMin, visibleArea.yMax, i / (height - 1f));
            pixelColors[i] = EvaluateGradient(worldY, artBounds, visibleArea);
        }
        gradientTexture.SetPixels(pixelColors);
        gradientTexture.Apply(false, false);

        gradientSprite = Sprite.Create(
            gradientTexture,
            new Rect(0f, 0f, 1f, height),
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.FullRect
        );
        gradientSprite.name = "Background Gradient Sprite";
        gradientSprite.hideFlags = HideFlags.DontSave;

        Transform gradientTransform = gradientRenderer.transform;
        gradientTransform.position = new Vector3(
            visibleArea.center.x,
            visibleArea.center.y,
            artBounds.center.z
        );
        gradientTransform.rotation = Quaternion.identity;
        gradientTransform.localScale = new Vector3(
            visibleArea.width,
            visibleArea.height / height,
            1f
        );

        gradientRenderer.sprite = gradientSprite;
    }

    /// <summary>
    /// Warna latar untuk satu posisi Y dunia. Di luar tepi gambar dipakai gradasi langit/tanah;
    /// di antara tepi atas dan bawah dipakai campuran keduanya (bagian ini tertutup gambar asli).
    /// </summary>
    private Color EvaluateGradient(float worldY, Bounds artBounds, Rect areaRect)
    {
        float artTop = artBounds.max.y;
        float artBottom = artBounds.min.y;

        if (worldY >= artTop)
        {
            // Kalau area kosong di atas gambar lebih tinggi dari jarak transisi, perubahan warna
            // direntangkan sampai ujung area supaya tidak berhenti di warna datar.
            float distance = Mathf.Max(skyTransitionDistance, areaRect.yMax - artTop);
            float t = distance > 0.01f
                ? Mathf.Clamp01((worldY - artTop) / distance)
                : 1f;
            return Color.Lerp(skyNearColor, skyFarColor, t);
        }

        if (worldY <= artBottom)
        {
            float distance = Mathf.Max(groundTransitionDistance, artBottom - areaRect.yMin);
            float t = distance > 0.01f
                ? Mathf.Clamp01((artBottom - worldY) / distance)
                : 1f;
            return Color.Lerp(groundNearColor, groundFarColor, t);
        }

        float middleT = Mathf.InverseLerp(artBottom, artTop, worldY);
        return Color.Lerp(groundNearColor, skyNearColor, middleT);
    }

    private void ReleaseGradient()
    {
        if (gradientRenderer != null)
        {
            gradientRenderer.sprite = null;
        }

        if (gradientSprite != null)
        {
            Destroy(gradientSprite);
            gradientSprite = null;
        }

        if (gradientTexture != null)
        {
            Destroy(gradientTexture);
            gradientTexture = null;
        }
    }

    #endregion
}
