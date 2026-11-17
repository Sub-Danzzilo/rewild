using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SeedPlantingController : MonoBehaviour
{
    /// <summary>Dipancarkan setelah animasi pertumbuhan selesai. Dipakai UI untuk memunculkan popup menang.</summary>
    public static event System.Action<SeedPlantingController> PlantingCompleted;

    #region Inspector Fields

    [Header("References")]
    [Tooltip("Transform pemain yang akan membawa dan menanam benih.")]
    public Transform player;

    [Tooltip("PlayerController pemain, dipakai untuk mengecek apakah pemain sedang bisa berinteraksi.")]
    public PlayerController playerController;

    [Tooltip("CameraDragController kamera. Jika kosong, dicari otomatis dari Main Camera.")]
    public CameraDragController cameraDragController;

    [Header("Seed")]
    [Tooltip("Objek benih yang bisa diambil dan dibawa pemain.")]
    public GameObject seedObject;

    [Tooltip("Area trigger di sekitar benih untuk mendeteksi pemain yang mendekat.")]
    public BoxCollider2D seedPickupArea;

    [Header("Soil")]
    [Tooltip("SpriteRenderer tanah tempat benih ditanam.")]
    public SpriteRenderer soilRenderer;

    [Tooltip("Area trigger tanah untuk mendeteksi pemain yang siap menanam.")]
    public BoxCollider2D soilPlantingArea;

    [Header("Growth Animation")]
    [Tooltip("Animation Clip pertumbuhan tanaman yang diputar setelah benih ditanam.")]
    public AnimationClip growthAnimation;


    [Tooltip("Jeda sebelum frame pertama animasi pertumbuhan tampil, dalam detik.")]
    public float firstFrameDelay = 1f;

    [Tooltip("Jarak waktu antar frame animasi pertumbuhan, dalam detik.")]
    public float frameInterval = 0.3f;

    [Header("Camera Focus")]
    [Tooltip("Fokuskan kamera ke tumpukan tanah saat animasi tumbuh dimulai.")]
    public bool focusCameraOnGrowth = true;

    [Tooltip("Ukuran ortografis kamera saat fokus ke tumpukan tanah (makin kecil makin dekat / zoom in). " +
             "Isi 0 untuk memakai nilai Focus Orthographic Size di CameraDragController.")]
    public float growthFocusZoomSize = 6f;

    [Tooltip("Geseran titik fokus kamera terhadap tengah tumpukan tanah. Naikkan Y kalau tanaman " +
             "tumbuh di bagian atas tanah.")]
    public Vector3 growthFocusOffset = Vector3.zero;

    [Header("Player Visual")]
    [Tooltip("Transparansi Webi selama animasi tumbuh berlangsung (1 = tetap pekat). " +
             "Isi nilai kecil supaya Webi jadi samar dan tanaman yang tumbuh jadi fokus pandangan.")]
    [Range(0f, 1f)]
    public float playerAlphaDuringGrowth = 0.3f;

    [Tooltip("Kembalikan Webi jadi pekat lagi setelah animasi tumbuh selesai.")]
    public bool restorePlayerAlphaAfterGrowth = true;

    [Header("Seed Follow")]
    [Tooltip("Posisi benih relatif terhadap pemain saat sedang dibawa.")]
    public Vector3 seedFollowOffset = new Vector3(0f, 0.5f, 0f);

    [Tooltip("Waktu penghalusan gerakan benih saat mengikuti pemain. Makin kecil, makin responsif.")]
    public float seedFollowSmoothTime = 0.2f;

    [Tooltip("Kecepatan maksimum benih saat mengikuti pemain.")]
    public float seedFollowMaxSpeed = 20f;

    [Header("Seed Bob")]
    [Tooltip("Arah naik-turun benih saat masih tergeletak di tanah.")]
    public Vector3 seedBobDirection = Vector3.up;

    [Tooltip("Jarak naik-turun benih saat mengambang. Isi 0 untuk mematikan efek ini.")]
    public float seedBobAmplitude = 0.15f;

    [Tooltip("Kecepatan naik-turun benih (putaran per detik).")]
    public float seedBobFrequency = 1.5f;

    [Header("Rendering")]
    [Tooltip("Selisih sorting order benih saat dibawa, supaya tampil di depan pemain.")]
    public int carriedSeedSortingOrderOffset = 1;

    [Header("Audio")]
    [Tooltip("SFX saat Webi berhasil mengambil benih.")]
    public AudioClip pickupSfx;

    [Tooltip("Volume SFX ambil benih (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float pickupSfxVolume = 1f;

    [Tooltip("SFX saat animasi tumbuh mulai tampil di tumpukkan tanah.")]
    public AudioClip growthSfx;

    [Tooltip("Volume SFX pertumbuhan tanaman (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float growthSfxVolume = 0.7f;

    #endregion

    private SpriteRenderer seedRenderer;
    private Collider2D playerCollider;
    private SpriteRenderer playerSpriteRenderer;
    private Animator playerAnimator;
    private bool cameraFocusActive;
    private Vector3 seedStartPosition;
    private Vector3 seedFollowVelocity;
    private bool isCarryingSeed;
    private bool seedIsPlanted;
    private bool isGrowing;
    private bool isComplete;
    private bool movementWasEnabled;
    private bool soilRendererWasEnabled;
    private float playerOriginalAlpha = 1f;
    private bool hasValidSetup;
    private int seedOriginalSortingLayerId;
    private int seedOriginalSortingOrder;
    private bool hasSeedSortingState;
    private PlayableGraph growthGraph;
    private GameObject growthVisualObject;
    private SpriteRenderer growthRenderer;

    // Acuan gambar dari file art (diukur dari PNG, bukan diisi manual di Inspector). Angkanya adalah
    // tepi gambar tanah di dalam canvas sprite-nya, 0..1 dari kiri / dari bawah.
    // prop_seed_grow_*.png (canvas 738x738): x 42..706 px, y 468..640 px dari atas. Kotaknya didapat
    // dengan mencocokkan bentuk gambar tanah kosong ke tiap frame (8 frame, kecocokan ±97%), jadi
    // hanya berisi tanahnya saja, tanpa batang pohon.
    private const float GrowthDirtLeftFraction = 0.056911f;
    private const float GrowthDirtRightFraction = 0.956640f;
    private const float GrowthDirtBottomFraction = 0.132791f;
    private const float GrowthDirtTopFraction = 0.365854f;
    // env_dirt.png (canvas 2048x2048): x 49..2004 px, y 904..1405 px dari atas.
    private const float SoilArtLeftFraction = 0.023926f;
    private const float SoilArtRightFraction = 0.978516f;
    private const float SoilArtBottomFraction = 0.313965f;
    private const float SoilArtTopFraction = 0.558594f;

    void OnValidate()
    {
        if (seedPickupArea == null && seedObject != null)
        {
            seedPickupArea = seedObject.GetComponent<BoxCollider2D>();
        }

        if (soilPlantingArea == null)
        {
            soilPlantingArea = GetComponent<BoxCollider2D>();
        }
    }

    void Awake()
    {
        if (soilRenderer == null)
        {
            soilRenderer = GetComponent<SpriteRenderer>();
        }

        if (soilRenderer != null)
        {
            soilRendererWasEnabled = soilRenderer.enabled;
        }

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (cameraDragController == null && Camera.main != null)
        {
            cameraDragController = Camera.main.GetComponent<CameraDragController>();
        }

        if (seedObject != null)
        {
            seedStartPosition = seedObject.transform.position;
            seedRenderer = seedObject.GetComponentInChildren<SpriteRenderer>();
            if (seedPickupArea == null)
            {
                seedPickupArea = seedObject.GetComponent<BoxCollider2D>();
            }

            if (seedRenderer != null)
            {
                seedOriginalSortingLayerId = seedRenderer.sortingLayerID;
                seedOriginalSortingOrder = seedRenderer.sortingOrder;
                hasSeedSortingState = true;
            }
        }

        if (soilPlantingArea == null)
        {
            soilPlantingArea = GetComponent<BoxCollider2D>();
        }

        hasValidSetup = ValidateSetup();
        if (!hasValidSetup)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (!hasValidSetup || isGrowing || isComplete)
        {
            return;
        }

        if (!isCarryingSeed)
        {
            if (playerController.CanPickUpItems &&
                seedObject.activeSelf &&
                IsPlayerTouchingSeed())
            {
                PickUpSeed();
            }
        }

        if (isCarryingSeed)
        {
            Vector3 followTarget = player.TransformPoint(seedFollowOffset);
            seedObject.transform.position = Vector3.SmoothDamp(
                seedObject.transform.position,
                followTarget,
                ref seedFollowVelocity,
                seedFollowSmoothTime,
                seedFollowMaxSpeed,
                Time.deltaTime
            );
        }
        else if (!seedIsPlanted && seedObject.activeSelf)
        {
            float bobOffset = Mathf.Sin(Time.time * seedBobFrequency * Mathf.PI * 2f) * seedBobAmplitude;
            seedObject.transform.position = seedStartPosition + seedBobDirection.normalized * bobOffset;
        }

        if (!isCarryingSeed)
        {
            return;
        }

        if (playerController.CanInteract &&
            IsPlayerInsideArea(soilPlantingArea))
        {
            StartCoroutine(PlantAndGrow());
        }
    }

    void OnDisable()
    {
        ReleaseCameraFocus();
        RestorePlayerMovement();
        StopGrowthGraph();
        RestoreRenderers();
        if (!seedIsPlanted)
        {
            RestoreSeedSorting();
        }
    }

    void OnDestroy()
    {
        StopGrowthGraph();
    }

    void OnDrawGizmosSelected()
    {
        SpriteRenderer rendererToUse = soilRenderer != null ? soilRenderer : GetComponent<SpriteRenderer>();
        if (rendererToUse == null)
        {
            return;
        }

        if (rendererToUse.sprite == null)
        {
            return;
        }

        // Menggambar kotak gambar tanah pile (acuan yang dipakai FitGrowthToSoil) supaya gampang
        // dilihat di Scene view dan ikut kalau pile diputar atau diskala.
        Vector2 spriteSize = rendererToUse.sprite.bounds.size;
        Vector2 drawSize = rendererToUse.drawMode == SpriteDrawMode.Simple ? spriteSize : rendererToUse.size;
        float left = (SoilArtLeftFraction - 0.5f) * drawSize.x;
        float right = (SoilArtRightFraction - 0.5f) * drawSize.x;
        float bottom = (SoilArtBottomFraction - 0.5f) * drawSize.y;
        float top = (SoilArtTopFraction - 0.5f) * drawSize.y;

        Transform soilTransform = rendererToUse.transform;
        Vector3 bottomLeft = soilTransform.TransformPoint(new Vector3(left, bottom, 0f));
        Vector3 bottomRight = soilTransform.TransformPoint(new Vector3(right, bottom, 0f));
        Vector3 topRight = soilTransform.TransformPoint(new Vector3(right, top, 0f));
        Vector3 topLeft = soilTransform.TransformPoint(new Vector3(left, top, 0f));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (player == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a Player Transform.", this);
            valid = false;
        }

        if (playerController == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a PlayerController.", this);
            valid = false;
        }

        if (seedObject == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a seed GameObject.", this);
            valid = false;
        }

        if (seedPickupArea == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a BoxCollider2D pickup area on the seed.", this);
            valid = false;
        }
        else if (!seedPickupArea.isTrigger)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} requires the seed pickup area to be a trigger.", seedPickupArea);
            valid = false;
        }

        if (soilRenderer == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a soil SpriteRenderer.", this);
            valid = false;
        }

        if (soilPlantingArea == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a BoxCollider2D planting area.", this);
            valid = false;
        }
        else if (!soilPlantingArea.isTrigger)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} requires the soil planting area to be a trigger.", soilPlantingArea);
            valid = false;
        }

        if (growthAnimation == null)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs a growth Animation Clip.", this);
            valid = false;
        }
        if (firstFrameDelay < 0f || frameInterval <= 0f)
        {
            Debug.LogError(
                $"{nameof(SeedPlantingController)} on {name} requires a non-negative first-frame delay " +
                "and a positive frame interval.",
                this
            );
            valid = false;
        }

        if (seedBobAmplitude < 0f ||
            seedBobFrequency < 0f ||
            seedBobDirection.sqrMagnitude <= 0.0001f ||
            seedFollowSmoothTime <= 0f ||
            seedFollowMaxSpeed <= 0f)
        {
            Debug.LogError(
                $"{nameof(SeedPlantingController)} on {name} requires a non-negative bob amplitude/frequency " +
                "and a non-zero bob direction, plus positive follow smooth time and max speed.",
                this
            );
            valid = false;
        }

        if (growthAnimation != null && growthAnimation.frameRate <= 0f)
        {
            Debug.LogError($"{nameof(SeedPlantingController)} on {name} needs an animation with a positive frame rate.", this);
            valid = false;
        }

        return valid;
    }

    bool IsPlayerInsideArea(BoxCollider2D interactionArea)
    {
        return interactionArea != null &&
            interactionArea.enabled &&
            player != null &&
            interactionArea.OverlapPoint(player.position);
    }

    /// <summary>
    /// Benih diambil kalau titik pemain masuk area benih ATAU badan pemain menyentuh area itu.
    /// Badan pemain jauh lebih lebar daripada titik root-nya, jadi sentuhan badan juga dihitung
    /// supaya berjalan di samping benih tetap mengambilnya.
    /// </summary>
    bool IsPlayerTouchingSeed()
    {
        if (IsPlayerInsideArea(seedPickupArea))
        {
            return true;
        }

        if (seedPickupArea == null || !seedPickupArea.enabled)
        {
            return false;
        }

        if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        return seedPickupArea.Distance(playerCollider).isOverlapped;
    }

    IEnumerator PlantAndGrow()
    {
        isGrowing = true;
        if (focusCameraOnGrowth && cameraDragController != null)
        {
            Vector3 focusPoint = soilRenderer.bounds.center + growthFocusOffset;
            cameraDragController.FocusOn(
                focusPoint,
                growthFocusZoomSize > 0f ? growthFocusZoomSize : (float?)null
            );
            cameraFocusActive = true;
        }

        movementWasEnabled = playerController.enabled;
        playerController.enabled = false;

        // Webi dibuat samar selama animasi tumbuh, supaya tanaman yang tumbuh jadi fokus pandangan.
        playerOriginalAlpha = playerSpriteRenderer != null ? playerSpriteRenderer.color.a : 1f;
        ApplyPlayerAlpha(playerAlphaDuringGrowth);

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsWalking", false);
            playerAnimator.Play("PlayerIdle", 0, 0f);
            playerAnimator.Update(0f);
        }

        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        seedObject.SetActive(false);
        seedIsPlanted = true;
        // Tumpukan tanah asli disembunyikan selama fase tumbuh: tiap frame animasi sudah membawa
        // gambar tanahnya sendiri (lihat konstanta GrowthDirt*Fraction), jadi kalau keduanya tampil
        // gambarnya terlihat dobel. Ukuran dan posisi tanah di frame animasi dipas-paskan ke kotak
        // tanah pile oleh FitGrowthToSoil(), jadi pergantiannya tidak meloncat.
        soilRendererWasEnabled = soilRenderer.enabled;
        soilRenderer.enabled = false;
        // Visual dibuat sebagai anak tumpukan tanah: posisi, skala, dan rotasinya otomatis mengikuti
        // pile (pile diputar 90° → tanaman ikut miring 90°), lalu ukuran dan posisinya dipas-paskan
        // ke gambar tanahnya sendiri oleh FitGrowthToSoil.
        growthVisualObject = new GameObject("Growth Animation");
        growthVisualObject.transform.SetParent(soilRenderer.transform, false);
        growthVisualObject.transform.localPosition = Vector3.zero;
        growthVisualObject.transform.localRotation = Quaternion.identity;
        growthVisualObject.transform.localScale = Vector3.one;
        growthRenderer = growthVisualObject.AddComponent<SpriteRenderer>();
        growthRenderer.sharedMaterial = soilRenderer.sharedMaterial;
        growthRenderer.color = soilRenderer.color;
        growthRenderer.sortingLayerID = soilRenderer.sortingLayerID;
        // Tanah pile sudah disembunyikan, jadi visual tumbuh memakai sorting order yang sama
        // dengan pile: tampil persis di tempat tanah itu berada.
        growthRenderer.sortingOrder = soilRenderer.sortingOrder;

        Animator growthAnimator = growthVisualObject.AddComponent<Animator>();
        growthGraph = PlayableGraph.Create($"{name} Seed Growth");
        growthGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(growthGraph, growthAnimation);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetDuration(growthAnimation.length);
        clipPlayable.SetSpeed(0d);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            growthGraph,
            "Seed Growth",
            growthAnimator
        );
        output.SetSourcePlayable(clipPlayable);
        growthGraph.Play();
        growthGraph.Evaluate(0f);
        FitGrowthToSoil();

        yield return new WaitForSeconds(firstFrameDelay);

        // Suara diputar tepat saat frame tumbuh pertama kali tampil, bukan saat benih ditanam:
        // jeda First Frame Delay hanya menunggu dan tidak terlihat oleh pemain.
        if (growthSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(growthSfx, 1f, 1f, growthSfxVolume);
        }

        float playbackSpeed = 1f / (growthAnimation.frameRate * frameInterval);
        clipPlayable.SetSpeed(playbackSpeed);
        float playbackDuration = growthAnimation.length / playbackSpeed;
        float elapsed = 0f;
        while (elapsed < playbackDuration)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, playbackDuration - elapsed);
            growthGraph.Evaluate(deltaTime);
            elapsed += deltaTime;
            yield return null;
        }

        if (growthGraph.IsValid())
        {
            clipPlayable.SetTime(growthAnimation.length);
            growthGraph.Evaluate(0f);
        }

        Sprite finalSprite = growthRenderer.sprite;
        Vector3 finalPosition = growthVisualObject.transform.localPosition;
        Quaternion finalRotation = growthVisualObject.transform.localRotation;
        Vector3 finalScale = growthVisualObject.transform.localScale;
        isComplete = true;
        StopGrowthGraph();
        growthRenderer.sprite = finalSprite;
        growthVisualObject.transform.localPosition = finalPosition;
        growthVisualObject.transform.localRotation = finalRotation;
        growthVisualObject.transform.localScale = finalScale;
        if (restorePlayerAlphaAfterGrowth)
        {
            ApplyPlayerAlpha(playerOriginalAlpha);
        }

        ReleaseCameraFocus();
        RestorePlayerMovement();
        PlantingCompleted?.Invoke(this);
    }

    void ReleaseCameraFocus()
    {
        if (!cameraFocusActive || cameraDragController == null)
        {
            return;
        }

        cameraDragController.ReleaseFocus();
        cameraFocusActive = false;
    }

    void PickUpSeed()
    {
        isCarryingSeed = true;
        seedFollowVelocity = Vector3.zero;

        if (pickupSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(pickupSfx, 1f, 1f, pickupSfxVolume);
        }

        if (seedRenderer != null && playerSpriteRenderer != null)
        {
            seedRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            seedRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + carriedSeedSortingOrderOffset;
        }
    }

    void RestoreSeedSorting()
    {
        if (seedRenderer != null && hasSeedSortingState)
        {
            seedRenderer.sortingLayerID = seedOriginalSortingLayerId;
            seedRenderer.sortingOrder = seedOriginalSortingOrder;
        }
    }

    /// <summary>
    /// Menyesuaikan ukuran dan posisi visual tumbuh dengan tumpukkan tanah yang dipakai. Semua hitungan
    /// dilakukan di ruang LOKAL pile, karena visualnya memang anak pile: begitu posisi, skala, atau
    /// rotasi pile diubah, animasinya ikut persis — tanah di animasi selalu menimpa tanah kosong dan
    /// tanaman tumbuh mengikuti arah tanah (pile diputar 90° → tanaman ikut miring 90°). Tidak ada
    /// angka yang perlu dicocokkan manual di Inspector.
    /// </summary>
    void FitGrowthToSoil()
    {
        if (growthRenderer == null || growthVisualObject == null)
        {
            return;
        }

        Sprite soilSprite = soilRenderer != null ? soilRenderer.sprite : null;
        Sprite frameSprite = growthRenderer.sprite;
        if (soilSprite == null || frameSprite == null)
        {
            return;
        }

        Vector2 soilSpriteSize = soilSprite.bounds.size;
        Vector2 soilDrawSize = soilRenderer.drawMode == SpriteDrawMode.Simple
            ? soilSpriteSize
            : soilRenderer.size;

        // Kotak gambar tanah milik pile di ruang lokalnya, dihitung dari area bergambar sprite-nya
        // (bukan seluruh canvas yang masih banyak transparan) dan mengikuti Draw Size renderer.
        float soilLeft = (SoilArtLeftFraction - 0.5f) * soilDrawSize.x;
        float soilRight = (SoilArtRightFraction - 0.5f) * soilDrawSize.x;
        float soilBottom = (SoilArtBottomFraction - 0.5f) * soilDrawSize.y;
        float soilTop = (SoilArtTopFraction - 0.5f) * soilDrawSize.y;

        // Kotak gambar tanah di frame animasi, di ruang lokal sprite frame itu sendiri.
        Vector2 frameSize = frameSprite.bounds.size;
        float frameLeft = (GrowthDirtLeftFraction - 0.5f) * frameSize.x;
        float frameRight = (GrowthDirtRightFraction - 0.5f) * frameSize.x;
        float frameBottom = (GrowthDirtBottomFraction - 0.5f) * frameSize.y;
        float frameTop = (GrowthDirtTopFraction - 0.5f) * frameSize.y;

        float soilWidth = soilRight - soilLeft;
        float soilHeight = soilTop - soilBottom;
        float frameWidth = frameRight - frameLeft;
        float frameHeight = frameTop - frameBottom;
        if (soilWidth <= 0.0001f || soilHeight <= 0.0001f || frameWidth <= 0.0001f || frameHeight <= 0.0001f)
        {
            return;
        }

        // Skala seragam: ambil yang paling kecil supaya gambar tanah di animasi selalu muat di dalam
        // gambar tanah pile (tersembunyi di belakangnya) dan proporsi tanamannya tetap utuh.
        float scale = Mathf.Min(soilWidth / frameWidth, soilHeight / frameHeight);
        float scaleX = scale;
        float scaleY = scale;

        // Posisi lokal dipilih supaya titik tengah tanah di animasi duduk tepat di titik tengah tanah
        // pile, jadi gambarnya saling menimpa tanpa perlu digeser-geser.
        float soilCenterX = (soilLeft + soilRight) * 0.5f;
        float soilCenterY = (soilBottom + soilTop) * 0.5f;
        float frameCenterX = (frameLeft + frameRight) * 0.5f;
        float frameCenterY = (frameBottom + frameTop) * 0.5f;

        growthVisualObject.transform.localPosition = new Vector3(
            soilCenterX - frameCenterX * scaleX,
            soilCenterY - frameCenterY * scaleY,
            0f
        );
        growthVisualObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    void RestorePlayerMovement()
    {
        if (!isGrowing)
        {
            return;
        }

        isGrowing = false;
        if (playerController != null)
        {
            playerController.enabled = movementWasEnabled;
        }
    }

    void StopGrowthGraph()
    {
        if (growthGraph.IsValid())
        {
            growthGraph.Destroy();
        }
    }

    void ApplyPlayerAlpha(float alpha)
    {
        if (playerSpriteRenderer == null)
        {
            return;
        }

        Color color = playerSpriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        playerSpriteRenderer.color = color;
    }

    void RestoreRenderers()
    {
        // Kalau alur menanam terhenti sebelum selesai (mis. objek dimatikan di tengah animasi),
        // kembalikan Webi pekat lagi supaya tidak tertinggal transparan.
        ApplyPlayerAlpha(playerOriginalAlpha);

        if (isComplete)
        {
            return;
        }

        if (soilRenderer != null)
        {
            soilRenderer.enabled = soilRendererWasEnabled;
        }

        if (growthVisualObject != null)
        {
            Destroy(growthVisualObject);
            growthVisualObject = null;
            growthRenderer = null;
        }
    }
}
