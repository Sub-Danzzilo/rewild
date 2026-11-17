using TMPro;
using UnityEngine;

/// <summary>
/// Popup yang muncul saat pemain mengklik level yang masih terkunci. Isinya pesan berapa bintang
/// lagi yang dibutuhkan. Popup ini memakai UIPopupSlide supaya animasi buka/tutupnya sama dengan
/// popup lain di Home Screen.
/// </summary>
[DisallowMultipleComponent]
public class LockedLevelPopup : MonoBehaviour
{
    #region Inspector Fields

    [Header("Popup References")]
    [Tooltip("Pengatur animasi popup. Jika kosong, akan diambil otomatis dari objek ini.")]
    [SerializeField] private UIPopupSlide popupSlide;

    [Tooltip("Teks pesan yang menampilkan nomor level dan jumlah bintang yang kurang.")]
    [SerializeField] private TMP_Text messageLabel;

    [Header("Message Format")]
    [Tooltip("Format pesan popup. {0} = nomor level, {1} = jumlah bintang yang kurang, {2} = akhiran 's' untuk bentuk jamak.")]
    [SerializeField] private string messageFormat = "Level {0} is locked.\nYou need {1} more star{2} to unlock it.";

    #endregion

    #region Unity Events

    private void Awake()
    {
        if (popupSlide == null)
        {
            popupSlide = GetComponent<UIPopupSlide>();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Menampilkan popup untuk level terkunci beserta jumlah bintang yang masih kurang.</summary>
    public void ShowLocked(int levelNumber, int missingStars)
    {
        if (messageLabel != null)
        {
            missingStars = Mathf.Max(1, missingStars);
            messageLabel.text = string.Format(messageFormat, levelNumber, missingStars, missingStars == 1 ? "" : "s");
        }

        if (popupSlide != null)
        {
            popupSlide.Show();
        }
    }

    /// <summary>Menutup popup. Dipakai tombol OK di dalam popup.</summary>
    public void Hide()
    {
        if (popupSlide != null)
        {
            popupSlide.Hide();
        }
    }

    #endregion
}
