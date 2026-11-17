using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animasi popup UI: panel masuk meluncur dari atas ke tengah, lalu keluar dari tengah ke bawah.
/// Latar gelap (Image fullscreen pada objek ini) ikut memudar. Tidak butuh Animator atau library tween,
/// dan memakai unscaled time sehingga tetap berjalan saat Time.timeScale = 0.
///
/// Setup: pasang di root popup (misalnya SettingsPopup) lalu isi field Panel dengan panel yang digeser
/// (misalnya SettingsPanel). Root popup boleh dibiarkan nonaktif di scene: Show() mengaktifkannya, dan
/// Hide() menonaktifkannya lagi setelah animasi selesai, jadi beberapa popup tidak saling menumpuk.
/// Tombol Settings memanggil Show(), tombol Back memanggil Hide().
/// </summary>
[DisallowMultipleComponent]
public class UIPopupSlide : MonoBehaviour
{
    #region Inspector Fields

    [Header("Animation Target")]
    [Tooltip("RectTransform yang digeser, misal SettingsPanel. Jika kosong, dipakai anak pertama dari objek ini.")]
    [SerializeField] private RectTransform panel;

    [Header("Backdrop")]
    [Tooltip("Gambar latar gelap popup. Jika kosong, dipakai Image di objek popup ini sendiri.")]
    [SerializeField] private Image backdrop;

    [Tooltip("Kepekatan latar gelap popup. 1 = hitam pekat tanpa latar permainan terlihat, " +
             "0,8 = latar permainan masih terlihat samar di belakang popup, 0 = tanpa latar gelap.")]
    [Range(0f, 1f)]
    [SerializeField] private float backdropOpacity = 0.8f;

    [Header("Slide Distance")]
    [Tooltip("Jarak dari posisi tengah ke atas saat panel muncul (satuan UI). Harus cukup besar agar panel keluar dari layar.")]
    [SerializeField] private float enterDistance = 1000f;

    [Tooltip("Jarak dari posisi tengah ke bawah saat panel menutup (satuan UI).")]
    [SerializeField] private float exitDistance = 1000f;

    [Header("Duration")]
    [Tooltip("Lama animasi muncul, dalam detik.")]
    [SerializeField] private float showDuration = 0.35f;

    [Tooltip("Lama animasi menutup, dalam detik.")]
    [SerializeField] private float hideDuration = 0.28f;

    [Header("On Scene Start")]
    [Tooltip("Jika aktif, popup langsung disembunyikan saat scene mulai. Biarkan aktif kalau popup memang tidak boleh tampil di awal.")]
    [SerializeField] private bool startHidden = true;

    #endregion

    #region Runtime State

    private Vector2 restPosition;
    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;
    private bool isVisible;
    private bool isAnimating;
    private bool initialized;

    // True while Show() is activating the popup, so the Awake that runs inside SetActive(true)
    // does not immediately hide the popup again because of startHidden.
    private bool suppressAutoHide;

    #endregion

    #region Public Properties

    /// <summary>True while the popup is visible or currently animating in.</summary>
    public bool IsVisible => isVisible;

    #endregion

    #region Unity Events

