using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pengatur menu bantuan (Help) dan menu tutorial level 1: menampilkan satu slide per topik,
/// berpindah dengan tombol PREV/NEXT, panah kiri/kanan (atau A/D), dan indikator bintang di bawah.
///
/// Tombol PREV disembunyikan di slide pertama dan NEXT disembunyikan di slide terakhir, jadi tidak
/// ada tombol mati di ujung. Kalau Play Scene diisi (menu tutorial), NEXT di slide terakhir tidak
/// disembunyikan: labelnya berubah menjadi Play dan tombol itu memuat scene level.
///
/// Semua animasi memakai waktu nyata, jadi tetap berjalan walau Time.timeScale = 0. Tombol Back
/// dan Esc kembali ke scene Home Screen; keduanya bisa dimatikan lewat Allow Back.
/// </summary>
[DisallowMultipleComponent]
public class HowToPlayController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Slides")]
    [Tooltip("RectTransform tiap slide, urut dari slide 1.")]
    [SerializeField] private RectTransform[] slides;

    [Tooltip("CanvasGroup tiap slide untuk animasi fade. Urutannya sama dengan daftar slide.")]
    [SerializeField] private CanvasGroup[] slideGroups;

    [Header("Navigation")]
    [Tooltip("Tombol pindah ke slide sebelumnya.")]
    [SerializeField] private Button prevButton;

    [Tooltip("Tombol pindah ke slide berikutnya.")]
    [SerializeField] private Button nextButton;

    [Tooltip("Tombol kembali ke Home Screen.")]
    [SerializeField] private Button backButton;

    [Header("End Of Slides")]
    [Tooltip("Jika diisi, slide terakhir tidak menyembunyikan NEXT: tombol yang sama berubah label menjadi " +
             "Play dan memuat scene ini. Dipakai menu tutorial sebelum Level 1; kosongkan untuk menu Help biasa.")]
    [SerializeField] private string playScene = "";

    [Tooltip("Teks tombol NEXT saat masih berpindah slide.")]
    [SerializeField] private string nextLabel = "NEXT";

    [Tooltip("Teks tombol NEXT di slide terakhir saat Play Scene diisi.")]
    [SerializeField] private string playLabel = "PLAY";

    [Tooltip("Teks label di dalam tombol NEXT. Kosongkan untuk dicari otomatis dari anak tombol.")]
    [SerializeField] private TMP_Text nextButtonLabel;

    [Tooltip("Jika dimatikan, tombol Back dan Esc tidak berfungsi. Dipakai menu tutorial yang tidak punya Back.")]
    [SerializeField] private bool allowBack = true;

    [Tooltip("Gambar indikator halaman (bintang), urut sesuai slide.")]
    [SerializeField] private Image[] dots;

    [Tooltip("Sprite bintang untuk halaman yang sedang aktif.")]
    [SerializeField] private Sprite activeDotSprite;

    [Tooltip("Sprite bintang untuk halaman lain.")]
    [SerializeField] private Sprite inactiveDotSprite;

    [Tooltip("Warna bintang halaman aktif.")]
    [SerializeField] private Color activeDotColor = Color.white;

    [Tooltip("Warna bintang halaman lain.")]
    [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("Jika aktif, NEXT di slide terakhir kembali ke slide pertama dan PREV di slide pertama " +
             "ke slide terakhir. Jika mati, tombolnya dinonaktifkan di ujung.")]
    [SerializeField] private bool wrapAround = false;

    [Header("Animation")]
    [Tooltip("Lama animasi geser antar slide (detik, waktu nyata).")]
    [Min(0f)]
    [SerializeField] private float slideDuration = 0.25f;

    [Tooltip("Jarak geser slide keluar/masuk (satuan UI). Harus cukup besar agar slide keluar dari layar.")]
    [Min(0f)]
    [SerializeField] private float slideDistance = 1600f;

    [Header("Keyboard")]
    [Tooltip("Aktifkan tombol panah kiri/kanan (atau A/D) untuk pindah slide dan Esc untuk kembali.")]
    [SerializeField] private bool enableKeyboard = true;

    [Header("Scene")]
    [Tooltip("Scene yang dibuka tombol Back. Harus terdaftar di Scenes In Build.")]
    [SerializeField] private string homeScene = "Home Screen";

    [Header("Touch (Android) Instructions")]
    [Tooltip("Kalau aktif, teks body tiap slide diganti versi sentuh saat bermain di Android/iOS.")]
    [SerializeField] private bool useTouchInstructionsOnMobile = true;

    [Tooltip("Paksa teks sentuh juga di Editor, supaya isinya bisa dicek tanpa build Android.")]
    [SerializeField] private bool previewTouchInstructionsInEditor = false;

    [Tooltip("Teks body pengganti per slide (urut slide 1). Entri kosong memakai teks bawaan versi " +
             "sentuh; kalau teks bawaan juga tidak ada, teks Windows yang sudah ada di scene dipakai.")]
    [SerializeField, TextArea(3, 6)] private string[] touchSlideBodies;

    [Tooltip("Ukuran font minimum saat teks sentuh lebih panjang dari kotak body. TMP mengecilkan teks " +
             "secara otomatis sampai muat, jadi judul slide tidak pernah tertimpa walau kalimatnya " +
             "diubah lewat Inspector.")]
    [Min(10f)]
    [SerializeField] private float touchBodyMinFontSize = 26f;

    #endregion

    #region Runtime State

    private int currentIndex;
    private bool animating;
    private bool wired;

    #endregion

    #region Unity Events

    private void Awake()
    {
        ResolveSlides();
        WireButtons();
        ApplyTouchInstructions();
    }

    private void OnEnable()
    {
        ShowFirstImmediate();
    }

    private void Update()
    {
        if (!enableKeyboard || animating || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            Prev();
            return;
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            Next();
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Back();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Pindah ke slide berikutnya, atau memuat scene Play kalau ini slide terakhir.</summary>
    public void Next()
    {
        if (IsPlaySlide())
        {
            LoadPlayScene();
            return;
        }

        GoTo(currentIndex + 1, 1);
    }

    /// <summary>Pindah ke slide sebelumnya.</summary>
    public void Prev()
    {
        GoTo(currentIndex - 1, -1);
    }

    /// <summary>Kembali ke Home Screen.</summary>
    public void Back()
    {
        if (!allowBack)
        {
            return;
        }

        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(homeScene))
        {
            Debug.LogError("HowToPlayController: Home Scene masih kosong.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(homeScene))
        {
            Debug.LogError($"Scene '{homeScene}' is missing from Scenes In Build.", this);
            return;
        }

        SceneManager.LoadScene(homeScene);
    }

    #endregion

    #region Internal Helpers

    /// <summary>Menampilkan slide pertama tanpa animasi dan menyembunyikan sisanya.</summary>
    private void ShowFirstImmediate()
    {
        if (!HasSlides())
        {
            return;
        }

        currentIndex = 0;
        animating = false;

        for (int i = 0; i < slides.Length; i++)
        {
            bool active = i == 0;
            slides[i].anchoredPosition = Vector2.zero;
            slides[i].gameObject.SetActive(active);

            CanvasGroup group = slideGroups[i];
            if (group != null)
            {
                group.alpha = active ? 1f : 0f;
                group.blocksRaycasts = active;
                group.interactable = false;
            }
        }

        UpdateDots();
        UpdateButtons();
    }

    private void GoTo(int targetIndex, int direction)
    {
        if (!HasSlides() || animating || targetIndex == currentIndex)
        {
            return;
        }

        if (wrapAround)
        {
            int count = slides.Length;
            targetIndex = (targetIndex + count) % count;
        }
        else
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, slides.Length - 1);
        }

        if (targetIndex == currentIndex)
        {
            return;
        }

        StartCoroutine(SlideRoutine(currentIndex, targetIndex, direction));
    }

    private IEnumerator SlideRoutine(int fromIndex, int toIndex, int direction)
    {
        animating = true;

        RectTransform fromSlide = slides[fromIndex];
        RectTransform toSlide = slides[toIndex];
        CanvasGroup fromGroup = slideGroups[fromIndex];
        CanvasGroup toGroup = slideGroups[toIndex];

        // Slide tujuan disiapkan di sisi berlawanan arah gerak.
        toSlide.gameObject.SetActive(true);
        toSlide.anchoredPosition = new Vector2(direction * slideDistance, 0f);

        if (toGroup != null)
        {
            toGroup.alpha = 0f;
            toGroup.blocksRaycasts = false;
            toGroup.interactable = false;
        }

        float duration = Mathf.Max(0.0001f, slideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float outEase = t * t * t;
            float inEase = 1f - Mathf.Pow(1f - t, 3f);

            fromSlide.anchoredPosition = new Vector2(-direction * slideDistance * outEase, 0f);
            toSlide.anchoredPosition = Vector2.Lerp(
                new Vector2(direction * slideDistance, 0f),
                Vector2.zero,
                inEase
            );

            if (fromGroup != null)
            {
                fromGroup.alpha = 1f - outEase;
            }

            if (toGroup != null)
            {
                toGroup.alpha = inEase;
            }

            yield return null;
        }

        fromSlide.gameObject.SetActive(false);
        fromSlide.anchoredPosition = Vector2.zero;
        if (fromGroup != null)
        {
            fromGroup.alpha = 0f;
        }

        toSlide.anchoredPosition = Vector2.zero;
        if (toGroup != null)
        {
            toGroup.alpha = 1f;
            toGroup.blocksRaycasts = true;
        }

        currentIndex = toIndex;
        animating = false;

        UpdateDots();
        UpdateButtons();
    }

    private void UpdateDots()
    {
        if (dots == null)
        {
            return;
        }

        for (int i = 0; i < dots.Length; i++)
        {
            Image dot = dots[i];
            if (dot == null)
            {
                continue;
            }

            bool active = i == currentIndex;
            dot.sprite = active ? activeDotSprite : inactiveDotSprite;
            dot.color = active ? activeDotColor : inactiveDotColor;
        }
    }

    private void UpdateButtons()
    {
        bool playSlide = IsPlaySlide();
        bool showPrev = wrapAround || currentIndex > 0;
        bool showNext = playSlide || wrapAround || currentIndex < slides.Length - 1;

        SetButtonVisible(prevButton, showPrev);
        SetButtonVisible(nextButton, showNext);
        UpdateNextLabel(playSlide);
    }

    /// <summary>
    /// Tombol yang tidak berguna di ujung daftar slide disembunyikan, bukan sekadar dinonaktifkan,
    /// supaya tidak ada tombol mati yang tetap terlihat.
    /// </summary>
    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null && button.gameObject.activeSelf != visible)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private void UpdateNextLabel(bool playSlide)
    {
        if (nextButtonLabel == null && nextButton != null)
        {
            nextButtonLabel = nextButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = playSlide ? playLabel : nextLabel;
        }
    }

    /// <summary>
    /// True kalau slide yang tampil adalah slide terakhir dan menu ini punya scene tujuan Play.
    /// Di kondisi itu tombol NEXT dipakai sebagai tombol Play.
    /// </summary>
    private bool IsPlaySlide()
    {
        return !string.IsNullOrWhiteSpace(playScene) &&
               slides != null &&
               slides.Length > 0 &&
               currentIndex >= slides.Length - 1;
    }

    private void LoadPlayScene()
    {
        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(playScene))
        {
            Debug.LogError($"Scene '{playScene}' is missing from Scenes In Build, so the level cannot be opened.", this);
            return;
        }

        SceneManager.LoadScene(playScene);
    }

    private void WireButtons()
    {
        if (wired)
        {
            return;
        }

        wired = true;

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(Prev);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Next);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(Back);
        }
    }

    private void ResolveSlides()
    {
        if ((slides == null || slides.Length == 0) && slideGroups != null && slideGroups.Length > 0)
        {
            slides = new RectTransform[slideGroups.Length];
            for (int i = 0; i < slideGroups.Length; i++)
            {
                slides[i] = slideGroups[i] != null ? slideGroups[i].transform as RectTransform : null;
            }
        }

        if (slides == null)
        {
            slides = new RectTransform[0];
        }

        if (slideGroups == null || slideGroups.Length != slides.Length)
        {
            CanvasGroup[] groups = new CanvasGroup[slides.Length];
            for (int i = 0; i < slides.Length; i++)
            {
                groups[i] = slides[i] != null ? slides[i].GetComponent<CanvasGroup>() : null;
            }

            slideGroups = groups;
        }
    }

    /// <summary>
    /// Teks body bawaan untuk perangkat sentuh, dipakai kalau daftar Touch Slide Bodies di Inspector
    /// masih kosong. Isinya sama dengan panduan Windows, hanya bagian gerak membidik dan menggeser
    /// kamera yang disesuaikan dengan tombol layar. Ilustrasi slide belum digambar ulang untuk
    /// Android; kalau sudah, cukup ganti sprite Illustration-nya.
    /// </summary>
    private static readonly string[] DefaultTouchSlideBodies =
    {
        "Pick up the seed and carry it to a dirt pile. When the plant grows, the level is complete.",
        "Use the arrow buttons to walk and drag empty space to look around. Webi sticks to every surface.",
        "Hold the net pad and drag to aim, then release to shoot. Drop it on the X to cancel. One shot = one Move.",
        "Walk over a seed to pick it up. Carry it into the dirt pile and Webi plants it automatically.",
        "Thorns kill on contact. Oil makes Webi lose grip and fall. Ice is slippery, so stop early.",
        "Touching a Bug or a Fly is fatal. Portals teleport Webi, and levers open gates.",
    };

    /// <summary>
    /// Mengganti teks body slide dengan versi sentuh saat berjalan di Android/iOS. Slide yang tidak
    /// punya teks pengganti dibiarkan seperti aslinya.
    /// </summary>
    private void ApplyTouchInstructions()
    {
        if (!useTouchInstructionsOnMobile)
        {
            return;
        }

        bool touchDevice = Application.isMobilePlatform ||
            (Application.isEditor && previewTouchInstructionsInEditor);
        if (!touchDevice || slides == null)
        {
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            string touchText = ResolveTouchSlideBody(i);
            if (string.IsNullOrWhiteSpace(touchText))
            {
                continue;
            }

            TMP_Text body = FindSlideText(slides[i], "SlideBody");
            if (body == null)
            {
                continue;
            }

            body.text = touchText;
            FitTouchBody(body);
        }
    }

    /// <summary>
    /// Teks sentuh bisa lebih panjang daripada teks Windows, sedangkan kotak body di scene hanya
    /// 500x300 dengan teks rata tengah. Auto-size TMP dipakai supaya teks mengecil sendiri sampai
    /// muat, jadi barisnya tidak menabrak judul slide di atasnya. Teks Windows di scene tidak
    /// disentuh; auto-size hanya hidup untuk slide yang teksnya diganti versi sentuh.
    /// </summary>
    private void FitTouchBody(TMP_Text body)
    {
        if (!body.enableAutoSizing)
        {
            // Ukuran font asli (mis. 40) dijadikan batas atas, jadi teks pendek tetap sebesar aslinya.
            body.fontSizeMax = body.fontSize;
            body.enableAutoSizing = true;
        }

        body.fontSizeMin = Mathf.Clamp(touchBodyMinFontSize, 10f, body.fontSizeMax);
    }

    private string ResolveTouchSlideBody(int index)
    {
        if (touchSlideBodies != null &&
            index < touchSlideBodies.Length &&
            !string.IsNullOrWhiteSpace(touchSlideBodies[index]))
        {
            return touchSlideBodies[index];
        }

        return index < DefaultTouchSlideBodies.Length ? DefaultTouchSlideBodies[index] : null;
    }

    /// <summary>Mencari label TMP di dalam slide berdasarkan nama objeknya (mis. "SlideBody").</summary>
    private static TMP_Text FindSlideText(RectTransform slide, string objectName)
    {
        if (slide == null)
        {
            return null;
        }

        TMP_Text[] texts = slide.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null &&
                texts[i].name.StartsWith(objectName, System.StringComparison.Ordinal))
            {
                return texts[i];
            }
        }

        return null;
    }

    private bool HasSlides()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("HowToPlayController: daftar slide masih kosong.", this);
            return false;
        }

        return true;
    }

    #endregion
}
