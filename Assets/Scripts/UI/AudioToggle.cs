using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AudioToggle : MonoBehaviour
{
    #region Enums

    public enum AudioChannel
    {
        Music,
        Sfx
    }

    #endregion

    #region Inspector Fields

    [Header("Toggle Settings")]
    [Tooltip("Audio yang dikontrol tombol ini: musik atau SFX.")]
    [SerializeField] private AudioChannel channel = AudioChannel.Music;

    [Header("Icon Settings")]
    [Tooltip("Objek ikon yang tampil saat audio menyala (misalnya MusicOn).")]
    [SerializeField] private GameObject onIcon;

    [Tooltip("Objek ikon yang tampil saat audio dimatikan (misalnya MusicOff).")]
    [SerializeField] private GameObject offIcon;

    #endregion

    #region Runtime State

    private Button buttonComponent;

    #endregion

    #region Unity Events

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Event klik dipasang otomatis, jadi On Click () di Button tidak perlu diisi manual.
        buttonComponent.onClick.AddListener(ToggleAudio);
        RefreshIcon();
    }

    private void OnDisable()
    {
        buttonComponent.onClick.RemoveListener(ToggleAudio);
    }

    #endregion

    #region Internal Helpers

    private void ToggleAudio()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioToggle needs an AudioManager in the first scene.");
            return;
        }

        if (channel == AudioChannel.Music)
        {
            AudioManager.Instance.SetMusicMuted(!AudioManager.Instance.IsMusicMuted);
        }
        else
        {
            AudioManager.Instance.SetSfxMuted(!AudioManager.Instance.IsSfxMuted);
        }

        RefreshIcon();
    }

    private void RefreshIcon()
    {
        bool muted = false;

        if (AudioManager.Instance != null)
        {
            muted = channel == AudioChannel.Music
                ? AudioManager.Instance.IsMusicMuted
                : AudioManager.Instance.IsSfxMuted;
        }

        if (onIcon != null)
        {
            onIcon.SetActive(!muted);
        }

        if (offIcon != null)
        {
            offIcon.SetActive(muted);
        }
    }

    #endregion
}
