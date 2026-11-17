using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Satu tombol level di scene Level Selection. Menyimpan identitas level (nomor dan nama scene)
/// sekaligus mengatur tampilannya: tombol terkunci jadi gelap + bergembok + angka disembunyikan,
/// dan tiga bintang di bawahnya terisi sesuai bintang yang tersimpan.
/// Syarat bintang yang membuka tombol ini tidak disimpan di sini, melainkan di
/// <see cref="LevelUnlockRules"/> supaya sama dengan yang dipakai popup menang.
/// </summary>
[DisallowMultipleComponent]
public class LevelButtonView : MonoBehaviour
{
    #region Inspector Fields

    [Header("Level Identity")]
    [Tooltip("Nomor level yang diwakili tombol ini (1, 2, 3, dan seterusnya).")]
    [SerializeField] private int levelNumber = 1;

    [Tooltip("Nama scene yang dibuka saat level ini diklik. Harus terdaftar di Scenes In Build. Kosongkan jika scene levelnya belum dibuat.")]
    [SerializeField] private string sceneName = "Level 1";

    [Header("Visual References")]
    [Tooltip("Gambar latar tombol level. Warna gambar ini yang digelapkan saat level masih terkunci.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Teks angka nomor level. Disembunyikan saat level terkunci supaya ikon gembok terlihat jelas.")]
    [SerializeField] private TMP_Text numberLabel;

    [Tooltip("Ikon gembok yang hanya tampil saat level masih terkunci. Sekarang masih placeholder, ganti sprite-nya kalau aset asli sudah ada.")]
    [SerializeField] private GameObject lockIcon;

    [Tooltip("Tiga ikon bintang di bawah tombol level. Diisi dari kiri ke kanan sesuai bintang yang sudah didapat.")]
    [SerializeField] private Image[] starIcons = new Image[3];

    [Header("Star Sprites")]
    [Tooltip("Sprite bintang yang sudah didapat.")]
    [SerializeField] private Sprite filledStarSprite;

    [Tooltip("Sprite bintang yang belum didapat.")]
    [SerializeField] private Sprite emptyStarSprite;

    [Header("Status Colors")]
    [Tooltip("Warna tombol level dan ikon bintang saat sudah terbuka.")]
    [SerializeField] private Color unlockedColor = Color.white;

    [Tooltip("Warna tombol level dan ikon bintang saat masih terkunci. Dibuat gelap, bukan transparan.")]
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    #endregion

    #region Runtime State

    private Button cachedButton;
    private UIButtonInteractive cachedFeedback;

    #endregion

    #region Public Properties

    /// <summary>Nomor level yang diwakili tombol ini.</summary>
    public int LevelNumber => levelNumber;

    /// <summary>Nama scene tujuan. Kosong berarti scene levelnya belum tersedia.</summary>
    public string SceneName => sceneName;

    /// <summary>Komponen Button pada objek ini, dipakai controller untuk mendaftarkan klik.</summary>
    public Button Button
    {
        get
        {
            if (cachedButton == null)
            {
                cachedButton = GetComponent<Button>();
            }

            return cachedButton;
        }
    }

    #endregion

    #region Unity Events

    private void Awake()
    {
        cachedFeedback = GetComponent<UIButtonInteractive>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Memperbarui tampilan tombol sesuai status buka/tutup dan bintang yang dimiliki pemain.
    /// </summary>
    public void ApplyState(bool isUnlocked, int earnedStars)
    {
        // Animasi hover dimatikan saat terkunci; selain biar tidak menipu, komponen itu juga
        // menimpa warna gambar tiap frame sehingga warna gelap di bawah ini bisa hilang.
        if (cachedFeedback != null)
        {
            cachedFeedback.enabled = isUnlocked;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        if (numberLabel != null)
        {
            numberLabel.gameObject.SetActive(isUnlocked);
        }

        ApplyStars(isUnlocked, earnedStars);
    }

    #endregion

    #region Internal Helpers

    private void ApplyStars(bool isUnlocked, int earnedStars)
    {
        if (starIcons == null)
        {
            return;
        }

        Color starColor = isUnlocked ? unlockedColor : lockedColor;

        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] == null)
            {
                continue;
            }

            Sprite targetSprite = i < earnedStars ? filledStarSprite : emptyStarSprite;

            if (targetSprite != null)
            {
                starIcons[i].sprite = targetSprite;
            }

            // Bintang kosong ikut digelapkan saat level terkunci, supaya warnanya sama dengan tombolnya.
            starIcons[i].color = starColor;
        }
    }

    #endregion
}
