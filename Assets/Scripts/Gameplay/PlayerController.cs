using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]
    [Tooltip("Kecepatan berjalan Webi di permukaan platform (unit per detik).")]
    public float walkSpeed = 5f;

    [Tooltip("Kecepatan Webi tertarik ke titik tembak web saat mengayun.")]
    public float swingSpeed = 15f;

    [Tooltip("Kecepatan ujung web melesat menuju target saat menembak.")]
    public float webShootSpeed = 25f;

    [Tooltip("Kecepatan benang web ditarik kembali setelah Webi mendarat.")]
    public float webRetractSpeed = 30f;

    [Header("Surface Hazards")]
    [Tooltip("Kecepatan maksimum Webi di es, sebagai pengali Walk Speed. 1 = sama dengan permukaan biasa.")]
    [Min(1f)] public float iceTopSpeedMultiplier = 1.35f;

    [Tooltip("Percepatan Webi saat menambah kecepatan di es (unit per detik kuadrat).")]
    [Min(0.1f)] public float iceAcceleration = 7f;

    [Tooltip("Perlambatan Webi saat berhenti atau berbalik arah di es (unit per detik kuadrat). " +
             "Makin kecil makin licin.")]
    [Min(0.1f)] public float iceDeceleration = 3.5f;

    [Tooltip("Kecepatan awal Webi saat terlempar dari oil, selalu menjauhi permukaan yang dipijak " +
             "(unit per detik).")]
    [Min(0f)] public float oilFallImpulse = 2.5f;

    [Tooltip("Percepatan jatuh setelah terlempar dari oil (unit per detik kuadrat). Arahnya menjauhi " +
             "permukaan: oil di atas ground → jatuh ke atas, di dinding kanan → ke kanan, dan seterusnya.")]
    [Min(0f)] public float oilFallAcceleration = 25f;

    [Tooltip("Kecepatan maksimum saat terlempar karena oil (unit per detik).")]
    [Min(0.1f)] public float oilFallMaxSpeed = 22f;

    [Tooltip("Ruang tambahan di luar isi level yang masih dianggap di dalam peta. Kalau Webi melewatinya " +
             "saat jatuh, dia dianggap jatuh dari peta dan kalah (unit dunia).")]
    [Min(0f)] public float mapExitMargin = 1f;

    [Tooltip("Jarak pemeriksaan ke dalam permukaan untuk membaca tile overlay es/oil (unit dunia).")]
    [Min(0f)] public float surfaceSampleDepth = 0.3f;

    [Header("Spawn Surface")]
    [Tooltip("Permukaan tempat Webi menempel saat level dimulai. Platform Below = berdiri di atas platform " +
             "di bawahnya (biasa). Platform Above = bergelantungan di bawah platform di atasnya: Webi otomatis " +
             "diputar 180 derajat, jadi tidak perlu dirotasi manual di scene.")]
    public StartSurface startSurface = StartSurface.PlatformBelow;

    [Header("Surface Detection")]
    [Tooltip("Jarak kecil antara badan Webi dan permukaan platform. Biarkan 0 jika collider sudah pas.")]
    public float surfaceOffset = 0f;

    [Tooltip("Jarak deteksi ke depan dan ke bawah untuk mencari permukaan atau tikungan platform.")]
    public float surfaceProbeDistance = 0.2f;

    [Tooltip("Cadangan padding kaki sprite untuk tekstur yang belum terdaftar di tabel otomatis. " +
             "Nilai tiap sprite Webi diukur dari PNG-nya (baris alpha terakhir), karena kanvas " +
             "frame walk/shoot berbeda ukuran, jadi satu angka tidak bisa dipakai untuk semuanya.")]
    [Range(0f, 0.45f)]
    public float spriteFootPadding = 0.1926f;

    [Tooltip("Layer platform yang bisa dipijak dan ditembak web. Collider di layer lain dianggap bukan tujuan.")]
    public LayerMask platformLayer;

    [Header("Web References")]
    [Tooltip("LineRenderer untuk benang web, sekaligus garis bantuan saat membidik.")]
    public LineRenderer lineRenderer;

    [Tooltip("Opsional; jika kosong, dibuat otomatis untuk segmen web setelah portal.")]
    public LineRenderer portalLineRenderer;

    [Tooltip("Material garis bidik saat target tidak valid, misalnya menembak bukan platform. Jika kosong, dipakai material web biasa.")]
    public Material invalidAimMaterial;

    [Tooltip("Sprite kecil di ujung benang web. Kosongkan jika tidak ingin ada ujung visual.")]
    public Sprite webCornerSprite;

    [Header("Web Visuals")]
    [Tooltip("Ukuran sprite ujung web.")]
    public float webCornerScale = 1f;

    [Header("Aiming")]
    [Tooltip("Warna garis bidik saat target valid (bisa ditempeli web).")]
    public Color aimLineColor = Color.white;

    [Tooltip("Warna garis bidik saat target tidak valid.")]
    public Color invalidAimLineColor = Color.red;

    [Tooltip("Jarak maksimum garis bidik dalam unit dunia.")]
    public float aimRayDistance = 100f;

    [Tooltip("Ketebalan garis bidik.")]
    public float aimLineWidth = 0.08f;

    [Header("Audio")]
    [Tooltip("SFX langkah kaki. Diputar sekali setiap Walk Step Interval selama Webi berjalan.")]
    public AudioClip walkSfx;

    [Tooltip("Volume SFX langkah kaki (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float walkSfxVolume = 0.7f;

    [Tooltip("Jeda antar langkah kaki dalam detik. Samakan dengan irama animasi walk.")]
    public float walkStepInterval = 0.45f;

    [Tooltip("SFX saat jaring ditembakkan: ujung jaring melesat menuju platform.")]
    public AudioClip webShootSfx;

    [Tooltip("Volume SFX tembakan jaring (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float webShootSfxVolume = 0.7f;

    [Tooltip("SFX saat Webi mulai tertarik ke platform (awal perpindahan memakai jaring).")]
    public AudioClip webPullSfx;

    [Tooltip("Volume SFX tarikan jaring (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float webPullSfxVolume = 0.7f;

    [Header("Death")]
    [Tooltip("Clip animasi mati (char_webi_death). Diputar lewat PlayableGraph, tidak perlu state di Animator.")]
    public AnimationClip deathClip;

    [Tooltip("Cadangan lama animasi mati kalau Death Clip kosong. Kalau Death Clip diisi, durasinya mengikuti panjang clip.")]
    public float deathAnimationDuration = 1.6f;

    [Header("Debug")]
    [Tooltip("Tampilkan status debug Webi di Console/OnGUI saat Play Mode.")]
    public bool debugMode;

    #endregion

    #region Runtime State

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer webCornerRenderer;
    private Gradient webLineColorGradient;
    private readonly Gradient aimLineGradient = new Gradient();
    private Color lastAimLineColor;
    private bool hasAimLineColor;
    private Material webLineMaterial;
    private Material currentLineMaterial;
    private Material[] webLineMaterials;
    private Material[] invalidLineMaterials;
    private float webLineStartWidth;
    private float webLineEndWidth;
    private ContactFilter2D aimFilter;
    private ContactFilter2D platformFilter;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private readonly RaycastHit2D[] transitionHits = new RaycastHit2D[8];
    private readonly RaycastHit2D[] aimHits = new RaycastHit2D[32];

    /// <summary>
    /// Bagian bawah tiap tekstur sprite Webi yang transparan (0 = gambar menyentuh tepi bawah),
    /// diukur dari baris terakhir yang isinya solid (alpha >= 128, minimal 3 piksel).
    /// Tepi alpha>0 tidak dipakai karena beberapa frame punya bintik pudar nyasar jauh di bawah
    /// ujung kaki (mis. walk_05: bintik 1 piksel di baris 1182 padahal kaki di baris 1021),
    /// sehingga garis kaki dihitung terlalu tinggi dan Webi terlihat melayang sedikit.
    /// Kanvas tiap frame berbeda (walk_01 1080, walk_02 878, walk_03-11 1254, shoot 1600x900).
    /// </summary>
    private static readonly Dictionary<string, float> spriteFootPaddings =
        new Dictionary<string, float>
        {
            // pad piksel dari tinggi kanvas, mis. 208 / 1080
            { "char_webi_walk_01", 0.1926f },
            { "char_webi_walk_02", 0.1344f },
            { "char_webi_walk_03", 0.1595f },
            { "char_webi_walk_04", 0.1364f },
            { "char_webi_walk_05", 0.1858f },
            { "char_webi_walk_06", 0.1069f },
            { "char_webi_walk_07", 0.1292f },
            { "char_webi_walk_08", 0.1427f },
            { "char_webi_walk_09", 0.1818f },
            { "char_webi_walk_10", 0.1850f },
            { "char_webi_walk_11", 0.1707f },
            { "char_webi_shoot_01", 0.1389f },
            { "char_webi_shoot_02", 0.1544f },
            { "char_webi_shoot_03", 0.1556f },
            { "char_webi_shoot_04", 0.1500f },
            { "char_webi_shoot_05", 0.1600f },
            { "char_webi_shoot_06", 0.1633f },
            { "char_webi_shoot_07", 0.1867f },
            { "char_webi_shoot_08", 0.2056f },
        };

    /// <summary>Sisi platform tempat Webi menempel saat level dimulai.</summary>
    public enum StartSurface
    {
        PlatformBelow,
        PlatformAbove
    }

    /// <summary>Penyebab Webi mati: musuh/hazard, atau terperosok karena oil.</summary>
    public enum DeathCause
    {
        Enemy,
        Fell
    }

    private enum State { Falling, Walking, Aiming, Swinging, Transitioning, OilFalling }
    private enum SwingPhase { ShootingWeb, PullingPlayer, LandingPlayer, RetractingWeb }
    private State currentState = State.Walking;
    private SwingPhase swingPhase;
    private Vector2 currentSurfaceNormal = Vector2.up;
    private Vector2 currentForward = Vector2.right;
    private Vector2 targetSurfacePoint;
    private Vector2 targetSurfaceNormal;
    private Vector2 targetPosition;
    private Collider2D targetPlatformCollider;
    private Vector3 movingSwingLocalAnchor;
    private Vector3 movingSwingLocalNormal;
    private bool hasMovingSwingTarget;
    private Transform supportMotionTransform;
    private Vector3 supportMotionLastPosition;
    private Collider2D currentSurfaceCollider;
    private Collider2D landingSurfaceCollider;
    private Collider2D transitionTargetCollider;
    private Vector2 swingStartPosition;
    private Vector2 webTipPosition;
    private PortalController swingPortal;
    private Vector2 portalEntryPoint;
    private Vector2 portalExitPoint;
    private Vector2 portalExitDirection;
    private bool swingUsesPortal;
    private bool webTipPassedPortal;
    private bool playerPassedPortal;
    private Vector2 swingLandingStartPosition;
    private float swingLandingStartRotation;
    private float swingLandingElapsed;
    private float swingLandingDuration;
    private float pullStallElapsed;
    private float pullLastDistance;
    private Vector2 transitionStartPosition;
    private Vector2 transitionTargetPosition;
    private Vector2 transitionTargetNormal;
    private Vector2 landingSurfaceNormal;
    private float transitionStartRotation;
    private float transitionTargetRotation;
    private float transitionElapsed;
    private float targetRotation;
    private float portalCooldown;
    private bool hasLandingContact;
    private bool supportLossReported;
    private bool isDead;
    private bool movementLocked;
    // Tekanan bidik yang masuk saat Webi belum bisa membidik (mis. sedang berayun ke platform
    // lain). Dipakai sekali setelah dia mendarat, lalu dibuang begitu jarinya dilepas.
    private bool pendingAimResume;
    private float walkStepTimer;
    private Coroutine deathRoutine;
    private PlayableGraph deathGraph;
    private Tilemap groundTilemap;
    private Tilemap iceSurfaceTilemap;
    private Tilemap oilSurfaceTilemap;
    private float slideSpeed;
    private Vector3Int oilLossCell;
    private bool hasOilLossCell;
    private Vector2 oilFallDirection;
    private float oilFallSpeed;
    private Bounds levelContentBounds;
    private bool hasLevelContentBounds;
    private string debugStatus = "Ready";
    private string debugSupport = "Not checked";
    private string landingColliderName = "Unknown";
    private const float SurfaceTransitionDuration = 0.16f;
    private const float SwingLandingDuration = 0.2f;
    private const float SwingPullStallDuration = 0.5f;
    private const float SwingPullProgressThreshold = 0.01f;
    private const float SupportNormalTolerance = 0.6f;

    // Sudut kemiringan maksimum yang boleh diikuti sambil berjalan. Batasnya sedikit di atas set
    // slope 45° supaya lantai miring ikut diputar, sedangkan dinding (normal mendatar) tetap di
    // luar batas. Nilai lama 20° membuat slope 45° tidak pernah diikuti: badan Webi tetap tegak
    // dan collider-nya menabrak tanah saat mendaki, sehingga dia berhenti di kaki slope.
    private const float WalkableSlopeAngle = 46f;
    private const string IceSurfaceTilemapName = "Tilemap_Surface_Ice";
    private const string OilSurfaceTilemapName = "Tilemap_Surface_Oil";

    // Seberapa jauh ke kiri/kanan sepanjang permukaan tile overlay ikut dibaca. Sambungan dua ramp
    // 26 derajat selalu bergeser satu baris sel, jadi titik tepat di kaki bisa jatuh ke sel kosong
    // di bawah sudut; jarak ini (0,35 unit, sekitar 17% satu sel) cukup untuk menemukan ramp di
    // kedua sisinya tanpa membuat efek terasa melebar ke tanah datar sebelahnya.
    private const float SurfaceSampleSpan = 0.35f;

    #endregion

    #region Public Properties

    public bool IsAiming => currentState == State.Aiming;
    public bool CanInteract => currentState == State.Walking &&
        swingPhase != SwingPhase.RetractingWeb &&
        !movementLocked;

    /// <summary>
    /// True kalau Webi boleh mengambil barang (mis. benih): sedang berjalan normal atau sedang
    /// berayun dengan jaring. Dipakai pengambilan benih supaya tetap bisa diambil saat pindah
    /// platform; menanam tetap memakai CanInteract karena butuh Webi berhenti di tanah.
    /// </summary>
    public bool CanPickUpItems => (currentState == State.Walking || currentState == State.Swinging) &&
        swingPhase != SwingPhase.RetractingWeb &&
        !movementLocked;

    /// <summary>True saat gerak dan tembakan Webi dikunci (misalnya selama intro kamera level).</summary>
    public bool IsMovementLocked => movementLocked;

    /// <summary>True setelah Webi mati; gerak dan input sudah dihentikan.</summary>
    public bool IsDead => isDead;

    /// <summary>Penyebab kematian terakhir. Dibaca UI untuk memilih pesan di popup.</summary>
    public DeathCause LastDeathCause { get; private set; }

    #endregion

    /// <summary>Dipancarkan sekali saat Webi mati. UI (LevelHudController) mendengarkan event ini.</summary>
    public static event System.Action<PlayerController> Died;

    /// <summary>Dipancarkan setiap kali Webi menembakkan jaring (satu langkah). UI mengurangi moves lewat event ini.</summary>
    public static event System.Action<PlayerController> ShotWeb;

    #region Public Methods

    /// <summary>
    /// Mengunci atau membuka gerak Webi. Dipakai intro kamera awal level supaya pemain tidak
    /// bisa berjalan atau membidik sebelum seluruh fase intro selesai. Mengunci saat Webi sedang
    /// membidik langsung membatalkan bidikan itu.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (!locked)
        {
            return;
        }

        // Permintaan sentuh yang masuk saat gerak dikunci (mis. selama intro kamera) dibuang, supaya
        // Webi tidak langsung membidik atau menembak begitu kunci dibuka.
        TouchControlsInput.ClearRequests();

        if (currentState == State.Aiming)
        {
            CancelAiming();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Mematikan Webi: hentikan gerak, matikan kontrol, lalu umumkan lewat <see cref="Died"/>.
    /// Aman dipanggil berkali-kali; panggilan berikutnya diabaikan.
    /// </summary>
    public void Die()
    {
        Die(DeathCause.Enemy);
    }

    /// <summary>
    /// Mematikan Webi dengan penyebab tertentu. Penyebab <see cref="DeathCause.Fell"/> langsung
    /// menutup Webi tanpa animasi mati, karena ia sudah keluar dari area level.
    /// </summary>
    public void Die(DeathCause cause)
    {
        if (isDead)
        {
            return;
        }

        LastDeathCause = cause;
        isDead = true;

        HideWebVisuals();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.enabled = false;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        if (cause == DeathCause.Fell)
        {
            // Webi sudah di luar area level; tidak perlu animasi mati di tempat kosong.
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }

            Died?.Invoke(this);
            enabled = false;
            return;
        }

        // Komponen tetap hidup sebentar supaya animasi mati bisa selesai;
        // Update/FixedUpdate sudah berhenti sendiri lewat pengecekan isDead.
        deathRoutine = StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        // Sprite asli disembunyikan, animasi mati dimainkan di objek visual terpisah
        // (pola yang sama dengan animasi tumbuh benih) supaya Animator Controller
        // tidak perlu diubah sama sekali.
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        GameObject deathVisual = CreateDeathVisual();

        // Tunggu selama klipnya sendiri, bukan angka tetap: begitu animasi mati selesai,
        // Webi langsung hilang dan popup muncul. deathAnimationDuration hanya cadangan
        // kalau Death Clip tidak diisi. Dengan angka tetap 1,6 sementara klipnya 0,75,
        // badan Webi sempat diam ~0,85 detik dan terlihat seperti idle.
        float deathWait = deathClip != null && deathClip.length > 0.01f
            ? deathClip.length
            : Mathf.Max(0f, deathAnimationDuration);
        if (deathWait > 0f)
        {
            yield return new WaitForSecondsRealtime(deathWait);
        }

        HideWebVisuals();
        DestroyDeathVisual(deathVisual);

        // Pastikan tidak ada sisa gambar Webi yang tampil setelah animasi selesai.
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        deathRoutine = null;
        Died?.Invoke(this);
        enabled = false;
    }

    GameObject CreateDeathVisual()
    {
        if (deathClip == null || spriteRenderer == null)
        {
            return null;
        }

        GameObject visual = new GameObject("Webi Death Visual");
        visual.transform.SetParent(transform.parent, false);
        visual.transform.rotation = transform.rotation;
        visual.transform.localScale = transform.localScale;

        // Pivot sprite mati ada di titik sentuh tanahnya, jadi visual diletakkan tepat di garis kaki
        // Webi (bukan di origin), supaya pose rebah menempel di permukaan tempat Webi mati.
        Vector2 up = transform.up;
        visual.transform.position = (Vector2)transform.position - up * GetVisualSupportOffset(up);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = spriteRenderer.sharedMaterial;
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder;
        renderer.flipX = spriteRenderer.flipX;

        Animator deathAnimator = visual.AddComponent<Animator>();
        deathGraph = PlayableGraph.Create($"{name} Death");
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(deathGraph, deathClip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetDuration(deathClip.length);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(deathGraph, "Death", deathAnimator);
        output.SetSourcePlayable(clipPlayable);
        deathGraph.Play();

        return visual;
    }

    void DestroyDeathVisual(GameObject visual)
    {
        if (deathGraph.IsValid())
        {
            deathGraph.Destroy();
        }

        if (visual != null)
        {
            Destroy(visual);
        }
    }

    public bool TryTeleportThroughPortal(
        PortalController sourcePortal,
        Vector2 exitPosition,
        Vector2 exitDirection,
        float searchDistance
    )
    {
        if (portalCooldown > 0f ||
            (currentState != State.Walking && currentState != State.Falling) ||
            swingPhase == SwingPhase.RetractingWeb)
        {
            return false;
        }

        exitDirection.Normalize();
        int hitCount = Physics2D.Raycast(
            exitPosition,
            exitDirection,
            platformFilter,
            transitionHits,
            Mathf.Max(searchDistance, 0.1f)
        );
        RaycastHit2D surfaceHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            if (transitionHits[i].collider != null &&
                (surfaceHit.collider == null || transitionHits[i].distance < surfaceHit.distance))
            {
                surfaceHit = transitionHits[i];
            }
        }

        if (surfaceHit.collider == null)
        {
            Debug.LogWarning(
                $"Portal {sourcePortal.name} has no platform in its exit direction; Webi stayed at the entrance.",
                sourcePortal
            );
            return false;
        }

        Vector2 surfaceNormal = surfaceHit.normal.normalized;
        float landingRotation = GetSurfaceAngle(surfaceNormal);
        Vector2 landingPosition = GetSurfaceLandingPosition(
            surfaceHit.point,
            surfaceNormal,
            landingRotation
        );
        if (!IsPortalLandingClear(landingPosition, landingRotation, surfaceHit.collider))
        {
            Debug.LogWarning(
                $"Portal {sourcePortal.name} has no clear landing space at its exit; Webi stayed at the entrance.",
                sourcePortal
            );
            return false;
        }

        rb.position = landingPosition;
        rb.rotation = landingRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        currentSurfaceNormal = surfaceNormal;
        currentForward = GetForward(surfaceNormal);
        currentSurfaceCollider = surfaceHit.collider;
        hasLandingContact = false;
        supportLossReported = false;
        currentState = State.Walking;
        portalCooldown = 0.35f;
        SetDebugStatus($"Teleported through {sourcePortal.name}; landed on {surfaceHit.collider.name}.");
        return true;
    }

    bool IsPortalLandingClear(Vector2 position, float rotation, Collider2D landingPlatform)
    {
        Collider2D[] overlaps;
        if (playerCollider is CapsuleCollider2D capsule)
        {
            Vector2 capsuleSize = Vector2.Scale(
                capsule.size,
                new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
            );
            Vector2 scaledOffset = Vector2.Scale(capsule.offset, transform.lossyScale);
            Vector2 capsuleCenter = position +
                (Vector2)(Quaternion.Euler(0f, 0f, rotation) * scaledOffset);
            overlaps = Physics2D.OverlapCapsuleAll(
                capsuleCenter,
                capsuleSize,
                capsule.direction,
                rotation,
                platformLayer
            );
        }
        else
        {
            overlaps = Physics2D.OverlapBoxAll(
                position,
                playerCollider.bounds.size,
                rotation,
                platformLayer
            );
        }

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i] != null &&
                !overlaps[i].isTrigger &&
                overlaps[i] != landingPlatform)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Unity Events

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (webCornerSprite != null)
        {
            webCornerRenderer = CreateWebSpriteRenderer("Web Corner", webCornerSprite);
            webCornerRenderer.enabled = false;
        }

        webLineColorGradient = lineRenderer.colorGradient;
        webLineStartWidth = lineRenderer.startWidth;
        webLineEndWidth = lineRenderer.endWidth;
        Material[] initialMaterials = lineRenderer.sharedMaterials;
        webLineMaterial = initialMaterials.Length > 0 ? initialMaterials[0] : null;
        webLineMaterials = new[] { webLineMaterial };
        invalidLineMaterials = new[] { invalidAimMaterial != null ? invalidAimMaterial : webLineMaterial };
        EnsurePortalLineRenderer();
        SetLineMaterial(webLineMaterial);
        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.zero;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        platformFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = platformLayer,
            useTriggers = false
        };
        aimFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = Physics2D.AllLayers,
            useTriggers = true
        };

        currentSurfaceNormal = transform.up;
        currentForward = GetForward(currentSurfaceNormal);
        currentState = State.Falling;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        // Body digerakkan lewat MovePosition tiap frame; jangan biarkan physics tidur
        // di tengah jalan, karena input jalan jadi tidak berefek sampai body dibangunkan.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        groundTilemap = FindGroundTilemap();
        iceSurfaceTilemap = FindSurfaceTilemap(IceSurfaceTilemapName);
        oilSurfaceTilemap = FindSurfaceTilemap(OilSurfaceTilemapName);
        CacheLevelContentBounds();
        SnapToSurfaceBelow();
    }

    /// <summary>
    /// Saat level mulai, tempatkan Webi langsung dengan kaki menempel di platform terdekat.
    /// Tanpa ini Webi digambar melayang di posisi spawn sampai gravitasi menjatuhkannya.
    /// Kalau Start Surface disetel ke Platform Above, Webi malah menempel di bawah platform di atasnya.
    /// </summary>
    void SnapToSurfaceBelow()
    {
        if (playerCollider == null || rb == null)
        {
            return;
        }

        if (startSurface == StartSurface.PlatformAbove)
        {
            SnapToSurfaceAbove();
            return;
        }

        int hitCount = playerCollider.Cast(Vector2.down, platformFilter, castHits, 8f);
        RaycastHit2D closestHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null || Vector2.Dot(hit.normal.normalized, Vector2.up) < SupportNormalTolerance)
            {
                continue;
            }

            if (closestHit.collider == null || hit.distance < closestHit.distance)
            {
                closestHit = hit;
            }
        }

        string supportName;
        Vector2 surfacePoint;
        if (closestHit.collider != null)
        {
            currentSurfaceNormal = closestHit.normal.normalized;
            currentSurfaceCollider = closestHit.collider;
            surfacePoint = closestHit.point;
            supportName = closestHit.collider.name;
        }
        else
        {
            // Webi sering di-spawn tepat di dalam balok tanah, dan cast tidak melaporkan
            // collider yang di-overlap sejak awal. Puncak sel tilemap di kolom ini tetap bisa
            // dibaca, jadi dipakai supaya spawn tidak berhenti melayang di dalam platform.
            float tileTop;
            if (!TryGetTilemapSurfaceTop(rb.position.x, rb.position.y, out tileTop))
            {
                return;
            }

            currentSurfaceNormal = Vector2.up;
            currentSurfaceCollider = null;
            surfacePoint = new Vector2(rb.position.x, tileTop);
            supportName = "Tilemap_Ground";
        }

        // Collider composite bisa berhenti sedikit di bawah gambar rumput; kalau ada sel tilemap
        // di kolom ini, puncaknya dipakai supaya kaki mendarat di rumput yang benar-benar terlihat.
        float tilemapTop;
        if (currentSurfaceNormal.normalized.y >= 0.9f &&
            TryGetTilemapSurfaceTop(rb.position.x, rb.position.y, out tilemapTop) &&
            Mathf.Abs(tilemapTop - surfacePoint.y) > 0.05f)
        {
            surfacePoint = new Vector2(rb.position.x, tilemapTop);
            supportName = "Tilemap_Ground";
        }

        currentForward = GetForward(currentSurfaceNormal);

        Vector2 landingPosition = GetSurfaceLandingPosition(
            surfacePoint,
            currentSurfaceNormal,
            GetSurfaceAngle(currentSurfaceNormal)
        );
        rb.position = landingPosition;
        rb.rotation = GetSurfaceAngle(currentSurfaceNormal);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        hasLandingContact = false;
        slideSpeed = 0f;
        currentState = State.Walking;
        debugSupport = supportName;
        SetDebugStatus($"Spawn snapped onto {supportName}.");

        if (debugMode)
        {
            float spriteWorldHeight = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y)
                : 0f;
            Debug.Log(
                $"[Webi] surface Y={surfacePoint.y:F2} | origin Y={landingPosition.y:F2} | " +
                $"kaki={GetSurfaceOffset(currentSurfaceNormal):F2} | tinggi sprite={spriteWorldHeight:F2} | " +
                $"padding={GetSpriteFootPadding(spriteRenderer != null ? spriteRenderer.sprite : null):F4} | " +
                $"scale={transform.lossyScale.y:F2} | support={supportName}"
            );
        }
    }

    /// <summary>
    /// Menempelkan Webi ke sisi bawah platform di atasnya, dengan badan terbalik seperti
    /// bergelantungan. Dipakai kalau Start Surface disetel ke Platform Above.
    /// </summary>
    void SnapToSurfaceAbove()
    {
        int hitCount = playerCollider.Cast(Vector2.up, platformFilter, castHits, 8f);
        RaycastHit2D closestHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null ||
                Vector2.Dot(hit.normal.normalized, Vector2.down) < SupportNormalTolerance)
            {
                continue;
            }

            if (closestHit.collider == null || hit.distance < closestHit.distance)
            {
                closestHit = hit;
            }
        }

        Vector2 surfaceNormal;
        Vector2 surfacePoint;
        string supportName;
        if (closestHit.collider != null)
        {
            surfaceNormal = closestHit.normal.normalized;
            currentSurfaceCollider = closestHit.collider;
            surfacePoint = closestHit.point;
            supportName = closestHit.collider.name;
        }
        else
        {
            // Webi bisa di-spawn tepat menempel di dalam balok tanah, dan cast tidak melaporkan
            // collider yang sudah di-overlap sejak awal. Dasar sel tilemap di atasnya dipakai
            // sebagai cadangan supaya spawn tidak berhenti melayang di dalam platform.
            float tileBottom;
            if (!TryGetTilemapSurfaceBottom(rb.position.x, rb.position.y, out tileBottom))
            {
                return;
            }

            surfaceNormal = Vector2.down;
            currentSurfaceCollider = null;
            surfacePoint = new Vector2(rb.position.x, tileBottom);
            supportName = "Tilemap_Ground";
        }

        currentSurfaceNormal = surfaceNormal;
        currentForward = GetForward(currentSurfaceNormal);

        Vector2 landingPosition = GetSurfaceLandingPosition(
            surfacePoint,
            currentSurfaceNormal,
            GetSurfaceAngle(currentSurfaceNormal)
        );
        rb.position = landingPosition;
        rb.rotation = GetSurfaceAngle(currentSurfaceNormal);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        hasLandingContact = false;
        currentState = State.Walking;
        debugSupport = supportName;
        SetDebugStatus($"Spawn snapped under {supportName}.");
    }

    void EnsurePortalLineRenderer()
    {
        if (portalLineRenderer == null || portalLineRenderer == lineRenderer)
        {
            GameObject portalLineObject = new GameObject("Portal Web Segment");
            portalLineObject.transform.SetParent(transform, false);
            portalLineRenderer = portalLineObject.AddComponent<LineRenderer>();
        }

        portalLineRenderer.useWorldSpace = lineRenderer.useWorldSpace;
        portalLineRenderer.sharedMaterials = lineRenderer.sharedMaterials;
        portalLineRenderer.colorGradient = lineRenderer.colorGradient;
        portalLineRenderer.widthCurve = lineRenderer.widthCurve;
        portalLineRenderer.widthMultiplier = lineRenderer.widthMultiplier;
        portalLineRenderer.startWidth = lineRenderer.startWidth;
        portalLineRenderer.endWidth = lineRenderer.endWidth;
        portalLineRenderer.textureMode = lineRenderer.textureMode;
        portalLineRenderer.alignment = lineRenderer.alignment;
        portalLineRenderer.sortingLayerID = lineRenderer.sortingLayerID;
        portalLineRenderer.sortingOrder = lineRenderer.sortingOrder;
        portalLineRenderer.numCapVertices = lineRenderer.numCapVertices;
        portalLineRenderer.numCornerVertices = lineRenderer.numCornerVertices;
        portalLineRenderer.positionCount = 2;
        portalLineRenderer.enabled = false;
    }

    void OnDestroy()
    {
        if (webCornerRenderer != null)
        {
            Destroy(webCornerRenderer.gameObject);
        }

    }

    void Update()
    {
        // Keyboard.current bisa null di Android; di sana input datang dari tombol layar sentuh.
        if (isDead || movementLocked)
        {
            return;
        }

        bool pressedAimKey = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        if (pressedAimKey)
        {
            SetDebugStatus($"F pressed in state {currentState} at {rb.position}.");
        }

        // Analog bidik Android: menekan pad = mulai membidik, melepas = menembak atau batal.
        bool requestedTouchAim = TouchControlsInput.ConsumeAimStart();
        bool aimInputHeld = (Keyboard.current != null && Keyboard.current.fKey.isPressed) ||
            (TouchControlsInput.Active && TouchControlsInput.AimHeld);
        bool canStartAim = currentState == State.Walking &&
            swingPhase != SwingPhase.RetractingWeb;

        // Tekanan yang masuk saat Webi belum bisa membidik (sedang berayun atau dipindah jaring ke
        // platform lain) diingat sampai jarinya dilepas. Tanpa ini tekanannya cuma terpakai sekali
        // lalu hilang: Webi sampai di platform tujuan, jari masih menahan analog, tetapi garis
        // bidik tidak pernah muncul karena event "mulai bidik" sudah lewat.
        if ((pressedAimKey || requestedTouchAim) &&
            !canStartAim &&
            currentState != State.Aiming)
        {
            pendingAimResume = true;
        }

        if (!aimInputHeld)
        {
            pendingAimResume = false;
        }

        if (canStartAim && (pressedAimKey || requestedTouchAim || pendingAimResume))
        {
            pendingAimResume = false;
            currentState = State.Aiming;
        }
        else if (currentState == State.Aiming)
        {
            HandleAiming();
            if (pressedAimKey || TouchControlsInput.ConsumeAimCancel())
            {
                CancelAiming();
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        portalCooldown = Mathf.Max(0f, portalCooldown - Time.fixedDeltaTime);
        FollowSupportSurfaceMotion();

        // Jatuh biasa: hanya keluar dari bawah/samping peta yang dihitung, supaya mengayun di atas
        // puncak level tidak dianggap terjatuh.
        if (currentState == State.Falling && IsOutsideLevelBounds(false))
        {
            Die(DeathCause.Fell);
            return;
        }

        // Terlempar karena oil: arahnya bisa ke atas, jadi keempat sisi peta dihitung.
        if (currentState == State.OilFalling && IsOutsideLevelBounds(true))
        {
            Die(DeathCause.Fell);
            return;
        }

        if (currentState == State.Falling)
        {
            HandleLanding();
        }
        else if (currentState == State.OilFalling)
        {
            HandleOilFall();
        }
        else if (currentState == State.Walking)
        {
            HandleWalking();
        }
        else if (currentState == State.Aiming)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else if (currentState == State.Swinging)
        {
            HandleSwinging();
        }
        else if (currentState == State.Transitioning)
        {
            HandleSurfaceTransition();
        }

        if (swingPhase == SwingPhase.RetractingWeb)
        {
            HandleWebRetraction();
        }

        UpdateWalkingAnimation();
    }

    void HandleLanding()
    {
        RaycastHit2D landingHit = default;
        float closestLandingDistance = float.PositiveInfinity;
        int landingHitCount = playerCollider.Cast(
            -currentSurfaceNormal,
            platformFilter,
            castHits,
            Mathf.Max(surfaceProbeDistance, 0.1f)
        );

        for (int i = 0; i < landingHitCount; i++)
        {
            if (castHits[i].collider != null && castHits[i].distance < closestLandingDistance)
            {
                landingHit = castHits[i];
                closestLandingDistance = castHits[i].distance;
            }
        }

        if (landingHit.collider != null)
        {
            landingSurfaceNormal = landingHit.normal.normalized;
            landingSurfaceCollider = landingHit.collider;
            landingColliderName = landingHit.collider.name;
            float correction = closestLandingDistance - GetExpectedSupportGap(landingSurfaceNormal);
            if (Mathf.Abs(correction) > 0.0001f)
            {
                rb.MovePosition(rb.position - landingSurfaceNormal * correction);
            }

            hasLandingContact = true;
        }

        if (!hasLandingContact)
        {
            return;
        }

        currentSurfaceNormal = landingSurfaceNormal;
        currentSurfaceCollider = landingSurfaceCollider;
        currentForward = GetForward(currentSurfaceNormal);
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));
        hasLandingContact = false;
        slideSpeed = 0f;
        currentState = State.Walking;
        debugSupport = landingColliderName;
        SetDebugStatus($"Landed on {landingColliderName}; normal {currentSurfaceNormal}.");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        CaptureLandingContact(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CaptureLandingContact(collision);
    }

    void CaptureLandingContact(Collision2D collision)
    {
        if (currentState != State.Falling ||
            (platformLayer.value & (1 << collision.gameObject.layer)) == 0 ||
            collision.contactCount == 0)
        {
            return;
        }

        landingSurfaceNormal = collision.GetContact(0).normal.normalized;
        landingSurfaceCollider = collision.collider;
        landingColliderName = collision.gameObject.name;
        hasLandingContact = true;
    }

    void UpdateWalkingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool hasMoveInput = !movementLocked &&
            ((Keyboard.current != null &&
              (Keyboard.current.aKey.isPressed ||
               Keyboard.current.dKey.isPressed ||
               Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed)) ||
             TouchControlsInput.MoveDirection != 0);

        animator.SetBool("IsWalking", currentState == State.Walking && hasMoveInput);
    }

    /// <summary>
    /// Webi ikut terbawa permukaan yang sedang dipijak (mis. Moving Ground). Delta posisi collider
    /// pijakan diterapkan sebelum logika langkah, jadi ray pijakan dan snap dihitung dari posisi
    /// baru dan Webi tidak ditinggal platform yang bergerak. Berlaku juga saat membidik, karena
    /// Webi berhenti bergerak tetapi tetap berdiri di platform yang sama. Platform yang berputar
    /// belum diikuti (Moving Ground hanya berpindah).
    /// </summary>
    void FollowSupportSurfaceMotion()
    {
        bool standingOnSurface = currentState == State.Walking || currentState == State.Aiming;
        if (!standingOnSurface || currentSurfaceCollider == null)
        {
            supportMotionTransform = null;
            return;
        }

        Transform surfaceTransform = currentSurfaceCollider.transform;
        if (surfaceTransform != supportMotionTransform)
        {
            supportMotionTransform = surfaceTransform;
            supportMotionLastPosition = surfaceTransform.position;
            return;
        }

        Vector3 surfaceDelta = surfaceTransform.position - supportMotionLastPosition;
        supportMotionLastPosition = surfaceTransform.position;
        if (surfaceDelta.sqrMagnitude <= 0.0000001f)
        {
            return;
        }

        rb.position += (Vector2)surfaceDelta;
    }

    void HandleWalking()
    {
        float moveInput = 0f;
        if (!movementLocked)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput = 1f;
            }

            // Tombol kiri/kanan layar sentuh hanya dipakai kalau keyboard tidak menekan apa pun.
            if (moveInput == 0f)
            {
                moveInput = TouchControlsInput.MoveDirection;
            }
        }

        if (moveInput != 0f && spriteRenderer != null)
        {
            spriteRenderer.flipX = moveInput > 0f;
        }

        // Permukaan yang dipijak menentukan perilaku gerak: es meluncur, oil melepas pijakan.
        // Webi selalu jatuh saat menapak oil; nasibnya ditentukan oleh apa yang ada di bawahnya
        // (mendarat di ground = selamat, tidak ada ground = kalah saat keluar peta).
        SurfaceKind surfaceKind = GetSurfaceKindAtFeet();
        if (surfaceKind == SurfaceKind.Oil)
        {
            // Satu sel oil melepas pijakan sekali saja. Tanpa ini, Webi yang mendarat lagi di sel
            // yang sama akan terus kehilangan pijakan dan memantul di tempat.
            Vector3Int feetCell = GetFeetSurfaceCell(oilSurfaceTilemap);
            if (!hasOilLossCell || feetCell != oilLossCell)
            {
                oilLossCell = feetCell;
                hasOilLossCell = true;
                BeginOilFall();
                return;
            }
        }
        else
        {
            hasOilLossCell = false;
        }

        float stepSpeed = ResolveWalkSpeed(moveInput, surfaceKind == SurfaceKind.Ice);
        float moveSign = Mathf.Abs(stepSpeed) > 0.05f ? Mathf.Sign(stepSpeed) : 0f;

        if (moveSign != 0f)
        {
            Vector2 moveDirection = currentForward * moveSign;
            if (TryTransitionToNextSurface(moveDirection, out Vector2 nextNormal, out Vector2 nextSurfacePosition))
            {
                BeginSurfaceTransition(nextNormal, nextSurfacePosition);
                return;
            }
            int hitCount = playerCollider.Cast(moveDirection, platformFilter, castHits, surfaceProbeDistance);
            for (int i = 0; i < hitCount; i++)
            {
                if (castHits[i].collider != null)
                {
                    Vector2 hitNormal = castHits[i].normal.normalized;
                    if (Vector2.Angle(currentSurfaceNormal, hitNormal) >= WalkableSlopeAngle)
                    {
                        continue;
                    }

                    currentSurfaceNormal = hitNormal;
                    currentForward = GetForward(currentSurfaceNormal);
                    break;
                }
            }
        }

        // Permukaan datar/hampir datar (lantai platform) memakai ray pendek dari titik kaki.
        // Ray ini tidak pernah mulai di dalam tile, jadi tetap terbaca walau collider Webi
        // menempel ke platform; CapsuleCast bisa gagal di kondisi itu dan membuat Webi
        // kehilangan pijakan padahal sedang berdiri di atas rumput.
        if (currentSurfaceNormal.normalized.y >= 0.9f)
        {
            RaycastHit2D groundRay = ProbeGroundRay(currentSurfaceNormal);
            // distance 0 berarti ray mulai di dalam tile (titik hit tidak bisa dipakai sebagai
            // acuan ketinggian), jadi kasus itu diserahkan ke jalur cast di bawah.
            if (groundRay.collider != null &&
                groundRay.distance > 0.001f &&
                groundRay.normal.normalized.y >= 0.9f)
            {
                Vector2 groundNormal = groundRay.normal.normalized;
                currentSurfaceNormal = groundNormal;
                currentForward = GetForward(currentSurfaceNormal);

                currentSurfaceCollider = groundRay.collider;
                debugSupport = groundRay.collider.name;
                supportLossReported = false;

                Vector2 groundPosition = GetSurfaceLandingPosition(
                    groundRay.point,
                    currentSurfaceNormal,
                    GetSurfaceAngle(currentSurfaceNormal)
                );
                groundPosition += currentForward * (stepSpeed * Time.fixedDeltaTime);

                if (Mathf.Abs(stepSpeed) > 0.05f)
                {
                    PlayWalkStep(Time.fixedDeltaTime);
                }
                else
                {
                    walkStepTimer = 0f;
                }

                rb.WakeUp();
                rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));
                rb.MovePosition(groundPosition);
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        RaycastHit2D surfaceHit = default;
        float closestSurfaceDistance = float.PositiveInfinity;
        int surfaceHitCount = playerCollider.Cast(
            -currentSurfaceNormal,
            platformFilter,
            castHits,
            2f
        );
        for (int i = 0; i < surfaceHitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            // Toleransi normal dilonggarkan supaya sudut platform yang sedikit miring
            // tidak membuat Webi kehilangan pijakan lalu terkunci di tempat.
            if (Vector2.Dot(hit.normal.normalized, currentSurfaceNormal) < SupportNormalTolerance)
            {
                continue;
            }

            // Platform yang sedang diinjak tetap diprioritaskan, tapi collider tetangga
            // (misalnya sambungan tile atau platform lain yang menempel) tetap diterima.
            float biasedDistance = hit.distance;
            if (currentSurfaceCollider != null && hit.collider != currentSurfaceCollider)
            {
                biasedDistance += 0.5f;
            }

            if (biasedDistance < closestSurfaceDistance)
            {
                surfaceHit = hit;
                closestSurfaceDistance = biasedDistance;
            }
        }

        if (surfaceHit.collider == null)
        {
            // Collider yang menempel/tertanam di platform tidak selalu dilaporkan oleh
            // CapsuleCast (overlap di awal cast diabaikan), sehingga Webi bisa kehilangan
            // pijakan lalu berhenti di tempat. Ray dari titik kaki yang diangkat sedikit
            // selalu terbaca, jadi dipakai sebagai cadangan sebelum memutuskan jatuh.
            RaycastHit2D groundRay = ProbeGroundRay(currentSurfaceNormal);
            if (groundRay.collider != null && groundRay.distance > 0.001f)
            {
                debugSupport = groundRay.collider.name;
                supportLossReported = false;
                currentSurfaceCollider = groundRay.collider;

                Vector2 groundPosition = GetSurfaceLandingPosition(
                    groundRay.point,
                    currentSurfaceNormal,
                    GetSurfaceAngle(currentSurfaceNormal)
                );
                groundPosition += currentForward * (stepSpeed * Time.fixedDeltaTime);
                rb.WakeUp();
                rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));
                rb.MovePosition(groundPosition);
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (!supportLossReported)
            {
                SetDebugStatus($"No support under Webi at {rb.position}; falling instead of locking in place.");
                supportLossReported = true;
            }

            BeginFallOffSurface();
            return;
        }

        debugSupport = surfaceHit.collider.name;
        supportLossReported = false;

        if (moveInput == 0f)
        {
            currentSurfaceNormal = surfaceHit.normal.normalized;
            currentForward = GetForward(currentSurfaceNormal);
        }

        rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));

        Vector2 nextPosition = GetSnappedPosition(currentSurfaceNormal, currentSurfaceCollider);
        nextPosition += currentForward * (stepSpeed * Time.fixedDeltaTime);

        if (Mathf.Abs(stepSpeed) > 0.05f)
        {
            PlayWalkStep(Time.fixedDeltaTime);
        }
        else
        {
            walkStepTimer = 0f;
        }

        rb.WakeUp();
        rb.MovePosition(nextPosition);
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Ray pendek dari titik kaki (diangkat sedikit supaya tidak mulai di dalam collider).
    /// Dipakai kalau CapsuleCast tidak menemukan platform yang sedang dipijak.
    /// </summary>
    RaycastHit2D ProbeGroundRay(Vector2 surfaceNormal)
    {
        float lift = Mathf.Max(surfaceProbeDistance, 0.1f);
        Vector2 feetPoint = rb.position - surfaceNormal * GetSurfaceOffset(surfaceNormal);
        return Physics2D.Raycast(
            feetPoint + surfaceNormal * lift,
            -surfaceNormal,
            lift * 4f,
            platformLayer
        );
    }

    void BeginSurfaceTransition(Vector2 nextNormal, Vector2 nextPosition)
    {
        transitionStartPosition = rb.position;
        transitionTargetPosition = nextPosition;
        transitionTargetNormal = nextNormal;
        transitionStartRotation = rb.rotation;
        transitionTargetRotation = GetSurfaceAngle(nextNormal);
        transitionElapsed = 0f;
        currentState = State.Transitioning;
        SetDebugStatus($"Surface transition started toward normal {nextNormal}.");
    }

    void HandleSurfaceTransition()
    {
        transitionElapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(transitionElapsed / SurfaceTransitionDuration);
        float easedProgress = progress * progress * (3f - 2f * progress);

        rb.MovePosition(Vector2.Lerp(transitionStartPosition, transitionTargetPosition, easedProgress));
        rb.MoveRotation(Mathf.LerpAngle(transitionStartRotation, transitionTargetRotation, easedProgress));
        rb.linearVelocity = Vector2.zero;

        if (progress >= 1f)
        {
            currentSurfaceNormal = transitionTargetNormal;
            currentSurfaceCollider = transitionTargetCollider;
            currentForward = GetForward(currentSurfaceNormal);
            currentState = State.Walking;
            SetDebugStatus($"Surface transition complete; support {debugSupport}.");
        }
    }

    bool TryTransitionToNextSurface(Vector2 moveDirection, out Vector2 nextNormal, out Vector2 nextPosition)
    {
        nextNormal = currentSurfaceNormal;
        nextPosition = rb.position;
        transitionTargetCollider = null;
        Vector2 candidateDirection = moveDirection.normalized;
        int hitCount = playerCollider.Cast(
            candidateDirection,
            platformFilter,
            transitionHits,
            Mathf.Max(0.05f, surfaceProbeDistance)
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = transitionHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            Vector2 nextSurfaceNormal = hit.normal.normalized;
            if (Vector2.Angle(currentSurfaceNormal, nextSurfaceNormal) < 20f ||
                Vector2.Dot(nextSurfaceNormal, candidateDirection) > -0.8f)
            {
                continue;
            }

            nextNormal = nextSurfaceNormal;
            nextPosition = hit.point + nextSurfaceNormal * GetSurfaceOffset(nextSurfaceNormal);
            transitionTargetCollider = hit.collider;
            return true;
        }

        Vector2 contactPoint = rb.position - currentSurfaceNormal * GetSurfaceOffset(currentSurfaceNormal);
        float cornerProbeDistance = Mathf.Max(surfaceProbeDistance, 0.05f);
        Vector2 pointAhead = contactPoint + candidateDirection * cornerProbeDistance;
        RaycastHit2D surfaceAhead = Physics2D.Raycast(
            pointAhead,
            -currentSurfaceNormal,
            2f,
            platformLayer
        );

        if (surfaceAhead.collider == null)
        {
            Vector2 cornerProbeOrigin = pointAhead - currentSurfaceNormal * Mathf.Max(surfaceOffset, 0.01f);
            RaycastHit2D cornerHit = Physics2D.Raycast(
                cornerProbeOrigin,
                -candidateDirection,
                2f,
                platformLayer
            );

            if (cornerHit.collider != null)
            {
                nextNormal = cornerHit.normal.normalized;
                nextPosition = cornerHit.point + nextNormal * GetSurfaceOffset(nextNormal);
                transitionTargetCollider = cornerHit.collider;
                return true;
            }
        }

        return false;
    }

    Vector2 GetSnappedPosition(Vector2 surfaceNormal, Collider2D supportCollider)
    {
        Vector2 snappedPosition = rb.position;
        int hitCount = playerCollider.Cast(-surfaceNormal, platformFilter, castHits, 2f);
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null ||
                (supportCollider != null && hit.collider != supportCollider) ||
                Vector2.Dot(hit.normal.normalized, surfaceNormal.normalized) < SupportNormalTolerance)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
            }
        }

        if (float.IsPositiveInfinity(closestDistance))
        {
            return snappedPosition;
        }

        float correction = closestDistance - GetExpectedSupportGap(surfaceNormal);
        if (Mathf.Abs(correction) > 0.0001f)
        {
            snappedPosition -= surfaceNormal * correction;
        }

        return PinFeetToSurface(snappedPosition, surfaceNormal);
    }

    float GetSurfaceAngle(Vector2 surfaceNormal)
    {
        return Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg - 90f;
    }

    void HandleAiming()
    {
        if (Camera.main == null)
        {
            lineRenderer.enabled = false;
            portalLineRenderer.enabled = false;
            debugStatus = "Aiming unavailable: Main Camera missing";
            return;
        }

        // Arah bidik: analog layar sentuh kalau sedang dipakai, kalau tidak mengikuti posisi mouse.
        // Jari yang masih di tengah analog berarti belum ada arah, jadi garis bidik disembunyikan
        // daripada memakai posisi mouse (di Android tidak ada mouse).
        if (TouchControlsInput.Active && TouchControlsInput.AimHeld && !TouchControlsInput.HasAimDirection)
        {
            lineRenderer.enabled = false;
            portalLineRenderer.enabled = false;
            debugStatus = "Aiming with touch pad; no direction yet";
            return;
        }

        Vector2 direction;
        bool usingTouchAim = TouchControlsInput.Active && TouchControlsInput.HasAimDirection;
        if (usingTouchAim)
        {
            direction = TouchControlsInput.ToWorldDirection(Camera.main);
        }
        else
        {
            if (Mouse.current == null)
            {
                lineRenderer.enabled = false;
                portalLineRenderer.enabled = false;
                debugStatus = "Aiming unavailable: mouse missing";
                return;
            }

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            direction = mousePosition - rb.position;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.right;
        }

        direction.Normalize();
        float rayDistance = Mathf.Max(aimRayDistance, 0.01f);
        int hitCount = Physics2D.Raycast(
            rb.position,
            direction,
            aimFilter,
            aimHits,
            rayDistance
        );
        RaycastHit2D hit = default;
        PortalController hitPortal = null;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D candidate = aimHits[i];
            PortalController candidatePortal = candidate.collider != null
                ? candidate.collider.GetComponentInParent<PortalController>()
                : null;
            if (candidate.collider == null ||
                (candidate.collider.isTrigger && candidatePortal == null) ||
                candidate.collider.attachedRigidbody == rb)
            {
                continue;
            }

            if (hit.collider == null || candidate.distance < hit.distance)
            {
                hit = candidate;
                hitPortal = candidatePortal;
            }
        }

        bool traversesPortal = false;
        Vector2 aimPortalEntry = default;
        Vector2 aimPortalExit = default;
        Vector2 aimExitDirection = default;
        if (hitPortal != null &&
            hitPortal.TryGetExit(hit.point, direction, out aimPortalExit, out aimExitDirection))
        {
            aimPortalEntry = hit.point;
            float remainingDistance = Mathf.Max(0f, rayDistance - hit.distance);
            int exitHitCount = Physics2D.Raycast(
                aimPortalExit,
                aimExitDirection,
                aimFilter,
                aimHits,
                remainingDistance
            );
            hit = default;
            for (int i = 0; i < exitHitCount; i++)
            {
                RaycastHit2D candidate = aimHits[i];
                if (candidate.collider == null ||
                    candidate.collider.isTrigger ||
                    candidate.collider.GetComponentInParent<PortalController>() != null ||
                    candidate.collider.attachedRigidbody == rb)
                {
                    continue;
                }

                if (hit.collider == null || candidate.distance < hit.distance)
                {
                    hit = candidate;
                }
            }

            traversesPortal = true;
        }

        bool canAimAtHit = hit.collider != null &&
            (traversesPortal || hitPortal == null) &&
            (platformLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        SetLineMaterial(canAimAtHit ? webLineMaterial : invalidAimMaterial);
        Vector2 aimEndPoint = hit.collider != null
            ? hit.point
            : traversesPortal
                ? aimPortalExit + aimExitDirection * Mathf.Max(0f, rayDistance - Vector2.Distance(rb.position, aimPortalEntry))
                : rb.position + direction * rayDistance;
        Color currentAimColor = canAimAtHit ? aimLineColor : invalidAimLineColor;
        debugStatus = canAimAtHit
            ? traversesPortal
                ? $"Aim passed through {hitPortal.name} and hit {hit.collider.name} at {hit.point}"
                : $"Aim hit {hit.collider.name} at {hit.point}"
            : hit.collider != null
                ? $"Aim blocked by {hit.collider.name}; target is not a platform"
                : $"Aim ray missed all solid colliders; Platform Layer ({platformLayer.value})";
        if (traversesPortal)
        {
            DrawAimingLine(rb.position, aimPortalEntry, currentAimColor);
            DrawPortalLine(aimPortalExit, aimEndPoint, currentAimColor);
        }
        else
        {
            portalLineRenderer.enabled = false;
            DrawAimingLine(rb.position, aimEndPoint, currentAimColor);
        }

        // Klik mouse diabaikan selama analog bidik ditahan, supaya menekan pad di Editor tidak
        // langsung menembak lewat jalur mouse.
        bool pressedShootMouse = !TouchControlsInput.AimHeld &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;
        bool releasedTouchAim = TouchControlsInput.ConsumeShoot();
        if (canAimAtHit && (pressedShootMouse || releasedTouchAim))
        {
            RestoreWebLineStyle();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = Vector2.Dot(transform.right, direction) > 0f;
            }

            targetSurfacePoint = hit.point;
            targetSurfaceNormal = hit.normal;
            targetPlatformCollider = hit.collider;
            targetRotation = Mathf.Atan2(targetSurfaceNormal.y, targetSurfaceNormal.x) * Mathf.Rad2Deg - 90f;
            targetPosition = GetSurfaceLandingPosition(
                targetSurfacePoint,
                targetSurfaceNormal,
                targetRotation
            );
            // Platform bergerak (mis. Moving Ground) bisa berpindah selama Webi ditarik. Jangkar
            // disimpan di ruang lokal collider, bukan sebagai titik dunia, supaya tarikan dan
            // pendaratan tetap mengikuti platformnya.
            // Berlaku juga untuk ayunan lewat portal: hit setelah portal berada di ruang dunia
            // yang sama dengan platformnya, jadi jangkar lokalnya tetap sah.
            hasMovingSwingTarget = hit.collider != null;
            if (hasMovingSwingTarget)
            {
                Transform targetTransform = hit.collider.transform;
                movingSwingLocalAnchor = targetTransform.InverseTransformPoint(hit.point);
                movingSwingLocalNormal = targetTransform.InverseTransformDirection(targetSurfaceNormal);
            }
            swingStartPosition = rb.position;
            webTipPosition = rb.position;
            swingPortal = traversesPortal ? hitPortal : null;
            swingUsesPortal = traversesPortal;
            portalEntryPoint = aimPortalEntry;
            portalExitPoint = aimPortalExit;
            portalExitDirection = aimExitDirection;
            webTipPassedPortal = false;
            playerPassedPortal = false;
            lineRenderer.SetPosition(0, swingStartPosition);
            lineRenderer.SetPosition(1, webTipPosition);
            portalLineRenderer.enabled = false;
            swingPhase = SwingPhase.ShootingWeb;
            currentState = State.Swinging;
            ShotWeb?.Invoke(this);
            PlaySfx(webShootSfx, webShootSfxVolume);
            UpdateWebVisuals(swingStartPosition, webTipPosition);
            if (animator != null)
            {
                animator.SetTrigger("ShootWeb");
            }
            SetDebugStatus($"Swing started toward {hit.collider.name}.");
        }
        else if (releasedTouchAim)
        {
            // Analog dilepas saat target tidak valid: bidikan berakhir tanpa menembak, supaya Webi
            // tidak tertinggal di mode membidik tanpa garis.
            CancelAiming();
        }
    }

    void DrawAimingLine(Vector2 start, Vector2 end, Color color)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        SetAimLineColor(color);
        lineRenderer.startWidth = Mathf.Max(aimLineWidth, 0f);
        lineRenderer.endWidth = Mathf.Max(aimLineWidth, 0f);
        lineRenderer.enabled = true;
    }

    void DrawPortalLine(Vector2 start, Vector2 end, Color color)
    {
        portalLineRenderer.positionCount = 2;
        portalLineRenderer.SetPosition(0, start);
        portalLineRenderer.SetPosition(1, end);
        portalLineRenderer.startWidth = Mathf.Max(aimLineWidth, 0f);
        portalLineRenderer.endWidth = Mathf.Max(aimLineWidth, 0f);
        portalLineRenderer.startColor = color;
        portalLineRenderer.endColor = color;
        portalLineRenderer.enabled = true;
    }

    void RestoreWebLineStyle()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.colorGradient = webLineColorGradient;
        lineRenderer.startWidth = webLineStartWidth;
        lineRenderer.endWidth = webLineEndWidth;
        portalLineRenderer.colorGradient = webLineColorGradient;
        portalLineRenderer.startWidth = webLineStartWidth;
        portalLineRenderer.endWidth = webLineEndWidth;
        SetLineMaterial(webLineMaterial);
        hasAimLineColor = false;
    }

    void SetAimLineColor(Color color)
    {
        if (hasAimLineColor && lastAimLineColor == color)
        {
            return;
        }

        aimLineGradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
        );
        lineRenderer.colorGradient = aimLineGradient;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        portalLineRenderer.colorGradient = aimLineGradient;
        portalLineRenderer.startColor = color;
        portalLineRenderer.endColor = color;
        lastAimLineColor = color;
        hasAimLineColor = true;
    }

    void SetLineMaterial(Material material)
    {
        if (currentLineMaterial == material)
        {
            return;
        }

        currentLineMaterial = material;
        lineRenderer.sharedMaterials = material == invalidAimMaterial
            ? invalidLineMaterials
            : webLineMaterials;
        portalLineRenderer.sharedMaterials = lineRenderer.sharedMaterials;
    }

    SpriteRenderer CreateWebSpriteRenderer(string objectName, Sprite sprite)
    {
        GameObject visualObject = new GameObject(objectName);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingLayerID = lineRenderer.sortingLayerID;
        renderer.sortingOrder = lineRenderer.sortingOrder + 1;
        return renderer;
    }

    void UpdateWebVisuals(Vector2 start, Vector2 end)
    {
        bool showPortalSegments = swingUsesPortal &&
            ((swingPhase == SwingPhase.ShootingWeb && webTipPassedPortal) ||
             (swingPhase == SwingPhase.PullingPlayer && !playerPassedPortal));
        if (showPortalSegments)
        {
            SetLineSegment(lineRenderer, start, portalEntryPoint);
            SetLineSegment(portalLineRenderer, portalExitPoint, end);
        }
        else
        {
            SetLineSegment(lineRenderer, start, end);
            portalLineRenderer.enabled = false;
        }

        Vector2 direction = end - (showPortalSegments ? portalExitPoint : start);
        float length = direction.magnitude;
        if (webCornerRenderer == null)
        {
            return;
        }

        webCornerRenderer.enabled = length > 0.0001f;
        if (length <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion cornerRotation = Quaternion.Euler(0f, 0f, angle);
        Transform cornerTransform = webCornerRenderer.transform;
        cornerTransform.rotation = cornerRotation;
        cornerTransform.localScale = Vector3.one * webCornerScale;
        Vector3 cornerTipOffset = new Vector3(
            webCornerSprite.bounds.min.x * webCornerScale,
            webCornerSprite.bounds.center.y * webCornerScale,
            0f
        );
        cornerTransform.position = end - (Vector2)(cornerRotation * cornerTipOffset);
    }

    Vector2 GetPullDestination()
    {
        return swingUsesPortal && !playerPassedPortal ? portalEntryPoint : targetPosition;
    }

    void SetLineSegment(LineRenderer renderer, Vector2 start, Vector2 end)
    {
        renderer.positionCount = 2;
        renderer.SetPosition(0, start);
        renderer.SetPosition(1, end);
        renderer.enabled = true;
    }

    void HideWebVisuals()
    {
        lineRenderer.enabled = false;
        portalLineRenderer.enabled = false;
        if (webCornerRenderer != null)
        {
            webCornerRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Mengikuti platform tujuan yang bergerak selama ayunan. Jangkar disimpan di ruang lokal
    /// collider saat jaring ditembak, lalu tiap physics frame dihitung ulang ke dunia: arah tarikan
    /// dan titik pendaratan mengejar posisi platform sekarang, bukan posisi saat jaring ditembak.
    /// Tanpa ini Webi menarik dirinya ke tempat kosong begitu platformnya bergeser.
    /// </summary>
    void TrackMovingSwingTarget()
    {
        if (!hasMovingSwingTarget || targetPlatformCollider == null)
        {
            return;
        }

        Transform targetTransform = targetPlatformCollider.transform;
        Vector3 worldAnchor = targetTransform.TransformPoint(movingSwingLocalAnchor);
        Vector2 delta = (Vector2)worldAnchor - targetSurfacePoint;
        if (delta.sqrMagnitude <= 0.0000001f)
        {
            return;
        }

        targetSurfacePoint = worldAnchor;
        targetPosition += delta;

        // Ujung jaring sudah menempel di platform, jadi ikut bergeser. Saat ShootingWeb ujungnya
        // masih terbang mengejar targetSurfacePoint, jadi tidak perlu digeser.
        if (swingPhase != SwingPhase.ShootingWeb)
        {
            webTipPosition += delta;
        }

        Vector3 worldNormal = targetTransform.TransformDirection(movingSwingLocalNormal);
        if (worldNormal.sqrMagnitude > 0.0001f)
        {
            targetSurfaceNormal = ((Vector2)worldNormal).normalized;
            targetRotation = Mathf.Atan2(targetSurfaceNormal.y, targetSurfaceNormal.x) * Mathf.Rad2Deg - 90f;
        }
    }

    void HandleSwinging()
    {
        TrackMovingSwingTarget();

        if (swingPhase == SwingPhase.ShootingWeb)
        {
            float movementDistance = webShootSpeed * Time.fixedDeltaTime;
            if (swingUsesPortal && !webTipPassedPortal)
            {
                float distanceToPortal = Vector2.Distance(webTipPosition, portalEntryPoint);
                if (movementDistance >= distanceToPortal)
                {
                    webTipPosition = portalExitPoint;
                    webTipPassedPortal = true;
                    movementDistance -= distanceToPortal;
                }
                else
                {
                    webTipPosition = Vector2.MoveTowards(
                        webTipPosition,
                        portalEntryPoint,
                        movementDistance
                    );
                    movementDistance = 0f;
                }
            }

            if (webTipPassedPortal || !swingUsesPortal)
            {
                webTipPosition = Vector2.MoveTowards(
                    webTipPosition,
                    targetSurfacePoint,
                    movementDistance
                );
            }

            UpdateWebVisuals(swingStartPosition, webTipPosition);

            if (Vector2.Distance(webTipPosition, targetSurfacePoint) < 0.001f)
            {
                webTipPosition = targetSurfacePoint;
                swingPhase = SwingPhase.PullingPlayer;
                pullStallElapsed = 0f;
                pullLastDistance = Vector2.Distance(rb.position, GetPullDestination());
                PlaySfx(webPullSfx, webPullSfxVolume);
                if (animator != null)
                {
                    animator.SetTrigger("PullWeb");
                }
            }

            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (swingPhase == SwingPhase.PullingPlayer)
        {
            Vector2 pullDestination = GetPullDestination();
            Vector2 nextPosition = Vector2.MoveTowards(
                rb.position,
                pullDestination,
                swingSpeed * Time.fixedDeltaTime
            );
            UpdateWebVisuals(rb.position, webTipPosition);
            rb.linearVelocity = Vector2.zero;

            Vector2 targetDelta = pullDestination - rb.position;
            float targetDistance = targetDelta.magnitude;
            if (targetDistance < pullLastDistance - SwingPullProgressThreshold)
            {
                pullLastDistance = targetDistance;
                pullStallElapsed = 0f;
            }
            else
            {
                pullStallElapsed += Time.fixedDeltaTime;
                if (pullStallElapsed >= SwingPullStallDuration)
                {
                    AbortBlockedWebPull();
                    return;
                }
            }

            if (swingUsesPortal &&
                !playerPassedPortal &&
                Vector2.Distance(nextPosition, portalEntryPoint) < 0.001f)
            {
                if (swingPortal == null || !swingPortal.IsReady)
                {
                    Debug.LogError($"Portal link became unavailable during Webi's swing.", this);
                    swingPhase = SwingPhase.RetractingWeb;
                    currentState = State.Walking;
                    rb.linearVelocity = Vector2.zero;
                    if (animator != null)
                    {
                        animator.SetTrigger("LandWeb");
                    }
                    UpdateWebVisuals(rb.position, webTipPosition);
                    return;
                }

                rb.position = swingPortal.TransformPointToLinkedPortal(nextPosition) +
                    portalExitDirection * Mathf.Max(swingPortal.LinkedPortal.exitOffset, 0f);
                rb.rotation = swingPortal.TransformRotationToLinkedPortal(rb.rotation);
                playerPassedPortal = true;
                pullLastDistance = Vector2.Distance(rb.position, targetPosition);
                pullStallElapsed = 0f;
                UpdateWebVisuals(rb.position, webTipPosition);
                return;
            }

            float movementDistance = Vector2.Distance(rb.position, nextPosition);
            bool touchingTargetPlatform = (!swingUsesPortal || playerPassedPortal) &&
                movementDistance > 0.000001f &&
                HasReachedTargetPlatform(
                    targetPosition - rb.position,
                    movementDistance + Mathf.Max(surfaceProbeDistance, 0.01f)
                );
            if (Vector2.Distance(nextPosition, targetPosition) < 0.001f || touchingTargetPlatform)
            {
                swingLandingStartPosition = rb.position;
                swingLandingStartRotation = rb.rotation;
                swingLandingElapsed = 0f;
                float landingDistance = Vector2.Distance(swingLandingStartPosition, targetPosition);
                float landingRotation = Mathf.Abs(
                    Mathf.DeltaAngle(swingLandingStartRotation, targetRotation)
                );
                swingLandingDuration = Mathf.Max(
                    SwingLandingDuration,
                    Mathf.Max(
                        landingDistance / Mathf.Max(swingSpeed, 0.01f),
                        landingRotation / Mathf.Max(swingSpeed * 10f, 0.01f)
                    )
                );
                swingPhase = SwingPhase.LandingPlayer;
                SetDebugStatus("Swing reached the platform; completing animated landing.");
            }
            else
            {
                rb.MovePosition(nextPosition);
                rb.MoveRotation(Mathf.MoveTowardsAngle(
                    rb.rotation,
                    targetRotation,
                    swingSpeed * 10f * Time.fixedDeltaTime
                ));
                UpdateWebVisuals(nextPosition, webTipPosition);
            }

            return;
        }

        if (swingPhase == SwingPhase.LandingPlayer)
        {
            swingLandingElapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(swingLandingElapsed / swingLandingDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            Vector2 landingPosition = Vector2.Lerp(
                swingLandingStartPosition,
                targetPosition,
                easedProgress
            );
            float landingRotation = Mathf.LerpAngle(
                swingLandingStartRotation,
                targetRotation,
                easedProgress
            );

            rb.MovePosition(landingPosition);
            rb.MoveRotation(landingRotation);
            rb.linearVelocity = Vector2.zero;
            UpdateWebVisuals(landingPosition, webTipPosition);

            if (progress >= 1f)
            {
                bool landingCorrectionNeeded = Vector2.Distance(rb.position, targetPosition) > 0.05f;
                bool landingCorrected = false;
                if (landingCorrectionNeeded && TrySnapToTargetSurface())
                {
                    rb.position = targetPosition;
                    rb.rotation = targetRotation;
                    UpdateWebVisuals(targetPosition, webTipPosition);
                    landingCorrected = true;
                }

                currentSurfaceNormal = targetSurfaceNormal;
                currentForward = GetForward(currentSurfaceNormal);
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                hasLandingContact = false;
                swingPhase = SwingPhase.RetractingWeb;
                currentState = State.Walking;
                if (animator != null)
                {
                    animator.SetTrigger("LandWeb");
                }
                string platformName = targetPlatformCollider != null
                    ? targetPlatformCollider.name
                    : "target surface";
                if (landingCorrected)
                {
                    SetDebugStatus(
                        $"Corrected landing onto the selected side of {platformName}; retracting web."
                    );
                }
                else if (landingCorrectionNeeded)
                {
                    SetDebugStatus(
                        $"Landing on {platformName} stopped short; safe correction was blocked by another platform."
                    );
                }
                else
                {
                    SetDebugStatus($"Landed on {platformName}; retracting web.");
                }
                currentSurfaceCollider = targetPlatformCollider;
            }
        }
    }

    Vector2 GetSurfaceLandingPosition(Vector2 anchorPoint, Vector2 surfaceNormal, float rotation)
    {
        // Dudukan dihitung dari bounds sprite, supaya kaki Webi yang menyentuh permukaan
        // (bukan collider yang lebih kecil dari gambar). Koreksi visual menutup selisih antara
        // permukaan collider dan gambar rumput.
        return anchorPoint + surfaceNormal.normalized * GetSurfaceOffset(surfaceNormal);
    }

    void AbortBlockedWebPull()
    {
        if (TrySnapToTargetSurface())
        {
            currentSurfaceNormal = targetSurfaceNormal;
            currentSurfaceCollider = targetPlatformCollider;
            currentForward = GetForward(currentSurfaceNormal);
            rb.position = targetPosition;
            rb.rotation = targetRotation;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            swingPhase = SwingPhase.RetractingWeb;
            currentState = State.Walking;
            if (animator != null)
            {
                animator.SetTrigger("LandWeb");
            }

            SetDebugStatus(
                $"Corrected blocked swing onto the selected side of {targetPlatformCollider.name}."
            );
            return;
        }

        swingPhase = SwingPhase.RetractingWeb;
        currentState = State.Walking;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        if (animator != null)
        {
            animator.SetTrigger("LandWeb");
        }

        SetDebugStatus(
            $"Swing cancelled: pull made no progress for {SwingPullStallDuration:F1}s; " +
            "the selected side could not be reached safely; restored walking control."
        );
    }

    bool TrySnapToTargetSurface()
    {
        if (targetPlatformCollider == null || !(playerCollider is CapsuleCollider2D capsule))
        {
            return false;
        }

        Vector2 snapPosition = GetSurfaceLandingPosition(
            targetSurfacePoint,
            targetSurfaceNormal,
            targetRotation
        );
        Vector2 snapDirection = snapPosition - rb.position;
        float snapDistance = snapDirection.magnitude;
        if (snapDistance > 0.001f)
        {
            Vector2 direction = snapDirection / snapDistance;
            int hitCount = playerCollider.Cast(
                direction,
                platformFilter,
                castHits,
                snapDistance
            );
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = castHits[i];
                if (hit.collider != null &&
                    hit.collider != targetPlatformCollider &&
                    hit.distance < snapDistance - 0.02f &&
                    Vector2.Dot(hit.normal, direction) < -0.01f)
                {
                    return false;
                }
            }
        }

        Vector2 capsuleSize = Vector2.Scale(
            capsule.size,
            new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
        );
        Vector2 scaledOffset = Vector2.Scale(capsule.offset, transform.lossyScale);
        Vector2 capsuleCenter = snapPosition +
            (Vector2)(Quaternion.Euler(0f, 0f, targetRotation) * scaledOffset);
        Collider2D[] overlaps = Physics2D.OverlapCapsuleAll(
            capsuleCenter,
            capsuleSize,
            capsule.direction,
            targetRotation,
            platformLayer
        );
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i] != null && overlaps[i] != targetPlatformCollider)
            {
                return false;
            }
        }

        targetPosition = snapPosition;
        return true;
    }

    void HandleWebRetraction()
    {
        webTipPosition = Vector2.MoveTowards(
            webTipPosition,
            rb.position,
            webRetractSpeed * Time.fixedDeltaTime
        );
        UpdateWebVisuals(rb.position, webTipPosition);
        rb.linearVelocity = Vector2.zero;

        if (Vector2.Distance(webTipPosition, rb.position) < 0.001f)
        {
            HideWebVisuals();
            swingPhase = SwingPhase.ShootingWeb;
            SetDebugStatus($"Swing complete at {rb.position}; normal {currentSurfaceNormal}.");
        }
    }

    bool HasReachedTargetPlatform(Vector2 targetDelta, float maxCastDistance)
    {
        if (targetPlatformCollider == null || targetDelta.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        int hitCount = playerCollider.Cast(
            targetDelta.normalized,
            platformFilter,
            castHits,
            maxCastDistance
        );

        RaycastHit2D closestHit = default;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider != null && castHits[i].distance < closestDistance)
            {
                closestHit = castHits[i];
                closestDistance = castHits[i].distance;
            }
        }

        return closestHit.collider == targetPlatformCollider;
    }

    void SetDebugStatus(string status)
    {
        debugStatus = status;
        if (debugMode)
        {
            Debug.Log($"PlayerController: {status}");
        }
    }

    void OnGUI()
    {
        if (!debugMode || rb == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10f, 10f, 520f, 210f), GUI.skin.box);
        GUILayout.Label($"State: {currentState} | Swing: {swingPhase}");
        GUILayout.Label($"Position: {rb.position} | Velocity: {rb.linearVelocity}");
        GUILayout.Label($"Target: {targetPosition} | Distance: {Vector2.Distance(rb.position, targetPosition):F2}");
        GUILayout.Label($"Web tip gap: {Vector2.Distance(webTipPosition, rb.position):F2}");
        GUILayout.Label($"Surface normal: {currentSurfaceNormal} | Gravity: {rb.gravityScale}");
        GUILayout.Label($"Support: {debugSupport} | Platform mask: {platformLayer.value}");
        GUILayout.Label($"Status: {debugStatus}");
        GUILayout.EndArea();
    }

    Vector2 GetForward(Vector2 surfaceNormal)
    {
        return new Vector2(surfaceNormal.y, -surfaceNormal.x).normalized;
    }

    /// <summary>
    /// Jarak dari origin Webi ke titik dudukannya di permukaan. Memakai bounds sprite (kaki visual),
    /// bukan bounds collider yang ukurannya beda, supaya Webi benar-benar menempel di platform.
    /// </summary>
    float GetSurfaceOffset(Vector2 surfaceNormal)
    {
        return GetVisualSupportOffset(surfaceNormal) + surfaceOffset;
    }

    /// <summary>Jarak origin ke kaki sprite di sepanjang -normal permukaan.</summary>
    float GetVisualSupportOffset(Vector2 surfaceNormal)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || playerCollider == null)
        {
            return GetColliderSupportOffset(surfaceNormal);
        }

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        float scaleX = Mathf.Abs(transform.lossyScale.x);
        float scaleY = Mathf.Abs(transform.lossyScale.y);

        // Sprite.bounds Unity memakai seluruh tekstur (termasuk area transparan), jadi tepi bawahnya
        // bukan kaki Webi. Padding transparan di bawah dibuang supaya yang menempel benar-benar kaki.
        // Padding dibaca per sprite karena tiap frame kanvasnya berbeda ukuran.
        float spriteHeight = spriteBounds.size.y * scaleY;
        float padding = GetSpriteFootPadding(spriteRenderer.sprite);
        float bottom = -(0.5f - Mathf.Clamp(padding, 0f, 0.45f)) * spriteHeight;
        float left = (spriteBounds.center.x - spriteBounds.extents.x) * scaleX;
        float right = (spriteBounds.center.x + spriteBounds.extents.x) * scaleX;

        // Webi diputar mengikuti permukaan, jadi kaki sprite jatuh tepat di sepanjang -normal.
        Quaternion rotation = Quaternion.Euler(0f, 0f, GetSurfaceAngle(surfaceNormal));
        Vector2 footLeft = rotation * new Vector3(left, bottom, 0f);
        Vector2 footRight = rotation * new Vector3(right, bottom, 0f);
        Vector2 normal = surfaceNormal.normalized;

        return Mathf.Max(-Vector2.Dot(footLeft, normal), -Vector2.Dot(footRight, normal));
    }

    /// <summary>Bagian bawah sprite yang transparan: nilai terukur per sprite, bukan satu konstanta.</summary>
    float GetSpriteFootPadding(Sprite sprite)
    {
        if (sprite != null && spriteFootPaddings.TryGetValue(sprite.name, out float padding))
        {
            return padding;
        }

        return spriteFootPadding;
    }

    /// <summary>Jarak origin ke titik terendah collider di sepanjang -normal permukaan.</summary>
    float GetColliderSupportOffset(Vector2 surfaceNormal)
    {
        Vector2 normal = surfaceNormal.normalized;
        Bounds colliderBounds = playerCollider.bounds;
        float probeDistance = Vector2.Distance(rb.position, colliderBounds.center) +
            colliderBounds.extents.magnitude + 1f;
        Vector2 probePoint = rb.position - normal * probeDistance;
        Vector2 supportPoint = playerCollider.ClosestPoint(probePoint);
        return Vector2.Dot(rb.position - supportPoint, normal);
    }

    /// <summary>
    /// Jarak yang diharapkan antara permukaan collider dan platform saat Webi sudah duduk pas.
    /// Dipakai untuk koreksi posisi, bukan untuk menempatkan Webi dari titik permukaan.
    /// </summary>
    float GetExpectedSupportGap(Vector2 surfaceNormal)
    {
        float visual = GetVisualSupportOffset(surfaceNormal);
        float collider = GetColliderSupportOffset(surfaceNormal);
        return Mathf.Max(0f, visual - collider) + surfaceOffset;
    }

    /// <summary>
    /// Saat berdiri tegak, kaki Webi dipaksa menempel di puncak sel tilemap. Collider lain
    /// (mound, spike, atau collider sisa) yang lebih tinggi dari gambar rumput tidak lagi
    /// membuat Webi tampak melayang di atas platform.
    /// </summary>
    Vector2 PinFeetToSurface(Vector2 position, Vector2 surfaceNormal)
    {
        if (surfaceNormal.normalized.y < 0.9f)
        {
            return position;
        }

        float feetY = position.y - GetVisualSupportOffset(surfaceNormal);
        float surfaceTop;
        if (!TryGetTilemapSurfaceTop(position.x, feetY + 0.5f, out surfaceTop))
        {
            return position;
        }

        // Hanya menurunkan Webi, supaya platform yang memang lebih tinggi tetap dihormati.
        float standingY = surfaceTop + GetVisualSupportOffset(surfaceNormal) + surfaceOffset;
        if (position.y > standingY + 0.02f)
        {
            position.y = standingY;
        }

        return position;
    }

    Tilemap FindGroundTilemap()
    {
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i] != null && tilemaps[i].name == "Tilemap_Ground")
            {
                return tilemaps[i];
            }
        }

        return null;
    }

    /// <summary>Puncak sel tilemap tertinggi di kolom worldX, mulai dari fromY ke bawah.</summary>
    bool TryGetTilemapSurfaceTop(float worldX, float fromY, out float surfaceTopY)
    {
        surfaceTopY = 0f;
        if (groundTilemap == null)
        {
            return false;
        }

        Vector3Int startCell = groundTilemap.WorldToCell(new Vector3(worldX, fromY, 0f));
        Vector3 cellSize = groundTilemap.cellSize;
        // Mulai dari sel tempat titik acuan berada lalu turun; jangan pernah mencari ke atas,
        // karena platform melayang di kolom yang sama bisa terbaca sebagai "tanah" Webi.
        for (int y = startCell.y; y >= startCell.y - 32; y--)
        {
            Vector3Int cell = new Vector3Int(startCell.x, y, startCell.z);
            if (!groundTilemap.HasTile(cell))
            {
                continue;
            }

            surfaceTopY = groundTilemap.CellToWorld(cell).y + cellSize.y;
            return true;
        }

        return false;
    }

    /// <summary>Dasar sel tilemap terdekat di atas worldX, untuk menempel di bawah platform.</summary>
    bool TryGetTilemapSurfaceBottom(float worldX, float fromY, out float surfaceBottomY)
    {
        surfaceBottomY = 0f;
        if (groundTilemap == null)
        {
            return false;
        }

        Vector3Int startCell = groundTilemap.WorldToCell(new Vector3(worldX, fromY, 0f));
        // Cari ke atas, bukan ke bawah: yang dipakai adalah sisi bawah sel pertama di atas Webi.
        for (int y = startCell.y; y <= startCell.y + 32; y++)
        {
            Vector3Int cell = new Vector3Int(startCell.x, y, startCell.z);
            if (!groundTilemap.HasTile(cell))
            {
                continue;
            }

            surfaceBottomY = groundTilemap.CellToWorld(cell).y;
            return true;
        }

        return false;
    }

    /// <summary>Lepaskan Webi dari dinding/tepi yang tidak punya pijakan supaya dia jatuh, bukan terkunci.</summary>
    #region Surface Hazards

    /// <summary>Jenis permukaan overlay yang sedang dipijak Webi.</summary>
    private enum SurfaceKind
    {
        Normal,
        Ice,
        Oil
    }

    /// <summary>
    /// Kecepatan maju Webi untuk satu langkah FixedUpdate. Di es kecepatannya tidak langsung
    /// mengikuti tombol: naik perlahan dan baru berhenti setelah beberapa saat, sehingga Webi
    /// tetap meluncur walau tombol sudah dilepas. Di permukaan biasa nilainya sama seperti dulu.
    /// </summary>
    float ResolveWalkSpeed(float moveInput, bool slippery)
    {
        if (!slippery)
        {
            slideSpeed = 0f;
            return moveInput * walkSpeed;
        }

        float deltaTime = Time.fixedDeltaTime;
        float targetSpeed = moveInput * walkSpeed * Mathf.Max(1f, iceTopSpeedMultiplier);
        bool reversing = Mathf.Abs(slideSpeed) > 0.01f &&
            Mathf.Sign(targetSpeed) != Mathf.Sign(slideSpeed);
        float rate = Mathf.Abs(targetSpeed) < 0.01f || reversing ? iceDeceleration : iceAcceleration;
        slideSpeed = Mathf.MoveTowards(slideSpeed, targetSpeed, Mathf.Max(0.1f, rate) * deltaTime);
        return slideSpeed;
    }

    /// <summary>
    /// Membaca tile overlay di bawah kaki Webi. Titiknya diambil sedikit masuk ke dalam
    /// permukaan, ditambah satu titik di sisi luar untuk jaga-jaga kalau tile overlay dilukis
    /// di sel sebelah. Sejak slope 26 derajat dipakai untuk es/oil, satu titik tidak cukup: di
    /// sambungan dua ramp titik masuk dari kaki jatuh ke sel kosong (ramp berikutnya naik satu
    /// baris), sehingga efek es hilang sesaat dan Webi berhenti. Karena itu dibaca juga dua titik
    /// di kiri/kanan sepanjang permukaan. Hasilnya dicocokkan dengan Tilemap_Surface_Oil dan
    /// Tilemap_Surface_Ice.
    /// </summary>
    SurfaceKind GetSurfaceKindAtFeet()
    {
        if (oilSurfaceTilemap == null && iceSurfaceTilemap == null)
        {
            return SurfaceKind.Normal;
        }

        Vector2 normal = currentSurfaceNormal.sqrMagnitude > 0.0001f
            ? currentSurfaceNormal.normalized
            : Vector2.up;
        Vector2 feetPoint = rb.position - currentSurfaceNormal * GetSurfaceOffset(currentSurfaceNormal);
        Vector2 tangent = new Vector2(-normal.y, normal.x);
        float depth = Mathf.Max(0f, surfaceSampleDepth);

        // Oil dicek lebih dulu supaya satu sel oil tetap menang atas es.
        if (HasSurfaceTileAround(oilSurfaceTilemap, feetPoint, tangent, normal, depth))
        {
            return SurfaceKind.Oil;
        }

        if (HasSurfaceTileAround(iceSurfaceTilemap, feetPoint, tangent, normal, depth))
        {
            return SurfaceKind.Ice;
        }

        return SurfaceKind.Normal;
    }

    /// <summary>
    /// Cek tile overlay di beberapa titik sekitar kaki: tepat di kaki, lalu digeser ke kiri dan
    /// kanan sepanjang permukaan, masing-masing juga dicoba sedikit masuk dan keluar permukaan.
    /// Titik samping inilah yang menjaga efek es/oil tetap menyala di sambungan ramp, karena di
    /// sana satu sel ramp bergeser satu baris terhadap pasangannya.
    /// </summary>
    static bool HasSurfaceTileAround(
        Tilemap tilemap,
        Vector2 feetPoint,
        Vector2 tangent,
        Vector2 normal,
        float depth)
    {
        if (tilemap == null)
        {
            return false;
        }

        for (int step = -1; step <= 1; step++)
        {
            Vector2 point = feetPoint + tangent * (SurfaceSampleSpan * step);
            if (HasSurfaceTile(tilemap, point) ||
                HasSurfaceTile(tilemap, point - normal * depth) ||
                HasSurfaceTile(tilemap, point + normal * depth))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasSurfaceTile(Tilemap tilemap, Vector2 worldPoint)
    {
        return tilemap != null && tilemap.HasTile(tilemap.WorldToCell(worldPoint));
    }

    Tilemap FindSurfaceTilemap(string tilemapName)
    {
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i] != null && tilemaps[i].name == tilemapName)
            {
                return tilemaps[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Webi menapak oil: lepas dari pijakan dan jatuh. Kalau di bawahnya ada ground, dia mendarat
    /// seperti jatuh biasa dan selamat. Kalau tidak ada ground, dia terus jatuh sampai keluar dari
    /// isi level lalu kalah lewat <see cref="IsOutsideLevelBounds"/>.
    /// </summary>
    void BeginOilFall()
    {
        Vector2 awayDirection = currentSurfaceNormal.sqrMagnitude > 0.0001f
            ? currentSurfaceNormal.normalized
            : Vector2.up;

        currentState = State.OilFalling;
        oilFallDirection = awayDirection;
        oilFallSpeed = Mathf.Max(0f, oilFallImpulse);
        hasLandingContact = false;
        supportLossReported = false;
        walkStepTimer = 0f;
        slideSpeed = 0f;

        if (rb != null)
        {
            // Gravitasi dunia dimatikan: arah "jatuh" ditentukan normal permukaan yang dipijak,
            // jadi oil di sisi atas ground melempar Webi ke atas, oil di dinding kanan ke kanan, dst.
            rb.gravityScale = 0f;
            rb.linearVelocity = awayDirection * oilFallSpeed;
            rb.angularVelocity = 0f;
        }

        SetDebugStatus($"Oil: Webi lost its footing, thrown toward {awayDirection}.");
    }

    /// <summary>
    /// Webi terlempar menjauhi permukaan karena oil: kecepatannya bertambah ke arah itu, bukan ke
    /// bawah dunia. Kalau di arah itu ada permukaan, dia mendarat dan selamat. Kalau tidak ada, dia
    /// keluar dari area level lalu kalah lewat pengecekan batas di FixedUpdate.
    /// </summary>
    void HandleOilFall()
    {
        oilFallSpeed = Mathf.Min(
            oilFallSpeed + Mathf.Max(0f, oilFallAcceleration) * Time.fixedDeltaTime,
            Mathf.Max(0.1f, oilFallMaxSpeed)
        );
        rb.linearVelocity = oilFallDirection * oilFallSpeed;

        int hitCount = playerCollider.Cast(
            oilFallDirection,
            platformFilter,
            castHits,
            Mathf.Max(surfaceProbeDistance, 0.1f)
        );

        RaycastHit2D landingHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider == null)
            {
                continue;
            }

            if (landingHit.collider == null || castHits[i].distance < landingHit.distance)
            {
                landingHit = castHits[i];
            }
        }

        if (landingHit.collider != null)
        {
            LandFromOilFall(landingHit);
        }
    }

    /// <summary>Mendarat di permukaan yang ditemui saat terlempar dari oil: Webi kembali berjalan.</summary>
    void LandFromOilFall(RaycastHit2D landingHit)
    {
        Vector2 surfaceNormal = landingHit.normal.normalized;
        if (surfaceNormal.sqrMagnitude < 0.0001f)
        {
            surfaceNormal = -oilFallDirection;
        }

        currentSurfaceNormal = surfaceNormal;
        currentForward = GetForward(surfaceNormal);
        currentSurfaceCollider = landingHit.collider;
        debugSupport = landingHit.collider.name;
        landingColliderName = landingHit.collider.name;
        hasLandingContact = false;
        supportLossReported = false;
        slideSpeed = 0f;

        rb.WakeUp();
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.MoveRotation(GetSurfaceAngle(surfaceNormal));
        rb.MovePosition(GetSurfaceLandingPosition(
            landingHit.point,
            surfaceNormal,
            GetSurfaceAngle(surfaceNormal)
        ));

        currentState = State.Walking;
        SetDebugStatus($"Landed on {landingHit.collider.name} after being thrown by oil.");
    }

    /// <summary>Sel overlay pada titik kaki Webi, dipakai untuk mengenali sel oil yang sedang dipijak.</summary>
    Vector3Int GetFeetSurfaceCell(Tilemap surfaceTilemap)
    {
        if (surfaceTilemap == null)
        {
            return default;
        }

        float depth = Mathf.Max(0f, surfaceSampleDepth);
        Vector2 feetPoint = rb.position - currentSurfaceNormal * GetSurfaceOffset(currentSurfaceNormal);
        return surfaceTilemap.WorldToCell(feetPoint - currentSurfaceNormal * depth);
    }

    /// <summary>Simpan gabungan kotak semua tilemap level, dipakai sebagai batas "di dalam peta".</summary>
    void CacheLevelContentBounds()
    {
        TilemapRenderer[] renderers = FindObjectsByType<TilemapRenderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        Bounds union = default;
        hasLevelContentBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            TilemapRenderer renderer = renderers[i];
            if (renderer == null || renderer.bounds.size == Vector3.zero)
            {
                continue;
            }

            if (!hasLevelContentBounds)
            {
                union = renderer.bounds;
                hasLevelContentBounds = true;
            }
            else
            {
                union.Encapsulate(renderer.bounds);
            }
        }

        levelContentBounds = union;
    }

    /// <summary>
    /// Webi sudah jatuh keluar dari area level, bukan sekadar keluar layar. Sisi atas sengaja tidak
    /// dipakai: Webi boleh mengayun melewati puncak level tanpa dianggap terjatuh.
    /// </summary>
    bool IsOutsideLevelBounds(bool includeTop)
    {
        if (!hasLevelContentBounds)
        {
            return false;
        }

        Bounds bounds = levelContentBounds;
        float margin = Mathf.Max(0f, mapExitMargin);
        Vector2 position = rb.position;

        bool outside = position.x < bounds.min.x - margin ||
            position.x > bounds.max.x + margin ||
            position.y < bounds.min.y - margin;

        if (!outside && includeTop)
        {
            outside = position.y > bounds.max.y + margin;
        }

        return outside;
    }
    #endregion

    void BeginFallOffSurface()
    {
        currentState = State.Falling;
        hasLandingContact = false;
        supportLossReported = false;
        walkStepTimer = 0f;

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        SetDebugStatus("No platform under Webi; falling.");
    }

    /// <summary>SFX langkah kaki, diputar sekali setiap Walk Step Interval.</summary>
    void PlayWalkStep(float deltaTime)
    {
        if (walkSfx == null)
        {
            return;
        }

        walkStepTimer -= deltaTime;
        if (walkStepTimer > 0f)
        {
            return;
        }

        walkStepTimer = Mathf.Max(0.05f, walkStepInterval);

        PlaySfx(walkSfx, walkSfxVolume);
    }

    /// <summary>
    /// Memutar satu SFX dengan volume khusus. Volume ini dikalikan dengan volume SFX global dari
    /// Settings, jadi slider dan tombol mute tetap berpengaruh.
    /// </summary>
    void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(clip, 1f, 1f, volumeScale);
    }

    void CancelAiming()
    {
        currentState = State.Walking;
        TouchControlsInput.ClearAimDirection();
        lineRenderer.enabled = false;
        portalLineRenderer.enabled = false;
        RestoreWebLineStyle();
        lineRenderer.enabled = false;
        portalLineRenderer.enabled = false;
    }

    #endregion
}