using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD level: penghitung moves, popup pause dengan hitung mundur saat resume, dan popup
/// out-of-moves yang bisa mengulang level atau kembali ke Level Selection.
///
/// Script ini tidak mengatur gameplay sama sekali. Gameplay cukup memanggil ConsumeMove()
/// setiap kali Webi menembakkan jaring ke platform lain, lalu popup out-of-moves muncul
/// otomatis saat moves habis.
/// </summary>
[DisallowMultipleComponent]
public class LevelHudController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Moves")]
    [Tooltip("Jumlah moves saat level dimulai.")]
    [SerializeField, Min(1)] private int maxMoves = 5;

    [Tooltip("Label penghitung moves di HUD.")]
    [SerializeField] private TMP_Text movesLabel;

    [Tooltip("Format label moves. {0} diganti sisa moves.")]
    [SerializeField] private string movesFormat = "Moves: {0}";

    [Header("HUD Buttons")]
    [Tooltip("Tombol retry di HUD. Disembunyikan selama intro kamera dan hitung mundur 3-2-1, " +
             "supaya level tidak bisa diulang di tengah fase itu. Kosongkan untuk dicari otomatis " +
             "sebagai anak HUD bernama \"RetryButton\".")]
    [SerializeField] private GameObject retryButton;

    [Header("Pause")]
    [Tooltip("Popup pause berisi tombol Resume dan Level Selection.")]
    [SerializeField] private UIPopupSlide pausePopup;

    [Tooltip("Objek hitung mundur di tengah layar. Hanya aktif selama hitung mundur resume.")]
    [SerializeField] private GameObject resumeCountdownRoot;

    [Tooltip("Label angka hitung mundur.")]
    [SerializeField] private TMP_Text resumeCountdownLabel;

    [Tooltip("Lama hitung mundur setelah Resume sebelum gameplay lanjut, dalam detik. 0 = langsung lanjut.")]
    [SerializeField, Min(0f)] private float resumeCountdownSeconds = 3f;

    [Header("Game Over")]
    [Tooltip("Popup game over: dipakai saat Webi mati (hazard, musuh) maupun saat moves habis.")]
    [SerializeField] private UIPopupSlide deathPopup;

    [Tooltip("Teks judul di dalam DeathPopup. Isinya diganti otomatis sesuai penyebab game over.")]
    [SerializeField] private TMP_Text deathTitleLabel;

    [Tooltip("Judul saat Webi mati karena musuh atau hazard.")]
    [SerializeField] private string enemyDeathTitle = "You Died!";

    [Tooltip("Judul saat moves habis.")]
    [SerializeField] private string outOfMovesTitle = "You've Run Out of Moves!";

    [Tooltip("Judul saat Webi terperosok karena menginjak oil.")]
    [SerializeField] private string fallDeathTitle = "You Fell!";

    [Header("Level")]
    [Tooltip("Nomor level ini. Dipakai untuk menyimpan bintang ke Level Selection.")]
    [SerializeField, Min(1)] private int levelNumber = 1;

    [Header("Win")]
    [Tooltip("Popup yang muncul setelah benih ditanam dan animasi tumbuh selesai.")]
    [SerializeField] private LevelCompletePopup winPopup;

    [Header("Scenes")]
    [Tooltip("Scene yang dibuka tombol Level Selection. Harus terdaftar di Scenes In Build.")]
    [SerializeField] private string levelSelectScene = "Level Selection";

    [Header("Debug (Testing)")]
    [Tooltip("Tombol untuk menghabiskan satu move saat menguji HUD. " +
             "Hanya berlaku di editor dan Development Build; build rilis selalu mengabaikannya.")]
    [SerializeField] private Key spendMoveKey = Key.M;

    [Header("Touch Controls (Android)")]
    [Tooltip("Prefab kontrol layar sentuh di folder Resources. Dimuat otomatis di setiap scene level, " +
             "jadi sepuluh scene level tidak perlu menyimpan salinan UI yang sama. Posisi, sprite, dan " +
             "transparansi tombolnya diatur di prefab itu, bukan di sini.")]
    [SerializeField] private string touchControlsResourcePath = "UI/Touch Controls";

    #endregion

    #region Runtime State

    private int moves;

    // Tombol debug (M = habiskan satu move) hanya hidup di editor dan Development Build. Tidak ada
    // centang di scene yang harus diingat: build rilis selalu mengabaikannya, jadi pemain tidak bisa
    // memakai pengurang move ini.
    private bool DebugKeysAllowed => Application.isEditor || Debug.isDebugBuild;
    private bool isPaused;
    private bool isCountingDown;

    // True selama intro kamera level (zoom ke tumpukkan tanah sampai hitung mundur 3-2-1) berjalan.
    private bool isIntroActive;
    private bool isDead;
    private bool isWon;

    #endregion

    #region Nested Types

    /// <summary>Penyebab game over, dipakai untuk memilih judul di DeathPopup.</summary>
    public enum DeathReason
    {
        Enemy,
        OutOfMoves,
        Fell
    }

    #endregion

    #region Public Properties

    /// <summary>Sisa moves saat ini.</summary>
    public int Moves => moves;

    /// <summary>Jumlah moves awal yang diisi di Inspector.</summary>
    public int MaxMoves => maxMoves;

    /// <summary>
    /// True saat popup pause sedang terbuka. Dipakai intro kamera dan hitung mundur supaya fase
    /// itu ikut berhenti, bukan berjalan terus di belakang popup.
    /// </summary>
    public bool IsPaused => isPaused;

    #endregion

    #region Unity Events

    private void Awake()
    {
        // Scene bisa dimuat saat game sebelumnya masih pause; pastikan waktu selalu normal.
        Time.timeScale = 1f;

        SetCountdownVisible(false);
        ResetMoves();
        ResolveHudButtons();
        CreateTouchControls();
    }

    /// <summary>
    /// Memuat overlay tombol sentuh dari Resources lalu menaruhnya tepat di bawah objek HUD, supaya
    /// popup pause/game over/menang tetap tampil di atasnya. Prefabnya sendiri yang memutuskan
    /// apakah tombol muncul di Android, di Editor, atau juga di build Windows.
    /// </summary>
    private void CreateTouchControls()
    {
        if (string.IsNullOrWhiteSpace(touchControlsResourcePath))
        {
            return;
        }

        // Scene boleh punya instance sendiri (mis. untuk mengatur tampilan); jangan sampai dobel.
        if (FindFirstObjectByType<TouchControlsView>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(touchControlsResourcePath);
        if (prefab == null)
        {
            if (Application.isMobilePlatform)
            {
                Debug.LogError(
                    $"LevelHudController: prefab '{touchControlsResourcePath}' tidak ada di Resources.",
                    this
                );
            }

            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = prefab.name;

        if (parent == transform.parent)
        {
            instance.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }
        else
        {
            instance.transform.SetAsLastSibling();
        }
    }

    private void OnEnable()
    {
        PlayerController.Died += HandlePlayerDied;
        PlayerController.ShotWeb += HandlePlayerShotWeb;
        SeedPlantingController.PlantingCompleted += HandlePlantingCompleted;
    }

    private void Update()
    {
        if (!DebugKeysAllowed || Keyboard.current == null || isDead || isWon)
        {
            return;
        }

        if (Keyboard.current[spendMoveKey].wasPressedThisFrame)
        {
            ConsumeMove();
        }
    }

    private void OnDisable()
    {
        PlayerController.Died -= HandlePlayerDied;
        PlayerController.ShotWeb -= HandlePlayerShotWeb;
        SeedPlantingController.PlantingCompleted -= HandlePlantingCompleted;

        // Jangan tinggalkan timeScale 0 saat scene diganti.
        if (isPaused || isCountingDown || isDead || isWon)
        {
            Time.timeScale = 1f;
        }
    }

    #endregion

    #region Play Mode Bootstrapping

    /// <summary>
    /// HUD dipaksa aktif setiap kali scene level dimuat. Di scene, objek HUD sering dimatikan
    /// supaya enak mengatur popup di editor; dengan pengaman ini HUD tetap muncul saat Play,
    /// baik centangnya di Hierarchy hidup maupun mati.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHudActiveInPlayMode()
    {
        SceneManager.sceneLoaded -= HandleSceneLoadedForHud;
        SceneManager.sceneLoaded += HandleSceneLoadedForHud;
        EnsureHudActiveInScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoadedForHud(Scene scene, LoadSceneMode mode)
    {
        EnsureHudActiveInScene(scene);
    }

    private static void EnsureHudActiveInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            LevelHudController[] controllers = roots[i].GetComponentsInChildren<LevelHudController>(true);
            for (int c = 0; c < controllers.Length; c++)
            {
                ActivateHierarchy(controllers[c].transform);
            }
        }
    }

    /// <summary>
    /// Mengaktifkan objek beserta induknya, urut dari induk paling atas, supaya Awake/OnEnable
    /// komponen HUD benar-benar berjalan.
    /// </summary>
    private static void ActivateHierarchy(Transform target)
    {
        List<Transform> inactiveChain = new List<Transform>();
        for (Transform current = target; current != null; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
            {
                inactiveChain.Add(current);
            }
        }

        for (int i = inactiveChain.Count - 1; i >= 0; i--)
        {
            inactiveChain[i].gameObject.SetActive(true);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Mengembalikan moves ke nilai awal.</summary>
    public void ResetMoves()
    {
        moves = Mathf.Max(0, maxMoves);
        RefreshMovesLabel();
    }

    /// <summary>
    /// Mengurangi satu move. Dipanggil otomatis lewat <see cref="PlayerController.ShotWeb"/> setiap kali
    /// Webi menembakkan jaring. Move terakhir memicu game over.
    /// Diabaikan saat pause atau saat game over sudah tampil.
    /// </summary>
    public void ConsumeMove()
    {
        if (moves <= 0 || isPaused || isDead || isWon)
        {
            return;
        }

        moves--;
        RefreshMovesLabel();

        if (moves <= 0)
        {
            ShowDeathPopup(DeathReason.OutOfMoves);
        }
    }

    /// <summary>Membuka popup pause dan membekukan gameplay.</summary>
    public void Pause()
    {
        if (isPaused || isCountingDown || isDead || isWon)
        {
            return;
        }

        isPaused = true;
        RefreshTimeScale();

        // Musik ikut berhenti sementara tanpa kehilangan posisinya.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMusic();
        }

        if (pausePopup != null)
        {
            pausePopup.Show();
        }
    }

    /// <summary>Menutup popup pause, memutar hitung mundur, lalu melanjutkan gameplay.</summary>
    public void Resume()
    {
        if (!isPaused || isCountingDown)
        {
            return;
        }

        if (pausePopup != null)
        {
            pausePopup.Hide();
        }

        // Selama intro kamera, hitung mundur milik intro yang dipakai. Menjalankan hitung mundur
        // kedua di atasnya membuat angka 3-2-1 tampil dua kali berturut-turut.
        if (resumeCountdownSeconds <= 0f || isIntroActive)
        {
            FinishResume();
            return;
        }

        StartCoroutine(ResumeCountdownRoutine());
    }

    /// <summary>
    /// Diberi tahu oleh <see cref="LevelStartCameraIntro"/> bahwa intro kamera dan hitung mundur
    /// 3-2-1 sedang berjalan. Selama fase itu tombol Retry disembunyikan.
    /// </summary>
    public void SetIntroActive(bool active)
    {
        if (isIntroActive == active)
        {
            return;
        }

        isIntroActive = active;
        ResolveHudButtons();

        if (retryButton != null)
        {
            retryButton.SetActive(!active);
        }
    }

    /// <summary>
    /// Menyelaraskan <c>Time.timeScale</c> dengan keadaan HUD: pause, hitung mundur, game over,
    /// menang, atau level yang sedang dibekukan intro kamera. Intro dan popup pause sama-sama
    /// menahan waktu di 0, jadi keduanya harus memakai jalur ini supaya tidak saling mencairkan.
    /// </summary>
    public void RefreshTimeScale()
    {
        bool frozen = isPaused || isCountingDown || isDead || isWon
                      || LevelStartCameraIntro.IsWorldFrozen;
        Time.timeScale = frozen ? 0f : 1f;
    }

    /// <summary>
    /// Memutar hitung mundur di tengah layar tanpa menyentuh state pause. Dipakai intro kamera level
    /// supaya pemain melihat hitung mundur yang sama (3-2-1) sebelum Webi boleh bergerak.
    /// </summary>
    public IEnumerator PlayCountdown(float seconds)
    {
        int total = Mathf.Max(1, Mathf.CeilToInt(seconds));
        SetCountdownVisible(true);

        for (int remaining = total; remaining > 0; remaining--)
        {
            if (resumeCountdownLabel != null)
            {
                resumeCountdownLabel.text = remaining.ToString();
            }

            // Hitung mundur ikut berhenti selama popup pause terbuka, jadi angka dan sisa waktunya
            // tidak berjalan di belakang layar.
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        SetCountdownVisible(false);
    }

    /// <summary>
    /// Menampilkan popup game over dan membekukan gameplay. Dipakai saat Webi mati
    /// (dipanggil lewat <see cref="PlayerController.Died"/>) maupun saat moves habis
    /// (dipanggil dari <see cref="ConsumeMove"/>). Judulnya menyesuaikan penyebabnya.
    /// </summary>
    public void ShowDeathPopup(DeathReason reason)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        RefreshTimeScale();

        // Musik berhenti seketika begitu popup game over muncul, di semua level. Retry memuat ulang
        // scene dan memutar musik lagi lewat LevelStartCameraIntro, jadi track-nya tidak hilang
        // permanen. Memakai henti seketika (bukan StopMusic yang ber-fade) supaya tidak ada sisa
        // suara di belakang popup.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }

        if (deathTitleLabel != null)
        {
            deathTitleLabel.text = reason == DeathReason.OutOfMoves
                ? outOfMovesTitle
                : reason == DeathReason.Fell
                    ? fallDeathTitle
                    : enemyDeathTitle;
        }

        if (deathPopup != null)
        {
            deathPopup.Show();
        }
        else
        {
            Debug.LogWarning("LevelHudController: field Death Popup belum diisi.", this);
        }
    }

    /// <summary>
    /// Menampilkan popup menang lalu membekukan gameplay. Dipanggil otomatis lewat
    /// <see cref="SeedPlantingController.PlantingCompleted"/> setelah animasi tumbuh selesai.
    /// </summary>
    public void ShowWinPopup()
    {
        if (isWon || isDead)
        {
            return;
        }

        isWon = true;
        RefreshTimeScale();

        if (winPopup != null)
        {
            winPopup.Show(moves, levelNumber);
        }
        else
        {
            Debug.LogWarning("LevelHudController: field Win Popup belum diisi.", this);
        }
    }

    /// <summary>Mengulang level saat ini.</summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;

        // Retry tidak perlu intro kamera (zoom ke tumpukan tanah lalu ke Webi); level langsung
        // dibuka dengan framing kamera default. Hitung mundur tetap berjalan.
        LevelStartCameraIntro.RequestSkipNextIntro();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    /// <summary>Kembali ke scene Level Selection.</summary>
    public void BackToLevelSelection()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(levelSelectScene))
        {
            Debug.LogError("Level Selection scene name is empty on LevelHudController.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(levelSelectScene))
        {
            Debug.LogError($"Scene '{levelSelectScene}' is missing from Scenes In Build.", this);
            return;
        }

        SceneManager.LoadScene(levelSelectScene);
    }

    #endregion

    #region Internal Helpers

    private IEnumerator ResumeCountdownRoutine()
    {
        isCountingDown = true;
        SetCountdownVisible(true);

        for (int remaining = Mathf.CeilToInt(resumeCountdownSeconds); remaining > 0; remaining--)
        {
            if (resumeCountdownLabel != null)
            {
                resumeCountdownLabel.text = remaining.ToString();
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        FinishResume();
    }

    private void FinishResume()
    {
        SetCountdownVisible(false);
        isCountingDown = false;
        isPaused = false;
        RefreshTimeScale();

        // Musik baru lanjut setelah hitung mundur 3-2-1 selesai, dari detik terakhirnya.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
        }
    }

    private void HandlePlayerDied(PlayerController player)
    {
        DeathReason reason = player != null && player.LastDeathCause == PlayerController.DeathCause.Fell
            ? DeathReason.Fell
            : DeathReason.Enemy;
        ShowDeathPopup(reason);
    }

    /// <summary>Satu tembakan jaring = satu move.</summary>
    private void HandlePlayerShotWeb(PlayerController player)
    {
        ConsumeMove();
    }

    /// <summary>Benih selesai tumbuh: tampilkan popup menang.</summary>
    private void HandlePlantingCompleted(SeedPlantingController planter)
    {
        ShowWinPopup();
    }

    private void RefreshMovesLabel()
    {
        if (movesLabel != null)
        {
            movesLabel.text = string.Format(movesFormat, moves);
        }
    }

    /// <summary>
    /// Mencari tombol Retry milik HUD kalau field-nya kosong. Dicari sebagai anak langsung HUD,
    /// bukan lewat seluruh scene, supaya tombol Retry di dalam DeathPopup tidak ikut terambil.
    /// </summary>
    private void ResolveHudButtons()
    {
        if (retryButton != null)
        {
            return;
        }

        Transform found = transform.Find("RetryButton");
        if (found != null)
        {
            retryButton = found.gameObject;
        }
    }

    private void SetCountdownVisible(bool visible)
    {
        if (resumeCountdownRoot != null)
        {
            resumeCountdownRoot.SetActive(visible);
        }
    }

    #endregion
}
