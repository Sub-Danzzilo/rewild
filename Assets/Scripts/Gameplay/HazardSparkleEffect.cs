using UnityEngine;

/// <summary>
/// Menandai duri sebagai bahaya: kilau bintang putih kecil muncul acak di badan hazard
/// lalu memudar. Efek ini murni visual (tanpa collider), jadi pemain langsung paham
/// "ini tajam, jangan disentuh".
///
/// Dipakai oleh semua prefab spike (Spike_1, Spike_2, dst). Bentuk bintang dibuat dari
/// kode supaya tidak perlu menambah file gambar baru ke project. Ukuran kilau memakai
/// satuan dunia, jadi tetap terlihat sama besar walau prefab dipasang dengan skala
/// berbeda-beda di tiap level.
/// </summary>
[DisallowMultipleComponent]
public class HazardSparkleEffect : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Collider area bahaya. Kalau kosong, diambil dari Collider2D di objek ini. " +
             "Kilau hanya dipilih di dalam bentuk collider ini.")]
    public Collider2D dangerArea;

    [Tooltip("Renderer hazard. Dipakai untuk menyamakan sorting layer/order supaya kilau tampil di atas duri.")]
    public SpriteRenderer hazardRenderer;

    [Header("Sparkle Visuals")]
    [Tooltip("Jumlah kilau yang tampil bergantian.")]
    [Min(1)] public int sparkleCount = 8;

    [Tooltip("Ukuran satu kilau dalam satuan dunia. Dipakai apa adanya, tidak ikut skala prefab induk.")]
    [Min(0.01f)] public float sparkleWorldSize = 0.44f;

    [Tooltip("Rentang pengali ukuran tiap kilau (x = paling kecil, y = paling besar). " +
             "Contoh 0.75..1.15 membuat kilau tidak semuanya sama besar.")]
    public Vector2 sizeRange = new Vector2(0.75f, 1.15f);

    [Tooltip("Skala kilau dari awal sampai puncak kilau (x = saat muncul, y = saat paling terang).")]
    public Vector2 scaleOverLife = new Vector2(0.7f, 1.3f);

    [Tooltip("Warna kilau.")]
    public Color sparkleColor = Color.white;

    [Tooltip("Opsional. Sprite bintang sendiri. Kalau kosong, bintang dibuat otomatis dari kode.")]
    public Sprite customStarSprite;

    [Tooltip("Opsional. Material khusus untuk kilau. Kalau kosong, dipakai material unlit otomatis " +
             "supaya kilau tetap terang walau scene memakai 2D light redup.")]
    public Material customMaterial;

    [Tooltip("Selisih sorting order dari sprite hazard. 12 = di atas semua tilemap (0/1) tapi masih di bawah Webi (20).")]
    public int sortingOrderOffset = 12;

    [Header("Timing")]
    [Tooltip("Lama satu kilau muncul lalu memudar (detik).")]
    [Min(0.05f)] public float flashDuration = 0.75f;

    [Tooltip("Jeda acak sebelum kilau yang sama muncul lagi (detik). x = paling cepat, y = paling lambat.")]
    public Vector2 waitRange = new Vector2(0.25f, 0.95f);

    [Tooltip("Jeda acak saat efek pertama kali muncul (detik). Supaya kilau tidak berkedip serentak.")]
    public Vector2 startDelayRange = new Vector2(0f, 0.75f);

    [Tooltip("Batas jumlah kilau yang boleh tampil bersamaan. 0 = tanpa batas.")]
    [Min(0)] public int maxSimultaneousSparkles = 0;

    [Tooltip("Jika aktif, kilau tetap berkedip saat game di-pause (Time.timeScale = 0).")]
    public bool useUnscaledTime = false;

    [Header("Motion")]
    [Tooltip("Jarak naik kilau selama berkedip, dalam satuan dunia. 0 = kilau diam di tempat.")]
    [Min(0f)] public float riseDistance = 0.15f;

    [Tooltip("Rentang sudut awal kilau (derajat). Bintang jadi tidak selalu tegak.")]
    public Vector2 rotationRange = new Vector2(0f, 90f);

    [Tooltip("Kecepatan putar kilau (derajat per detik). 0 = tidak berputar.")]
    public float spinSpeed = 0f;

    [Tooltip("Berapa kali dicoba mencari titik acak yang benar-benar berada di dalam collider bahaya.")]
    [Min(1)] public int placementAttempts = 12;

    [Header("Placement")]
    [Tooltip("Bagian atas area bahaya tempat kilau dipilih. 1 = hanya di tepi paling atas.")]
    [Range(0.05f, 1f)] public float topAreaFraction = 0.7f;

    #endregion

    #region Runtime State

    private struct Sparkle
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 origin;
        public float timer;
        public bool visible;
        public float size;
        public float angle;
    }

    private static Sprite starSprite;
    private static Material generatedSparkleMaterial;

    private Sparkle[] sparkles;
    private float sparkleSpriteWorldSize = 1f;
    private bool ready;

    #endregion

    #region Unity Events

    private void Awake()
    {
        if (dangerArea == null)
        {
            dangerArea = GetComponent<Collider2D>();
        }

        if (hazardRenderer == null)
        {
            hazardRenderer = GetComponent<SpriteRenderer>();
        }

        if (dangerArea == null)
        {
            enabled = false;
            return;
        }

        BuildSparkles();
    }

    private void OnEnable()
    {
        if (!ready && dangerArea != null)
        {
            BuildSparkles();
        }
    }

    private void Update()
    {
        if (!ready || sparkles == null || dangerArea == null)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float flash = Mathf.Max(0.05f, flashDuration);
        float minScale = Mathf.Min(scaleOverLife.x, scaleOverLife.y);
        float maxScale = Mathf.Max(scaleOverLife.x, scaleOverLife.y);

        for (int i = 0; i < sparkles.Length; i++)
        {
            Sparkle sparkle = sparkles[i];
            sparkle.timer -= deltaTime;

            if (sparkle.timer <= 0f)
            {
                if (sparkle.visible)
                {
                    sparkle.visible = false;
                    sparkle.renderer.enabled = false;
                    sparkle.timer = RandomWait();
                }
                else if (maxSimultaneousSparkles <= 0 || CountVisibleSparkles() < maxSimultaneousSparkles)
                {
                    sparkle.visible = true;
                    sparkle.renderer.enabled = true;
                    sparkle.timer = flash;
                    sparkle.size = RandomSize();
                    sparkle.angle = RandomAngle();
                    PlaceSparkle(ref sparkle);
                }
                else
                {
                    sparkle.timer = RandomWait();
                }
            }

            if (sparkle.visible)
            {
                float progress = Mathf.Clamp01(1f - sparkle.timer / flash);
                float pulse = Mathf.Sin(progress * Mathf.PI);

                // Kilau memudar di ujung umurnya, dengan inti yang tetap terang di tengah.
                Color color = sparkleColor;
                color.a *= Mathf.Clamp01(Mathf.Sqrt(pulse) * 1.3f);
                sparkle.renderer.color = color;

                // Skala memakai satuan dunia supaya besar kilau sama di semua level.
                float pulseScale = Mathf.Lerp(minScale, maxScale, pulse);
                float worldScale = sparkleWorldSize * sparkle.size * pulseScale;
                float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
                float localScale = worldScale / (parentScale * sparkleSpriteWorldSize);
                sparkle.transform.localScale = new Vector3(localScale, localScale, 1f);

                sparkle.angle += spinSpeed * deltaTime;
                sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, sparkle.angle);

                // Naik sedikit saat berkilau, supaya terasa seperti percikan, bukan titik diam.
                float rise = Mathf.Max(0f, riseDistance) * progress / parentScale;
                sparkle.transform.localPosition = sparkle.origin + Vector3.up * rise;
            }

            sparkles[i] = sparkle;
        }
    }

    private void OnDestroy()
    {
        ready = false;
        sparkles = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Membuat ulang semua objek kilau dengan pengaturan yang sedang dipakai. Berguna untuk
    /// menerapkan perubahan jumlah/ukuran/warna saat men-tuning langsung di Play Mode.
    /// </summary>
    [ContextMenu("Buat Ulang Kilau")]
    public void RebuildSparkles()
    {
        if (!Application.isPlaying)
        {
            Debug.Log(
                "[HazardSparkle] Pengaturan baru otomatis dipakai saat masuk Play Mode.",
                this
            );
            return;
        }

        if (dangerArea == null)
        {
            dangerArea = GetComponent<Collider2D>();
        }

        if (hazardRenderer == null)
        {
            hazardRenderer = GetComponent<SpriteRenderer>();
        }

        if (dangerArea == null)
        {
            return;
        }

        BuildSparkles();
    }

    #endregion

    #region Internal Helpers

    private float RandomWait()
    {
        float min = Mathf.Max(0f, Mathf.Min(waitRange.x, waitRange.y));
        float max = Mathf.Max(min + 0.05f, Mathf.Max(waitRange.x, waitRange.y));
        return Random.Range(min, max);
    }

    private float RandomStartDelay()
    {
        float min = Mathf.Max(0f, Mathf.Min(startDelayRange.x, startDelayRange.y));
        float max = Mathf.Max(min + 0.05f, Mathf.Max(startDelayRange.x, startDelayRange.y));
        return Random.Range(min, max);
    }

    private float RandomSize()
    {
        float min = Mathf.Max(0.01f, Mathf.Min(sizeRange.x, sizeRange.y));
        float max = Mathf.Max(min, Mathf.Max(sizeRange.x, sizeRange.y));
        return Random.Range(min, max);
    }

    private float RandomAngle()
    {
        float min = Mathf.Min(rotationRange.x, rotationRange.y);
        float max = Mathf.Max(rotationRange.x, rotationRange.y);
        return Random.Range(min, max);
    }

    private int CountVisibleSparkles()
    {
        int count = 0;
        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkles[i].visible)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Buat (atau buat ulang) semua objek kilau sebagai anak hazard.</summary>
    private void BuildSparkles()
    {
        ClearSparkles();

        Sprite sprite = customStarSprite != null ? customStarSprite : GetStarSprite();
        Material material = customMaterial != null ? customMaterial : GetSparkleMaterial();
        sparkleSpriteWorldSize = sprite != null
            ? Mathf.Max(0.0001f, sprite.bounds.size.x)
            : 1f;
        int order = hazardRenderer != null
            ? hazardRenderer.sortingOrder + Mathf.Max(1, sortingOrderOffset)
            : Mathf.Max(1, sortingOrderOffset);
        int layerId = hazardRenderer != null ? hazardRenderer.sortingLayerID : 0;

        sparkles = new Sparkle[Mathf.Max(1, sparkleCount)];

        for (int i = 0; i < sparkles.Length; i++)
        {
            GameObject go = new GameObject("Sparkle_" + i);
            go.transform.SetParent(transform, false);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = sparkleColor;
            renderer.enabled = false;
            renderer.sortingLayerID = layerId;
            renderer.sortingOrder = order;

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            sparkles[i] = new Sparkle
            {
                transform = go.transform,
                renderer = renderer,
                origin = Vector3.zero,
                timer = RandomStartDelay(),
                visible = false,
                size = 1f,
                angle = 0f,
            };
        }

        ready = true;
    }

    private void ClearSparkles()
    {
        if (sparkles == null)
        {
            return;
        }

        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkles[i].renderer != null)
            {
                Destroy(sparkles[i].renderer.gameObject);
            }
        }

        sparkles = null;
        ready = false;
    }

    /// <summary>
    /// Pilih titik acak di bagian atas area bahaya, lalu taruh satu kilau di titik itu.
    /// OverlapPoint dipakai supaya kilau menempel di badan hazard, bukan di ruang kosong
    /// di dalam kotak batas collider.
    /// </summary>
    private void PlaceSparkle(ref Sparkle sparkle)
    {
        Bounds bounds = dangerArea.bounds;
        float lowestY = bounds.min.y + bounds.size.y * (1f - Mathf.Clamp01(topAreaFraction));

        Vector3 worldPoint = bounds.center;

        for (int attempt = 0; attempt < Mathf.Max(1, placementAttempts); attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(lowestY, bounds.max.y)
            );

            if (!dangerArea.OverlapPoint(candidate))
            {
                continue;
            }

            worldPoint = candidate;
            break;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        localPoint.z = 0f;

        sparkle.origin = localPoint;
        sparkle.transform.localPosition = localPoint;
    }

    /// <summary>Material unlit supaya kilau tetap terang walau scene memakai 2D light redup.</summary>
    private static Material GetSparkleMaterial()
    {
        if (generatedSparkleMaterial != null)
        {
            return generatedSparkleMaterial;
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

        generatedSparkleMaterial = new Material(shader);
        generatedSparkleMaterial.name = "HazardSparkle";
        return generatedSparkleMaterial;
    }

    /// <summary>
    /// Bintang putih berujung empat dengan inti padat dan halo tipis. Inti padat membuatnya
    /// tetap terbaca di atas background terang, halo membuatnya terasa berkilau.
    /// </summary>
    private static Sprite GetStarSprite()
    {
        if (starSprite != null)
        {
            return starSprite;
        }

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "HazardSparkleStar";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = Mathf.Abs((x - center) / radius);
                float v = Mathf.Abs((y - center) / radius);

                // Kurva astroid: makin dekat ke sumbu, makin panjang "kaki" bintangnya.
                float shape = Mathf.Pow(u, 0.55f) + Mathf.Pow(v, 0.55f);
                float core = Mathf.Clamp01((1f - shape) * 8f);

                // Halo lembut di sekitar inti.
                float distance = Mathf.Sqrt(u * u + v * v);
                float halo = Mathf.Clamp01(1f - distance * 2f) * 0.4f;

                float alpha = Mathf.Clamp01(core + halo);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        starSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        starSprite.name = "HazardSparkleStar";

        return starSprite;
    }

    #endregion
}
