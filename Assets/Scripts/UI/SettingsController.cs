using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Slider References")]
    [Tooltip("Slider untuk volume musik. Nilai awalnya diambil dari AudioManager saat scene mulai.")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("Slider untuk volume SFX (efek suara). Nilai awalnya diambil dari AudioManager saat scene mulai.")]
    [SerializeField] private Slider sfxSlider;

    #endregion

    #region Unity Events

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("SettingsController needs an AudioManager in the first scene.");
            return;
        }

        // SetValueWithoutNotify supaya slider tidak memicu OnValueChanged saat nilainya disiapkan.
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Dipanggil dari event OnValueChanged pada slider musik.</summary>
    public void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    /// <summary>Dipanggil dari event OnValueChanged pada slider SFX.</summary>
    public void SetSfxVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(volume);
        }
    }

    #endregion
}
