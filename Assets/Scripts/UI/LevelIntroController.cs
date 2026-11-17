using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Layar intro yang diputar sebelum sebuah level dimainkan. Scene ini khusus untuk intro,
/// jadi Level 1 tidak langsung dibuka dari Level Selection: tombol Level 1 membuka scene
/// intro ini dulu, lalu intro memuat scene levelnya.
///
/// Alurnya: layar menggelap -> fade in -> video diputar -> fade out -> level dimuat.
/// Lama fade in dan fade out diatur di Inspector. Selama intro berjalan tombol Skip selalu
/// terlihat; menekannya tidak memotong fade out: layar menggelap dulu selama Fade Out Duration
/// sebelum scene level dimuat, jadi perpindahannya tidak terasa mendadak.
///
/// Musik menu dihentikan begitu intro mulai (video membawa audionya sendiri), lalu scene level
/// memutar playlist-nya sendiri seperti biasa.
///
/// Clip video dimuat dari folder Resources memakai Video Resource Path. Kalau clip tidak
/// ditemukan, layar memakai placeholder diam selama Placeholder Duration detik.
/// </summary>
[DisallowMultipleComponent]
public class LevelIntroController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Level Settings")]
    [Tooltip("Nama scene level yang dimuat setelah intro selesai atau di-skip. Harus terdaftar di Scenes In Build.")]
    [SerializeField] private string targetScene = "Level 1";

    [Header("Playback")]
    [Tooltip("Putar intro otomatis saat scene ini dimuat. Matikan hanya kalau intro mau dipicu manual dari script lain.")]
    [SerializeField] private bool playOnStart = true;

    [Header("Fade")]
    [Tooltip("Lama fade dari layar hitam ke video dalam detik. 0 = video langsung muncul tanpa fade in.")]
    [SerializeField, Min(0f)] private float fadeInDuration = 1f;

    [Tooltip("Lama fade dari video ke layar hitam sebelum level dimuat, dalam detik. 0 = langsung pindah tanpa fade out.")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 1.5f;

    [Header("Scene References")]
    [Tooltip("Kumpulan teks placeholder yang tampil selama video belum diisi. Boleh kosong.")]
    [SerializeField] private GameObject placeholderRoot;

    [Tooltip("Isian progress bar di bawah layar intro. Boleh kosong.")]
    [SerializeField] private Image progressFill;

    [Tooltip("Tombol Skip. Listener-nya dipasang dari kode supaya tidak perlu diisi manual di Inspector.")]
    [SerializeField] private Button skipButton;

    [Header("Video")]
    [Tooltip("VideoPlayer untuk intro. Boleh dikosongkan: script memakai VideoPlayer di objek ini, atau menambahkannya sendiri.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Lokasi clip video di dalam folder Resources, tanpa ekstensi file.")]
    [SerializeField] private string videoResourcePath = "Video/Level1Intro";

    [Tooltip("Induk permukaan video. Isi latar layar intro supaya video tampil di atas latar hitam " +
             "tetapi tetap di bawah tombol Skip. Kalau kosong, permukaan video dibuat di bawah objek ini.")]
    [SerializeField] private Transform videoSurfaceParent;

    [Header("Placeholder")]
    [Tooltip("Lama intro placeholder dalam detik, dipakai selama clip video belum tersedia.")]
    [SerializeField, Min(1f)] private float placeholderDuration = 6f;

    #endregion

    #region Runtime State

    // Waktu maksimal menunggu video siap. Kalau lewat, level tetap dibuka supaya game tidak nyangkut.
    private const float PrepareTimeoutSeconds = 20f;

    // Video dianggap macet kalau jamnya tidak maju selama selama ini.
    private const float StallTimeoutSeconds = 10f;

    private bool introRunning;
    private bool hasStartedLevel;
    private bool skipWired;

    private bool hasVideo;
    private bool playbackStarted;
    private bool videoFinished;
    private float videoDuration;

    private float prepareClock;
    private float placeholderClock;

    // Deteksi video macet: jam video sendiri tidak maju selama beberapa detik.
    private double lastPlayerTime;
    private float stalledClock;

    private float fadeClock;
    private bool fadingOut;
    private float fadeOutClock;
    private float fadeOutStartAlpha;

    private RawImage videoSurface;
    private AspectRatioFitter videoAspect;
    private Image fadeOverlay;

    #endregion

    #region Unity Events

    private void Start()
    {
        if (playOnStart)
        {
            BeginIntro();
        }
    }

    private void Update()
    {
        if (!introRunning || hasStartedLevel)
        {
            return;
        }

        // Delta dibatasi supaya frame berat setelah scene dimuat tidak melompati fade.
        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.25f);

        // 1. Fade out: layar menggelap dulu, baru level dibuka.
        if (fadingOut)
        {
            fadeOutClock += deltaTime;
            float fadeOutProgress = fadeOutDuration > 0f ? Mathf.Clamp01(fadeOutClock / fadeOutDuration) : 1f;
            SetFadeAlpha(Mathf.Lerp(fadeOutStartAlpha, 1f, fadeOutProgress));

            if (fadeOutClock >= fadeOutDuration)
            {
                StartLevel();
            }

            return;
        }

        // 2. Fade in dari layar hitam ke video.
        if (fadeClock < fadeInDuration)
        {
            fadeClock += deltaTime;
            SetFadeAlpha(1f - Mathf.Clamp01(fadeClock / fadeInDuration));
        }

        if (hasVideo)
        {
            UpdateVideoStage(deltaTime);
            return;
        }

        // 3. Tanpa video: layar placeholder diam selama Placeholder Duration detik.
        placeholderClock += deltaTime;
        UpdateProgress(placeholderClock / placeholderDuration);

        if (placeholderClock >= placeholderDuration)
        {
            BeginFadeOut();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Memulai intro. Aman dipanggil berkali-kali karena hanya yang pertama berjalan.</summary>
    public void BeginIntro()
    {
        if (introRunning || hasStartedLevel)
        {
            return;
        }

        introRunning = true;

        // Musik menu (mis. playlist Level Selection) dimatikan selama intro: video punya audionya
        // sendiri. Musik level dimulai lagi oleh LevelStartCameraIntro saat scene level dimuat.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        WireSkipButton();
        EnsureFadeOverlay();

        // Layar selalu mulai dari hitam supaya perpindahan dari menu tidak terasa mendadak.
        fadeClock = 0f;
        SetFadeAlpha(fadeInDuration > 0f ? 1f : 0f);

        hasVideo = TryStartVideo();

        if (placeholderRoot != null)
        {
            placeholderRoot.SetActive(!hasVideo);
        }

        videoDuration = hasVideo ? (float)videoPlayer.clip.length : 0f;
        UpdateProgress(0f);
    }

    /// <summary>
    /// Dipanggil tombol Skip: layar menggelap dulu memakai Fade Out Duration, lalu level dimuat.
    /// Kalau intro belum berjalan (misalnya Play On Start dimatikan), level langsung dimuat.
    /// </summary>
    public void SkipIntro()
    {
        if (!introRunning)
        {
            StartLevel();
            return;
        }

        BeginFadeOut();
    }

    /// <summary>Memuat scene level. Aman dipanggil berkali-kali karena hanya yang pertama dieksekusi.</summary>
    public void StartLevel()
    {
        if (hasStartedLevel)
        {
            return;
        }

        hasStartedLevel = true;
        introRunning = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("Level intro target scene name is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError($"Scene '{targetScene}' is missing from Scenes In Build, so the level cannot be opened.", this);
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    #endregion

    #region Internal Helpers

    private void UpdateVideoStage(float deltaTime)
    {
        if (!playbackStarted)
        {
            prepareClock += deltaTime;

            if (prepareClock >= PrepareTimeoutSeconds)
            {
                Debug.LogWarning("Video intro tidak selesai prepare, level dibuka tanpa menunggu video.", this);
                BeginFadeOut();
            }

            return;
        }

        if (videoSurface != null && videoPlayer.texture != null)
        {
            videoSurface.texture = videoPlayer.texture;
            videoSurface.enabled = true;
        }

        if (!videoFinished)
        {
            // Progres diambil dari jam VideoPlayer sendiri, bukan jam dinding. Kalau mesin sempat
            // lambat (misalnya baru selesai memuat scene), video tetap ditonton sampai frame akhir.
            double playerTime = videoPlayer.time;
            UpdateProgress(videoDuration > 0f ? (float)(playerTime / videoDuration) : 0f);

            // Jaring pengaman hanya untuk video yang benar-benar berhenti, bukan yang sekadar lambat.
            if (playerTime > lastPlayerTime)
            {
                lastPlayerTime = playerTime;
                stalledClock = 0f;
            }
            else
            {
                stalledClock += deltaTime;

                if (stalledClock >= StallTimeoutSeconds)
                {
                    Debug.LogWarning("Video intro berhenti sebelum selesai, level dibuka.", this);
                    videoFinished = true;
                }
            }
        }

        if (videoFinished)
        {
            BeginFadeOut();
        }
    }

    private void BeginFadeOut()
    {
        if (fadingOut || hasStartedLevel)
        {
            return;
        }

        fadingOut = true;
        fadeOutClock = 0f;

        // Mulai dari alpha layar saat ini supaya skip di tengah fade in tidak membuat layar
        // sempat terang lagi sebelum menggelap.
        fadeOutStartAlpha = fadeOverlay != null ? fadeOverlay.color.a : 0f;
    }

    // Menyiapkan dan memutar video kalau clip-nya ada. Mengembalikan false kalau masih harus pakai placeholder.
    private bool TryStartVideo()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // Clip dimuat dari Resources supaya video tidak perlu diseret manual ke Inspector.
        if (videoPlayer.clip == null && !string.IsNullOrEmpty(videoResourcePath))
        {
            videoPlayer.clip = Resources.Load<VideoClip>(videoResourcePath);
        }

        if (videoPlayer.clip == null)
        {
            Debug.LogWarning($"Video intro tidak ditemukan di Resources/{videoResourcePath}, layar memakai placeholder.", this);
            return false;
        }

        // Render mode APIOnly dipakai supaya tidak perlu Render Texture khusus; teksturnya
        // ditempel langsung ke RawImage yang dibuat dari kode.
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        // Jangan pernah membuang frame video demi mengejar jam dinding: kalau mesin lambat,
        // video boleh sedikit tertinggal, yang penting isinya utuh sampai frame terakhir.
        videoPlayer.skipOnDrop = false;

        // Suara video diputar langsung lewat AudioListener, tanpa perlu AudioSource tambahan.
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        EnsureVideoSurface();

        // Prepare dulu supaya tekstur, ukuran, dan track audio sudah terbaca saat frame pertama tampil.
        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.Prepare();

        return true;
    }

    private void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        if (hasStartedLevel)
        {
            return;
        }

        // Rasio asli video dipakai supaya gambar tidak gepeng di layar yang bukan 16:9.
        if (videoAspect != null && preparedPlayer.width > 0 && preparedPlayer.height > 0)
        {
            videoAspect.aspectRatio = (float)preparedPlayer.width / preparedPlayer.height;
        }

        if (preparedPlayer.audioTrackCount > 0)
        {
            preparedPlayer.controlledAudioTrackCount = 1;
            preparedPlayer.EnableAudioTrack(0, true);
            preparedPlayer.SetDirectAudioMute(0, false);
            preparedPlayer.SetDirectAudioVolume(0, 1f);
        }

        if (videoSurface != null)
        {
            videoSurface.texture = preparedPlayer.texture;
            videoSurface.enabled = true;
        }

        // Jam progres baru berjalan di sini, bukan saat scene dimuat, supaya waktu loading video
        // tidak ikut memotong durasi yang seharusnya ditonton.
        playbackStarted = true;
        lastPlayerTime = 0d;
        stalledClock = 0f;

        preparedPlayer.loopPointReached -= HandleVideoFinished;
        preparedPlayer.loopPointReached += HandleVideoFinished;
        preparedPlayer.Play();
    }

    private void HandleVideoFinished(VideoPlayer finishedPlayer)
    {
        videoFinished = true;
    }

    private RectTransform ResolveUiParent()
    {
        RectTransform parentRect = videoSurfaceParent as RectTransform;
        return parentRect != null ? parentRect : transform as RectTransform;
    }

    private void EnsureVideoSurface()
    {
        if (videoSurface != null)
        {
            return;
        }

        RectTransform parentRect = ResolveUiParent();
        if (parentRect == null)
        {
            Debug.LogError("Permukaan video butuh induk RectTransform (UI Canvas).", this);
            return;
        }

        GameObject surfaceObject = new GameObject("VideoSurface", typeof(RectTransform), typeof(RawImage));
        surfaceObject.layer = parentRect.gameObject.layer;

        RectTransform surfaceRect = (RectTransform)surfaceObject.transform;
        surfaceRect.SetParent(parentRect, false);
        surfaceRect.anchorMin = Vector2.zero;
        surfaceRect.anchorMax = Vector2.one;
        surfaceRect.offsetMin = Vector2.zero;
        surfaceRect.offsetMax = Vector2.zero;

        // Diletakkan paling belakang di dalam induknya supaya tombol Skip tetap di depan video.
        surfaceRect.SetSiblingIndex(0);

        videoSurface = surfaceObject.GetComponent<RawImage>();
        videoSurface.raycastTarget = false;
        videoSurface.enabled = false;

        // Video tidak diregangkan penuh; rasio aslinya dijaga supaya tidak gepeng di layar non 16:9.
        videoAspect = surfaceObject.AddComponent<AspectRatioFitter>();
        videoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        videoAspect.aspectRatio = 16f / 9f;
    }

    // Lapisan hitam penuh layar untuk fade. Tidak menangkap klik supaya tombol Skip tetap bisa ditekan.
    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            return;
        }

        RectTransform parentRect = ResolveUiParent();
        if (parentRect == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image));
        overlayObject.layer = parentRect.gameObject.layer;

        RectTransform overlayRect = (RectTransform)overlayObject.transform;
        overlayRect.SetParent(parentRect, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Paling depan supaya menggelapkan video sekaligus tombol Skip saat fade.
        overlayRect.SetAsLastSibling();

        fadeOverlay = overlayObject.GetComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
        {
            return;
        }

        Color color = fadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = color;
        fadeOverlay.enabled = color.a > 0.001f;
    }

    private void UpdateProgress(float normalizedValue)
    {
        if (progressFill == null)
        {
            return;
        }

        // Image di-set ke tipe Filled (Horizontal) di scene; nilai 0..1 mengisi dari kiri ke kanan.
        progressFill.fillAmount = Mathf.Clamp01(normalizedValue);
    }

    // Listener Skip dipasang dari kode supaya event OnClick di scene tidak perlu diisi manual.
    private void WireSkipButton()
    {
        if (skipWired || skipButton == null)
        {
            return;
        }

        skipWired = true;
        skipButton.onClick.AddListener(SkipIntro);
    }

    #endregion
}
