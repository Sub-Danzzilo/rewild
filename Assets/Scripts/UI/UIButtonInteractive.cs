using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonInteractive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    #region Inspector Fields

    [Header("Target Graphic Settings")]
    [Tooltip("Target gambar atau grafik UI yang warnanya akan diubah saat interaksi. Jika kosong, akan mengambil grafik utama dari Button.")]
    [SerializeField] private Graphic targetGraphic;

    [Header("Hover Visual Settings")]
    [Tooltip("Apakah skala tombol akan membesar saat kursor berada di atas tombol?")]
    [SerializeField] private bool enableHoverScale = true;

    [Tooltip("Skala ukuran tombol saat di-hover (misal: 1.1 berarti membesar 10%).")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    [Tooltip("Warna grafik saat tombol di-hover.")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    [Header("Active / Click Visual Settings")]
    [Tooltip("Apakah skala tombol akan mengecil saat tombol ditekan?")]
    [SerializeField] private bool enableActiveScale = true;

    [Tooltip("Skala ukuran tombol saat aktif ditekan (misal: 0.95 berarti mengecil 5%).")]
    [SerializeField] private Vector3 activeScale = new Vector3(0.95f, 0.95f, 1f);

    [Tooltip("Warna grafik saat tombol ditekan.")]
    [SerializeField] private Color activeColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Animation Settings")]
    [Tooltip("Kecepatan transisi perubahan ukuran dan warna (makin tinggi nilainya, makin cepat transisinya).")]
    [SerializeField] private float transitionSpeed = 15f;

    [Header("Hit Area Settings")]
    [Tooltip("Ambang alpha untuk area hover dan klik. Dengan nilai ini area interaksi mengikuti bentuk " +
             "sprite tombol, bukan seluruh kotak RectTransform-nya.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float alphaHitThreshold = 0.1f;

    [Header("Audio Settings (SFX)")]
    [Tooltip("AudioSource cadangan, hanya dipakai jika tidak ada AudioManager di scene. Biarkan kosong; suara tombol akan diputar lewat AudioManager supaya volume dan mute SFX dari Settings berlaku.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Clip efek suara saat kursor masuk ke area tombol (Hover). Seret file clip dari jendela Project (bukan dari Hierarchy). Kosongkan jika tidak perlu.")]
    [SerializeField] private AudioClip hoverSound;

    [Tooltip("Volume suara hover (0 = senyap, 1 = penuh). Suara UI biasanya lebih pelan daripada SFX gameplay.")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.4f;

    [Tooltip("Clip efek suara saat tombol ditekan (Active/Click). Seret file clip dari jendela Project (bukan dari Hierarchy). Kosongkan jika tidak perlu.")]
    [SerializeField] private AudioClip activeSound;

    [Tooltip("Volume suara klik (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    [SerializeField] private float activeVolume = 0.6f;

    [Tooltip("Pitch acak minimal dan maksimal agar suara SFX bervariasi dan tidak terkesan monoton saat diklik berulang kali.")]
    [Range(0.8f, 1.2f)] [SerializeField] private float minPitch = 0.95f;

    [Tooltip("Pitch acak maksimal. Nilai ini dipakai bersama Min Pitch untuk memilih pitch acak tiap klik.")]
    [Range(0.8f, 1.2f)] [SerializeField] private float maxPitch = 1.05f;

    [Header("Debug Settings")]
    [Tooltip("Aktifkan untuk menampilkan log debug area klik tombol (UI HitArea) di console.")]
    public bool debugMode = false;

    #endregion

    #region Runtime State

    // Privates for Internal Mechanics
    private Vector3 originalScale;
    private Color originalColor;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isHovered = false;
    private bool isActive = false;
    private Button buttonComponent;

    #endregion

    #region Unity Events

    private void Awake()
    {
        // Menyimpan nilai dasar awal
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Mencari komponen tombol
        buttonComponent = GetComponent<Button>();

        // Mengambil target grafik jika belum diisi manual
        if (targetGraphic == null && buttonComponent != null)
        {
            targetGraphic = buttonComponent.targetGraphic;
        }

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
            targetColor = originalColor;
        }

        // AudioSource hanya cadangan; utama lewat AudioManager (lihat PlaySound).
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Menonaktifkan Transition default dari Button Unity agar tidak bertabrakan dengan script ini
        if (buttonComponent != null)
        {
            buttonComponent.transition = Selectable.Transition.None;
        }

        ApplyAccurateHitArea();
    }

    /// <summary>
    /// Area hover/klik dibuat mengikuti bentuk sprite tombol. Tombol memakai Image Simple dengan
    /// Preserve Aspect, jadi gambar plank lebih kecil dari RectTransform-nya; tanpa ini sisa area
    /// transparan di sekitar gambar tetap ikut ter-hover dan bisa diklik.
    /// </summary>
    private void ApplyAccurateHitArea()
    {
        Image image = targetGraphic as Image;
        if (image == null)
        {
            return;
        }

        // Grafik tombol harus menerima raycast supaya seluruh badan gambar bisa diklik,
        // bukan hanya teks label di atasnya.
        image.raycastTarget = true;

        Sprite sprite = image.sprite;
        bool readable = sprite != null && sprite.texture != null && sprite.texture.isReadable;

        // Margin kosong akibat Preserve Aspect selalu dibuang dulu, jadi area raycast
        // minimal sudah seukuran gambar walaupun uji alpha tidak tersedia.
        image.raycastPadding = CalculateLetterboxPadding(image);

        if (readable)
        {
            // Piksel transparan tidak lagi menangkap hover maupun klik.
            image.alphaHitTestMinimumThreshold = Mathf.Max(0.01f, alphaHitThreshold);
        }

        if (debugMode)
        {
            Debug.Log(
                $"[UI HitArea] {name}: rect={image.rectTransform.rect.size} " +
                $"pad={image.raycastPadding} alpha={image.alphaHitTestMinimumThreshold:F2} readable={readable}",
                this
            );
        }
    }

    /// <summary>Inset area raycast sebesar margin kosong yang ditinggalkan Preserve Aspect.</summary>
    private static Vector4 CalculateLetterboxPadding(Image image)
    {
        Sprite sprite = image.sprite;
        Rect rect = image.rectTransform.rect;
        if (!image.preserveAspect ||
            sprite == null ||
            sprite.rect.height <= 0f ||
            rect.width <= 0f ||
            rect.height <= 0f)
        {
            return Vector4.zero;
        }

        float spriteAspect = sprite.rect.width / sprite.rect.height;
        if (rect.width / rect.height > spriteAspect)
        {
            // Gambar menempel di kiri-kanan, sisa kosong ada di samping.
            float side = (rect.width - rect.height * spriteAspect) * 0.5f;
            return new Vector4(side, 0f, side, 0f);
        }

        float vertical = (rect.height - rect.width / spriteAspect) * 0.5f;
        return new Vector4(0f, vertical, 0f, vertical);
    }

    private void OnEnable()
    {
        // Reset keadaan tombol saat GameObject diaktifkan kembali
        ResetState();
    }

    private void Update()
    {
        // Melakukan interpasi animasi secara halus (Smooth Interpolation)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        if (targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    #endregion

    #region Event System Handlers

    // Dipanggil saat kursor masuk ke area tombol (Hover Start)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonComponent != null && !buttonComponent.interactable) return;

        isHovered = true;

        if (!isActive)
        {
            ApplyHoverState();
            PlaySound(hoverSound, hoverVolume);
        }
    }

    // Dipanggil saat kursor keluar dari area tombol (Hover End)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonComponent != null && !buttonComponent.interactable) return;

        isHovered = false;

        if (!isActive)
        {
            ResetState();
        }
    }

    // Dipanggil saat tombol mouse ditekan di atas tombol (Active Start)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonComponent != null && !buttonComponent.interactable) return;

        isActive = true;
        ApplyActiveState();
        PlaySound(activeSound, activeVolume);
    }

    // Dipanggil saat tombol mouse dilepas (Active End)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonComponent != null && !buttonComponent.interactable) return;

        isActive = false;

        if (isHovered)
        {
            ApplyHoverState();
        }
        else
        {
            ResetState();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Mengembalikan tombol ke ukuran dan warna dasar.</summary>
    public void ResetState()
    {
        isHovered = false;
        isActive = false;
        targetScale = originalScale;
        targetColor = originalColor;
    }

    #endregion

    #region Internal Helpers

    private void ApplyHoverState()
    {
        if (enableHoverScale) targetScale = Vector3.Scale(originalScale, hoverScale);
        targetColor = hoverColor;
    }

    private void ApplyActiveState()
    {
        if (enableActiveScale) targetScale = Vector3.Scale(originalScale, activeScale);
        targetColor = activeColor;
    }

    private void PlaySound(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        // Lewat AudioManager agar volume dan mute SFX dari Settings berlaku untuk suara tombol.
        // PlayUiSfx dipakai supaya klik tombol tetap terdengar saat SFX gameplay dibungkam
        // selama intro kamera level.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiSfx(clip, minPitch, maxPitch, volumeScale);
            return;
        }

        // Cadangan jika AudioManager tidak ada di scene.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // Variasi Pitch agar SFX terasa lebih natural
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    #endregion
}