    private void Awake()
    {
        Initialize();
        if (!initialized) return;

        if (suppressAutoHide) return;

        if (startHidden)
        {
            SnapHidden();
        }
        else
        {
            isVisible = true;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Open: activates the popup, slides the panel down from the top, background fades in.</summary>
    public void Show()
    {
        Initialize();
        if (!initialized) return;
        if (isVisible && !isAnimating) return;

        StopAnimation();
        ApplyBackdrop();

        // Closed means: inactive, or still active but fully transparent.
        bool wasClosed = !gameObject.activeSelf
                         || (!isVisible && (canvasGroup == null || canvasGroup.alpha <= 0.001f));

        if (!gameObject.activeSelf)
        {
            // The first activation runs Awake right here; that must not hide the popup again.
            suppressAutoHide = true;
            gameObject.SetActive(true);
            suppressAutoHide = false;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("UIPopupSlide: the popup sits under an inactive parent, so Show() cannot animate.", this);
            return;
        }

        Vector2 from;
        float fromAlpha;

        if (wasClosed)
        {
            // Start offscreen above the centered position, fully transparent.
            from = restPosition + Vector2.up * enterDistance;
            fromAlpha = 0f;
        }
        else
        {
            // Interrupted slide-out: continue from wherever the panel currently is.
            from = panel.anchoredPosition;
            fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        }

        panel.anchoredPosition = from;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = fromAlpha;
        }

        isVisible = true;
        SetRaycastBlocking(true);
        animationRoutine = StartCoroutine(Animate(from, restPosition, fromAlpha, 1f, showDuration, true));
    }

    /// <summary>Close: slides the panel down offscreen, background fades out, then deactivates the popup.</summary>
    public void Hide()
    {
        Initialize();
        if (!initialized) return;
        if (!isVisible && !isAnimating) return;

        if (!gameObject.activeInHierarchy)
        {
            SnapHidden();
            return;
        }

        StopAnimation();

        Vector2 to = restPosition + Vector2.down * exitDistance;
        float fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        isVisible = false;
        // While sliding out, stop blocking clicks so the buttons behind can be used again.
        SetRaycastBlocking(false);
        animationRoutine = StartCoroutine(Animate(panel.anchoredPosition, to, fromAlpha, 0f, hideDuration, false));
    }

    /// <summary>Opens if closed, closes if open.</summary>
    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    #endregion

    #region Internal Helpers

    private IEnumerator Animate(Vector2 fromPosition, Vector2 toPosition, float fromAlpha, float toAlpha,
        float duration, bool showing)
    {
        isAnimating = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = showing ? EaseOut(t) : EaseIn(t);

            panel.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, Mathf.Clamp01(t));
            }

            yield return null;
        }

        panel.anchoredPosition = toPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = toAlpha;
        }

        isAnimating = false;
        animationRoutine = null;

        if (!showing)
        {
            // Closed and fully offscreen: turn the popup off, same as unchecking it in the Hierarchy.
            gameObject.SetActive(false);
        }
    }

    private void SnapHidden()
    {
        isVisible = false;
        isAnimating = false;
        panel.anchoredPosition = restPosition + Vector2.up * enterDistance;
        SetRaycastBlocking(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Initialize()
    {
        if (initialized) return;

        if (panel == null && transform.childCount > 0)
        {
            panel = transform.GetChild(0) as RectTransform;
        }

        if (panel == null)
        {
            Debug.LogError("UIPopupSlide: the Panel field is not assigned.", this);
            enabled = false;
            return;
        }

        // The panel's centered position is cached; the animation always returns here.
        restPosition = panel.anchoredPosition;

        // Memastikan background overlay menutupi layar sepenuhnya tanpa celah di pojok/sisi layar,
        // bahkan jika aspect ratio layar lebar/Free Aspect atau canvas scale tidak bulat.
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(-2000f, -2000f);
            rootRect.offsetMax = new Vector2(2000f, 2000f);
        }

        if (backdrop == null)
        {
            backdrop = GetComponent<Image>();
        }

        ApplyBackdrop();

        // The CanvasGroup on this object handles the dark background fade and blocks clicks while open.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        initialized = true;
    }

    /// <summary>
    /// Latar gelap popup dibuat pekat dan dilebarkan melewati tepi layar, jadi tidak ada
    /// bagian layar yang tetap terang saat popup mana pun terbuka.
    /// </summary>
    private void ApplyBackdrop()
    {
        if (backdrop == null)
        {
            return;
        }

        RectTransform backdropRect = backdrop.rectTransform;
        if (backdropRect != null && backdropRect != panel)
        {
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(-2000f, -2000f);
            backdropRect.offsetMax = new Vector2(2000f, 2000f);
        }

        Color backdropColor = backdrop.color;
        backdropColor.a = Mathf.Clamp01(backdropOpacity);
        backdrop.color = backdropColor;
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        isAnimating = false;
    }

    private void SetRaycastBlocking(bool value)
    {
        if (canvasGroup == null) return;

        canvasGroup.blocksRaycasts = value;
        canvasGroup.interactable = value;
    }

    // Decelerates at the end: the panel rises and settles softly.
    private static float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    // Accelerates at the end: the panel drops down.
    private static float EaseIn(float t)
    {
        return t * t * t;
    }

    #endregion
}
