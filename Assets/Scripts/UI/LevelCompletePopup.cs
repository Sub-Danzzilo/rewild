using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Popup menang: muncul setelah benih ditanam dan animasi tumbuh selesai.
/// Menampilkan 3 bintang kosong yang terisi satu per satu sesuai sisa moves,
/// dengan tiga tombol: level berikutnya, Level Selection, dan ulangi level ini.
/// </summary>
public class LevelCompletePopup : MonoBehaviour
{
    #region Inspector Fields

    [Header("Animation")]
    [Tooltip("Animasi popup (UIPopupSlide di root popup ini).")]
    [SerializeField] private UIPopupSlide slide;

    [Header("Stars")]
    [Tooltip("Tiga Image bintang, urut dari kiri ke kanan.")]
    [SerializeField] private Image[] stars = new Image[3];

    [Tooltip("Sprite bintang kosong (misal icon_star_empty).")]
    [SerializeField] private Sprite emptyStar;

    [Tooltip("Sprite bintang terisi (misal icon_star).")]
    [SerializeField] private Sprite filledStar;

    [Tooltip("Warna untuk bintang kosong. Putih = pakai warna asli sprite.")]
    [SerializeField] private Color emptyStarColor = Color.white;

    [Tooltip("Warna untuk bintang terisi. Putih = pakai warna asli sprite.")]
    [SerializeField] private Color filledStarColor = Color.white;

    [Tooltip("Jeda antar bintang saat terisi, dalam detik (pakai waktu nyata).")]
    [SerializeField, Min(0f)] private float starFillDelay = 0.4f;

    [Tooltip("Lama animasi 'pop' setiap bintang. 0 = tanpa animasi.")]
    [SerializeField, Min(0f)] private float starPopDuration = 0.18f;

    [Header("Star Sound")]
    [Tooltip("SFX saat satu bintang terisi. Pitch berbeda per bintang supaya terdengar naik.")]
    [SerializeField] private AudioClip starSfx;

    [Tooltip("Pitch tiap bintang, urut bintang 1, 2, 3. Bintang 1 paling rendah.")]
    [SerializeField] private float[] starSfxPitches = { 0.85f, 1f, 1.15f };

    [Tooltip("Volume tiap bintang, urut bintang 1, 2, 3.")]
    [SerializeField] private float[] starSfxVolumes = { 1f, 1f, 1f };

    [Header("Star Rating (tune as needed)")]
    [Tooltip("Sisa moves minimal untuk mendapat 3 bintang.")]
    public int movesForThreeStars = 3;

    [Tooltip("Sisa moves minimal untuk mendapat 2 bintang. Di bawah nilai ini = 1 bintang.")]
    public int movesForTwoStars = 1;

    [Header("Scene")]
    [Tooltip("Scene level berikutnya. Kosongkan untuk level terakhir.")]
    [SerializeField] private string nextLevelScene = "";

    [Tooltip("Tombol Next Level di popup. Disembunyikan otomatis kalau Next Level Scene kosong " +
             "(mis. level terakhir).")]
    [SerializeField] private GameObject nextLevelButton;

    #endregion

    #region Runtime State

    // Prefab popup "level masih terkunci" dimuat dari Resources supaya sepuluh scene level tidak
    // perlu menyimpan salinan UI-nya masing-masing. Item yang sama dipakai Level Selection.
    private const string LockedPopupResourcePath = "UI/LockedLevelPopup";

    private Coroutine fillRoutine;
    private LockedLevelPopup lockedPopup;

    #endregion

    #region Public Methods

    /// <summary>Jumlah bintang (1 sampai 3) berdasarkan sisa moves.</summary>
    public int EvaluateStars(int remainingMoves)
    {
        int twoStarThreshold = Mathf.Max(0, movesForTwoStars);
        int threeStarThreshold = Mathf.Max(twoStarThreshold, movesForThreeStars);

        if (remainingMoves >= threeStarThreshold)
        {
            return 3;
        }

        if (remainingMoves >= twoStarThreshold)
        {
            return 2;
        }

        return 1;
    }

    /// <summary>
    /// Tampilkan popup menang, simpan bintang terbaik ke progres level, lalu isi bintangnya
    /// sesuai sisa moves.
    /// </summary>
    public void Show(int remainingMoves, int levelNumber)
    {
        gameObject.SetActive(true);

        // Level terakhir tidak punya level berikutnya, jadi tombol Next Level disembunyikan.
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(!string.IsNullOrWhiteSpace(nextLevelScene));
        }

        if (slide != null)
        {
            slide.Show();
        }

        ResetStars();

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        int earnedStars = EvaluateStars(remainingMoves);
        AwardStars(levelNumber, earnedStars);

