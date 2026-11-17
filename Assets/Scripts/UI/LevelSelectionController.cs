using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pengatur scene Level Selection: menghitung total bintang, membuka/mengunci tiap tombol level,
/// memuat scene level saat tombol yang sudah terbuka diklik, dan menampilkan popup saat tombol
/// yang masih terkunci diklik.
///
/// Syarat buka level diatur terpusat di <see cref="LevelUnlockRules"/> (satu tabel di kode), supaya
/// tombol di scene ini dan tombol Next Level di popup menang selalu memakai angka yang sama.
/// </summary>
[DisallowMultipleComponent]
public class LevelSelectionController : MonoBehaviour
{
    #region Inspector Fields

    [Header("UI References")]
    [Tooltip("Teks penghitung total bintang di bagian atas layar.")]
    [SerializeField] private TMP_Text totalStarsLabel;

    [Tooltip("Semua tombol level di scene ini. Urutan bebas; nomor level diambil dari tiap LevelButtonView.")]
    [SerializeField] private LevelButtonView[] levelButtons;

    [Tooltip("Popup yang muncul saat pemain mengklik level yang masih terkunci.")]
    [SerializeField] private LockedLevelPopup lockedPopup;

    [Header("Stars")]
    [Tooltip("Jumlah bintang maksimal yang bisa didapat dari satu level.")]
    [SerializeField, Min(1)] private int maxStarsPerLevel = 3;

    [Header("Debug (Testing)")]
    [Tooltip("Mengaktifkan tombol keyboard untuk menguji progres tanpa harus menang level. " +
             "Hanya berlaku di editor dan Development Build; build rilis selalu mengabaikannya.")]
    [SerializeField] private bool enableDebugKeys = true;

    [Tooltip("Tombol untuk menambah satu bintang ke level terbuka tertinggi, supaya bisa mengecek level berikutnya terbuka.")]
    [SerializeField] private Key addStarKey = Key.F1;

    [Tooltip("Tombol untuk menghapus semua progres bintang dan mengembalikan semua level ke kondisi awal.")]
    [SerializeField] private Key resetProgressKey = Key.F2;

    #endregion

    #region Runtime State

    // Total bintang pemain saat ini, dihitung ulang setiap kali tampilan diperbarui.
    private int currentTotalStars;

    // Tombol debug (F1/F2) hanya hidup di editor dan Development Build. Build rilis selalu
    // mengabaikannya walau centang Enable Debug Keys tidak sengaja terbawa dari duplikat scene.
    private bool DebugKeysAllowed => enableDebugKeys && (Application.isEditor || Debug.isDebugBuild);

    #endregion

    #region Unity Events

    private void Awake()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (!DebugKeysAllowed || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[addStarKey].wasPressedThisFrame)
        {
            AddDebugStar();
        }

        if (Keyboard.current[resetProgressKey].wasPressedThisFrame)
        {
            LevelProgress.ResetAll(LevelUnlockRules.MaxLevel);
            RefreshAll();
            Debug.Log("Level progress reset: all stars cleared.");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Menghitung ulang total bintang lalu memperbarui semua tombol level dan penghitung bintang.
    /// Panggil ini setelah progres bintang berubah.
    /// </summary>
    public void RefreshAll()
    {
        currentTotalStars = LevelUnlockRules.GetTotalStars();

        if (totalStarsLabel != null)
        {
            totalStarsLabel.text = currentTotalStars.ToString();
        }

        if (levelButtons == null)
        {
            return;
        }

        foreach (LevelButtonView view in levelButtons)
        {
            if (view == null)
            {
                continue;
            }

            int earnedStars = LevelProgress.GetStars(view.LevelNumber);
            view.ApplyState(IsUnlocked(view), earnedStars);
        }
    }

    /// <summary>True jika total bintang pemain sudah memenuhi syarat level pada view tersebut.</summary>
    public bool IsUnlocked(LevelButtonView view)
    {
        return view != null && LevelUnlockRules.IsUnlocked(view.LevelNumber, currentTotalStars);
    }

    #endregion

    #region Internal Helpers

    private void RegisterButtonListeners()
    {
        if (levelButtons == null)
        {
            return;
        }

        foreach (LevelButtonView view in levelButtons)
        {
            if (view == null || view.Button == null)
            {
                continue;
            }

            // Listener dipasang dari kode supaya event OnClick tiap tombol di scene tidak perlu diisi manual.
            LevelButtonView capturedView = view;
            view.Button.onClick.AddListener(() => OnLevelClicked(capturedView));
        }
    }

    private void OnLevelClicked(LevelButtonView view)
    {
        if (view == null)
        {
            return;
        }

        if (!IsUnlocked(view))
        {
            if (lockedPopup != null)
            {
                lockedPopup.ShowLocked(
                    view.LevelNumber,
                    LevelUnlockRules.GetMissingStars(view.LevelNumber, currentTotalStars)
                );
            }

            return;
        }

        LoadLevelScene(view);
    }

    private void LoadLevelScene(LevelButtonView view)
    {
        if (string.IsNullOrWhiteSpace(view.SceneName))
        {
            Debug.LogWarning($"Level {view.LevelNumber} is unlocked but its Scene Name is still empty.", view);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(view.SceneName))
        {
            Debug.LogError($"Scene '{view.SceneName}' is missing from Scenes In Build, so Level {view.LevelNumber} cannot be opened.");
            return;
        }

        SceneManager.LoadScene(view.SceneName);
    }

    // Menambah satu bintang ke level terbuka tertinggi, supaya progres bisa dites tanpa menang level.
    private void AddDebugStar()
    {
        LevelButtonView target = null;

        if (levelButtons != null)
        {
            foreach (LevelButtonView view in levelButtons)
            {
                if (view == null || !IsUnlocked(view))
                {
                    continue;
                }

                if (target == null || view.LevelNumber > target.LevelNumber)
                {
                    target = view;
                }
            }
        }

        if (target == null)
        {
            return;
        }

        bool changed = LevelProgress.AwardStars(target.LevelNumber, LevelProgress.GetStars(target.LevelNumber) + 1, maxStarsPerLevel);
        RefreshAll();

        if (changed)
        {
            Debug.Log($"Debug: Level {target.LevelNumber} now has {LevelProgress.GetStars(target.LevelNumber)} stars.");
        }
    }

    #endregion
}
