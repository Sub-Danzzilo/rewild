using System.Collections;
using UnityEngine;

/// <summary>
/// Intro kamera yang diputar setiap kali scene level dibuka: kamera mulai dekat ke tumpukkan
/// tanah, menahan pandangan di sana beberapa detik, lalu bergeser perlahan ke Webi, dan
/// akhirnya zoom out kembali ke framing kamera default.
///
/// Selama intro berjalan, kamera dikendalikan penuh oleh script ini (geser kamera oleh pemain
/// dan mode fokus menanam benih diabaikan lewat <see cref="CameraDragController.BeginScriptedControl"/>)
/// dan gerak Webi dikunci supaya pemain tidak bisa kabur sebelum intronya selesai. Setelah zoom out
/// selesai, HUD menampilkan hitung mundur 3-2-1 seperti saat Resume dari popup pause, dan Webi baru
/// bisa digerakkan setelah hitung mundur itu habis.
///
/// Kamera selalu dikurung di dalam area level, jadi zoom in ke tumpukkan tanah yang ada di tepi
/// level tidak memperlihatkan area kosong di luar gambar latar.
/// </summary>
[DisallowMultipleComponent]
public class LevelStartCameraIntro : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("CameraDragController di kamera utama. Kosongkan untuk dicari otomatis di objek ini atau di Main Camera.")]
    public CameraDragController cameraDragController;

    [Tooltip("Transform tumpukkan tanah yang jadi fokus pertama kamera. Kosongkan untuk memakai tumpukkan tanah " +
             "yang punya SeedPlantingController di level ini.")]
    public Transform dirtPile;

    [Tooltip("Transform Webi yang jadi fokus kedua kamera. Kosongkan untuk mencari PlayerController di level ini.")]
    public Transform player;

    [Tooltip("PlayerController Webi, dipakai untuk mengunci gerak selama intro. Kosongkan untuk mencari otomatis.")]
    public PlayerController playerController;

    [Tooltip("HUD level yang menampilkan hitung mundur. Kosongkan untuk dicari otomatis di scene.")]
    public LevelHudController levelHud;

    [Header("Playback")]
    [Tooltip("Putar intro otomatis saat scene level dimulai.")]
    public bool playOnStart = true;

    [Tooltip("Jeda sebelum intro dimulai (detik). Memberi waktu tilemap level selesai dibangun.")]
    [Min(0f)]
    public float startDelay = 0.15f;

    [Tooltip("Lama kamera bergerak dari tampilan awal ke tumpukkan tanah, dalam detik. 0 = langsung berada di tumpukkan tanah.")]
    [Min(0f)]
    public float zoomInDuration = 0.8f;

    [Tooltip("Lama kamera menahan pandangan di tumpukkan tanah sebelum pindah ke Webi, dalam detik.")]
    [Min(0f)]
    public float holdOnDirtDuration = 2.5f;

    [Tooltip("Lama kamera bergeser dari tumpukkan tanah ke Webi, dalam detik.")]
    [Min(0f)]
    public float moveToPlayerDuration = 1.5f;

    [Tooltip("Lama kamera zoom out dari Webi kembali ke tampilan kamera default, dalam detik.")]
    [Min(0f)]
    public float zoomOutDuration = 1.5f;

    [Header("Zoom")]
    [Tooltip("Ukuran ortografis kamera saat melihat tumpukkan tanah (makin kecil nilainya, makin dekat zoom-nya).")]
    [Min(0.1f)]
    public float dirtZoomSize = 5f;

    [Tooltip("Ukuran ortografis kamera saat kamera berhenti di Webi sebelum zoom out.")]
    [Min(0.1f)]
    public float playerZoomSize = 6f;

    [Tooltip("Geseran titik fokus kamera terhadap tumpukkan tanah (unit dunia). Naikkan Y kalau tumpukkan tanah " +
             "ingin terlihat sedikit lebih tinggi di layar.")]
    public Vector3 dirtFocusOffset = Vector3.zero;

    [Tooltip("Geseran titik fokus kamera terhadap Webi (unit dunia).")]
    public Vector3 playerFocusOffset = Vector3.zero;

    [Header("Player Control")]
    [Tooltip("Kunci gerak dan tembakan Webi sampai seluruh fase intro selesai.")]
    public bool lockPlayerControl = true;

    [Header("Start Countdown")]
    [Tooltip("Tampilkan hitung mundur 3-2-1 setelah kamera selesai zoom out (sama seperti hitung mundur " +
             "setelah menekan Resume di popup pause). Webi baru bisa digerakkan setelah angkanya habis.")]
    public bool playCountdown = true;

    [Tooltip("Lama hitung mundur sebelum Webi bisa digerakkan, dalam detik. 0 = tanpa hitung mundur.")]
    [Min(0f)]
    public float countdownDuration = 3f;

    [Header("World Freeze")]
    [Tooltip("Membekukan seluruh isi level selama intro kamera dan hitung mundur 3-2-1: musuh, " +
             "moving ground, animasi, partikel, dan efek lain berhenti total. Kamera intro tetap " +
             "bergerak karena memakai waktu nyata. Hitung mundur setelah Resume dari popup pause " +
             "dan setelah Retry juga membekukan level dengan cara yang sama.")]
    public bool freezeWorldDuringIntro = true;

    #endregion

    #region Runtime State

    private Coroutine introRoutine;
    private bool isRunning;
    private PlayerController lockedPlayer;
    private bool cameraControlStarted;

    // Diminta tombol Retry: intro kamera (zoom ke tumpukan tanah lalu ke Webi) dilewati sekali
    // pada pemuatan scene berikutnya. Static supaya nilainya bertahan saat scene dimuat ulang.
    private static bool skipNextIntro;

    // True kalau script ini yang sedang menahan Time.timeScale di 0, jadi hanya script ini yang
    // mengembalikannya ke 1 (jangan menimpa timeScale milik popup pause).
    private bool worldFrozen;

    // Jumlah intro yang sedang membekukan dunia. HUD membacanya supaya popup pause tidak
    // mengembalikan waktu ke normal selama intro masih berjalan.
    private static int frozenWorlds;

    // True kalau script ini yang membungkam SFX lewat AudioManager selama intro dan hitung mundur,
    // jadi hanya script ini yang melepasnya (termasuk saat intro dihentikan paksa lewat OnDisable).
    private bool sfxSuppressed;

    #endregion

    #region Unity Events

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (!playOnStart)
        {
            skipNextIntro = false;
            return;
        }

        if (skipNextIntro)
        {
            skipNextIntro = false;
            PlayRetryIntro();
            return;
        }

        PlayIntro();
    }

    private void OnDisable()
    {
        StopIntro();
        UnfreezeWorld();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Memulai intro kamera. Aman dipanggil berkali-kali: selama intro masih berjalan,
    /// pemanggilan berikutnya diabaikan.
    /// </summary>
    public void PlayIntro()
    {
        if (isRunning || !isActiveAndEnabled)
        {
            return;
        }

        ResolveReferences();
        if (cameraDragController == null || cameraDragController.TargetCamera == null)
        {
            Debug.LogWarning($"{nameof(LevelStartCameraIntro)} on {name} needs a CameraDragController.", this);
            return;
        }

        if (playerController == null && player == null)
        {
            Debug.LogWarning($"{nameof(LevelStartCameraIntro)} on {name} needs Webi to focus on.", this);
            return;
        }

        isRunning = true;

        // Tombol Retry disembunyikan selama intro dan hitung mundur 3-2-1.
        SetHudIntroActive(true);

        // Semua SFX dibungkam sejak intro mulai (zoom ke tumpukkan tanah) sampai hitung mundur
        // 3-2-1 habis, supaya dengung portal dan loop lain tidak menyeletuk di belakang intro.
        SuppressSfx();

        // Musik scene dimulai di sini, bukan saat scene dimuat, supaya tepat saat zoom ke tumpukan tanah mulai.
        StartSceneMusic();

        FreezeWorld();
        LockPlayerControl();
        introRoutine = StartCoroutine(IntroRoutine());
    }

    /// <summary>True selama ada intro kamera yang sedang membekukan isi level.</summary>
    public static bool IsWorldFrozen => frozenWorlds > 0;

    /// <summary>
    /// Minta intro kamera dilewati pada pemuatan scene berikutnya. Dipakai tombol Retry supaya level
    /// langsung tampil dengan framing kamera normal, tanpa fase zoom ke tumpukan tanah dan Webi.
    /// </summary>
    public static void RequestSkipNextIntro()
    {
        skipNextIntro = true;
    }

    #endregion

    #region Internal Helpers

    private void ResolveReferences()
    {
        if (cameraDragController == null)
        {
            cameraDragController = GetComponent<CameraDragController>();
        }

        if (cameraDragController == null && Camera.main != null)
        {
            cameraDragController = Camera.main.GetComponent<CameraDragController>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (player == null && playerController != null)
        {
            player = playerController.transform;
        }

        if (levelHud == null)
        {
            levelHud = FindFirstObjectByType<LevelHudController>();
        }

        if (dirtPile == null)
        {
            SeedPlantingController planter = FindFirstObjectByType<SeedPlantingController>();
            if (planter != null)
            {
                dirtPile = planter.soilRenderer != null ? planter.soilRenderer.transform : planter.transform;
            }
        }
    }

    /// <summary>
    /// Versi intro untuk Retry: kamera langsung di framing default (tanpa zoom ke tumpukan tanah dan
    /// Webi), lalu tetap menunggu hitung mundur sebelum Webi bisa digerakkan.
    /// </summary>
    private void PlayRetryIntro()
    {
        if (isRunning || !isActiveAndEnabled)
        {
            return;
        }

        ResolveReferences();
        if (cameraDragController == null || cameraDragController.TargetCamera == null)
        {
            return;
        }

        isRunning = true;

        // Tombol Retry tetap disembunyikan selama hitung mundur, sama seperti intro biasa.
        SetHudIntroActive(true);

        // Retry tetap punya hitung mundur 3-2-1, jadi SFX juga dibungkam selama fase itu.
        SuppressSfx();

        // Retry: scene yang sama, jadi playlist yang sedang berjalan dilanjutkan (tidak diulang dari awal).
        StartSceneMusic();

        FreezeWorld();
        LockPlayerControl();
        introRoutine = StartCoroutine(RetryIntroRoutine());
    }

    private IEnumerator RetryIntroRoutine()
    {
        // Tunggu satu frame supaya CameraDragController selesai menghitung batas level di Start.
        yield return null;

        cameraDragController.SnapToDefaultView();

        yield return PlayStartCountdown();

        FinishIntro();
    }

    private IEnumerator IntroRoutine()
    {
        // Tunggu satu frame supaya CameraDragController selesai menghitung batas level di Start
        // dan PlayerController selesai menempelkan Webi ke permukaan.
        yield return null;

        if (startDelay > 0f)
        {
            yield return WaitRealtime(startDelay);
        }

        Camera targetCamera = cameraDragController.TargetCamera;
        if (targetCamera == null)
        {
            FinishIntro();
            yield break;
        }

        cameraDragController.BeginScriptedControl();
        cameraControlStarted = true;

        // Fase 1: dari tampilan awal kamera, zoom in perlahan ke tumpukkan tanah.
        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        Vector3 pileView = cameraDragController.GetClampedViewPosition(
            GetDirtFocusPoint(),
            dirtZoomSize
        );
        yield return TweenView(startPosition, startSize, pileView, dirtZoomSize, zoomInDuration);

        // Fase 2: tahan pandangan di tumpukkan tanah beberapa detik.
        if (holdOnDirtDuration > 0f)
        {
            yield return WaitRealtime(holdOnDirtDuration);
        }

        // Fase 3: geser ke Webi dengan zoom yang sama seperti saat melihat tumpukkan tanah.
        Vector3 playerView = cameraDragController.GetClampedViewPosition(
            GetPlayerFocusPoint(),
            playerZoomSize
        );
        yield return TweenView(pileView, dirtZoomSize, playerView, playerZoomSize, moveToPlayerDuration);

        // Fase 4: zoom out kembali ke framing kamera default.
        Vector3 defaultView = cameraDragController.DefaultViewPosition;
        yield return TweenView(
            playerView,
            playerZoomSize,
            defaultView,
            cameraDragController.DefaultOrthographicSize,
            zoomOutDuration
        );

        cameraDragController.EndScriptedControl();
        cameraControlStarted = false;

        // Fase 5: hitung mundur 3-2-1 (tampilan sama dengan hitung mundur setelah Resume). Webi tetap
        // terkunci selama angka masih berjalan, baru dilepas di FinishIntro.
        yield return PlayStartCountdown();

        FinishIntro();
    }

    private IEnumerator PlayStartCountdown()
    {
        if (!playCountdown || countdownDuration <= 0f)
        {
            yield break;
        }

        if (levelHud == null)
        {
            levelHud = FindFirstObjectByType<LevelHudController>();
        }

        if (levelHud != null)
        {
            yield return levelHud.PlayCountdown(countdownDuration);
            yield break;
        }

        // Tanpa HUD di scene, tetap tunggu selama hitung mundur supaya Webi tidak bisa bergerak duluan.
        yield return WaitRealtime(countdownDuration);
    }

    /// <summary>
    /// Meminta AudioManager memutar playlist scene ini. Kalau scene yang sama sudah diputar
    /// (Retry), AudioManager melanjutkan track yang sedang berjalan, bukan memulainya dari awal.
    /// </summary>
    private void StartSceneMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySceneMusic(gameObject.scene.name);
        }
    }

    private Vector3 GetDirtFocusPoint()
    {
        return dirtPile != null ? dirtPile.position + dirtFocusOffset : cameraDragController.DefaultViewPosition;
    }

    private Vector3 GetPlayerFocusPoint()
    {
        return player != null ? player.position + playerFocusOffset : cameraDragController.DefaultViewPosition;
    }

    private IEnumerator TweenView(
        Vector3 fromPosition,
        float fromSize,
        Vector3 toPosition,
        float toSize,
        float duration
    )
    {
        if (duration <= 0f || fromSize <= 0f)
        {
            ApplyView(toPosition, toSize);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Selama popup pause terbuka, fase kamera berhenti juga; kamera tidak boleh bergerak
            // di belakang popup.
            if (levelHud != null && levelHud.IsPaused)
            {
                yield return null;
                continue;
            }

            // Waktu nyata: intro tetap berjalan walau Time.timeScale = 0 (isi level dibekukan).
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyView(
                Vector3.Lerp(fromPosition, toPosition, progress),
                Mathf.Lerp(fromSize, toSize, progress)
            );
            yield return null;
        }

        ApplyView(toPosition, toSize);
    }

    private void ApplyView(Vector3 worldPosition, float orthographicSize)
    {
        Camera targetCamera = cameraDragController.TargetCamera;
        if (targetCamera == null)
        {
            return;
        }

        float size = Mathf.Max(0.1f, orthographicSize);
        Vector3 clamped = cameraDragController.GetClampedViewPosition(worldPosition, size);
        Vector3 current = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(clamped.x, clamped.y, current.z);
        targetCamera.orthographicSize = size;
    }

    /// <summary>
    /// Membekukan seluruh isi level selama intro dan hitung mundur. Time.timeScale = 0
    /// menghentikan FixedUpdate (moving ground, musuh), Animator (animasi musuh), particle
    /// system, dan script yang memakai waktu bertingkat. Semua jeda dan animasi kamera di script
    /// ini memakai waktu nyata, jadi intro tetap terlihat berjalan.
    /// </summary>
    private void FreezeWorld()
    {
        if (!freezeWorldDuringIntro || worldFrozen)
        {
            return;
        }

        worldFrozen = true;
        frozenWorlds++;
        Time.timeScale = 0f;
    }

    /// <summary>Mengembalikan waktu normal, hanya kalau script ini yang membekukannya.</summary>
    private void UnfreezeWorld()
    {
        if (!worldFrozen)
        {
            return;
        }

        worldFrozen = false;
        frozenWorlds = Mathf.Max(0, frozenWorlds - 1);

        // Waktu dikembalikan lewat HUD kalau ada: kalau game sedang dijeda, intro yang selesai
        // tidak boleh mencairkan level di belakang popup pause.
        if (levelHud != null)
        {
            levelHud.RefreshTimeScale();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Membungkam semua SFX lewat AudioManager selama intro dan hitung mundur. Hanya SFX yang
    /// dibungkam; musik tetap memakai jalur playlist-nya sendiri.
    /// </summary>
    private void SuppressSfx()
    {
        if (sfxSuppressed)
        {
            return;
        }

        sfxSuppressed = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxSuppressed(true);
        }
    }

    /// <summary>Melepas bungkam SFX, hanya kalau script ini yang membungkamnya.</summary>
    private void ReleaseSfx()
    {
        if (!sfxSuppressed)
        {
            return;
        }

        sfxSuppressed = false;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxSuppressed(false);
        }
    }

    /// <summary>Memberi tahu HUD bahwa intro (dan hitung mundur 3-2-1) sedang berjalan.</summary>
    private void SetHudIntroActive(bool active)
    {
        if (levelHud == null)
        {
            levelHud = FindFirstObjectByType<LevelHudController>();
        }

        if (levelHud != null)
        {
            levelHud.SetIntroActive(active);
        }
    }

    /// <summary>
    /// Menunggu sekian detik waktu nyata, tetapi berhenti selama popup pause terbuka. Dipakai
    /// jeda intro yang tidak boleh berjalan di belakang popup.
    /// </summary>
    private IEnumerator WaitRealtime(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (levelHud != null && levelHud.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void LockPlayerControl()
    {
        if (!lockPlayerControl)
        {
            return;
        }

        lockedPlayer = playerController;
        if (lockedPlayer != null)
        {
            lockedPlayer.SetMovementLocked(true);
        }
    }

    private void FinishIntro()
    {
        isRunning = false;
        introRoutine = null;

        if (cameraControlStarted && cameraDragController != null)
        {
            cameraDragController.EndScriptedControl();
            cameraControlStarted = false;
        }

        if (lockedPlayer != null)
        {
            lockedPlayer.SetMovementLocked(false);
            lockedPlayer = null;
        }

        UnfreezeWorld();

        // Hitung mundur sudah lewat: SFX boleh berbunyi lagi (dengung portal, langkah musuh, dst).
        ReleaseSfx();

        // Fase intro selesai: tombol Retry muncul lagi.
        SetHudIntroActive(false);
    }

    private void StopIntro()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (!isRunning)
        {
            return;
        }

        FinishIntro();
    }

    #endregion
}