        if (isActiveAndEnabled)
        {
            fillRoutine = StartCoroutine(FillStarsRoutine(earnedStars));
        }
    }

    /// <summary>
    /// Buka scene level berikutnya. Kalau total bintang pemain belum memenuhi syarat level
    /// berikutnya (lihat <see cref="LevelUnlockRules"/>), scene TIDAK dimuat: popup "level masih
    /// terkunci" muncul di depan popup menang, dan pemain tetap di level ini.
    /// </summary>
    public void NextLevel()
    {
        if (string.IsNullOrWhiteSpace(nextLevelScene))
        {
            Debug.LogWarning("LevelCompletePopup: field 'nextLevelScene' belum diisi nama scene level berikutnya.", this);
            return;
        }

        // Dicek sebelum timeScale dikembalikan ke 1: kalau level berikutnya masih terkunci,
        // popup terkunci tampil di atas popup menang dan level tetap dalam kondisi berhenti.
        if (!IsNextLevelReachable())
        {
            return;
        }

        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(nextLevelScene))
        {
            Debug.LogError($"Scene '{nextLevelScene}' tidak ada di Scenes In Build.", this);
            return;
        }

        SceneManager.LoadScene(nextLevelScene);
    }

    #endregion

    #region Unity Events

    private void OnDisable()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// True kalau tombol Next Level boleh memuat scene berikutnya. Kalau belum, popup kunci
    /// ditampilkan lebih dulu dan hasilnya false.
    /// </summary>
    private bool IsNextLevelReachable()
    {
        if (!LevelUnlockRules.TryGetLevelNumber(nextLevelScene, out int nextLevelNumber))
        {
            // Nama scene di luar pola "Level <angka>" tidak bisa dicek syaratnya; biarkan seperti dulu.
            return true;
        }

        int totalStars = LevelUnlockRules.GetTotalStars();
        int missingStars = LevelUnlockRules.GetMissingStars(nextLevelNumber, totalStars);
        if (missingStars <= 0)
        {
            return true;
        }

        LockedLevelPopup popup = EnsureLockedPopup();
        if (popup == null)
        {
            // Tanpa popup, pemain tidak dapat penjelasan; lebih aman tetap di level ini daripada
            // diam-diam membuka level yang masih terkunci.
            return false;
        }

        popup.ShowLocked(nextLevelNumber, missingStars);
        return false;
    }

    /// <summary>
    /// Menyiapkan popup kunci sekali saja: dimuat dari Resources lalu ditempel sebagai anak terakhir
    /// dari induk popup menang, supaya digambar paling depan dan menutupi tombol-tombol di belakangnya.
    /// </summary>
    private LockedLevelPopup EnsureLockedPopup()
    {
        if (lockedPopup != null)
        {
            return lockedPopup;
        }

        GameObject prefab = Resources.Load<GameObject>(LockedPopupResourcePath);
        if (prefab == null)
        {
            Debug.LogError(
                "LevelCompletePopup: prefab popup kunci tidak ditemukan di Resources/" + LockedPopupResourcePath,
                this
            );
            return null;
        }

        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject instance = Instantiate(prefab, parent);
        instance.name = "LockedLevelPopup";
        instance.transform.SetAsLastSibling();

        lockedPopup = instance.GetComponent<LockedLevelPopup>();
        return lockedPopup;
    }

    private IEnumerator FillStarsRoutine(int earnedStars)
    {
        if (starFillDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(starFillDelay);
        }

        for (int i = 0; i < stars.Length; i++)
        {
            if (i >= earnedStars)
            {
                break;
            }

            if (stars[i] == null)
            {
                continue;
            }

            PlayStarSfx(i);
            yield return FillStar(stars[i]);

            if (starFillDelay > 0f && i < earnedStars - 1)
            {
                yield return new WaitForSecondsRealtime(starFillDelay);
            }
        }

        fillRoutine = null;
    }

    private IEnumerator FillStar(Image star)
    {
        if (filledStar != null)
        {
            star.sprite = filledStar;
        }

        star.color = filledStarColor;

        if (starPopDuration <= 0f)
        {
            yield break;
        }

        Transform starTransform = star.transform;
        Vector3 baseScale = starTransform.localScale;
        float elapsed = 0f;

        while (elapsed < starPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / starPopDuration);
            starTransform.localScale = baseScale * (1f + 0.25f * Mathf.Sin(progress * Mathf.PI));
            yield return null;
        }

        starTransform.localScale = baseScale;
    }

    /// <summary>Menyimpan bintang terbaik level ini supaya terbaca di Level Selection.</summary>
    private void AwardStars(int levelNumber, int earnedStars)
    {
        if (levelNumber < 1)
        {
            return;
        }

        int maxStars = stars != null && stars.Length > 0 ? stars.Length : 3;
        LevelProgress.AwardStars(levelNumber, earnedStars, maxStars);
    }

    /// <summary>
    /// SFX tiap bintang terisi; pitch dan volume diambil per urutan bintang. Diputar lewat jalur UI
    /// karena popup menang sudah membekukan SFX gameplay (loop portal/musuh/platform), dan suara
    /// bintang justru bagian dari animasi popup itu sendiri.
    /// </summary>
    private void PlayStarSfx(int index)
    {
        if (starSfx == null || AudioManager.Instance == null)
        {
            return;
        }

        float pitch = GetValue(starSfxPitches, index, 1f);
        float volume = GetValue(starSfxVolumes, index, 1f);
        AudioManager.Instance.PlayUiSfx(starSfx, pitch, pitch, volume);
    }

    private static float GetValue(float[] values, int index, float fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private void ResetStars()
    {
        if (stars == null)
        {
            return;
        }

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null)
            {
                continue;
            }

            if (emptyStar != null)
            {
                stars[i].sprite = emptyStar;
            }

            stars[i].color = emptyStarColor;
        }
    }

    #endregion
}
