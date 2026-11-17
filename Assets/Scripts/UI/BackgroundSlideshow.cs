using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundSlideshow : MonoBehaviour
{
    #region Inspector Fields

    [Header("Layer References")]
    [Tooltip("Image lapisan depan yang menampilkan background yang sedang aktif.")]
    [SerializeField] private Image imageA;

    [Tooltip("Image lapisan belakang. Dipakai sebagai lapisan yang fade masuk saat ganti background.")]
    [SerializeField] private Image imageB;

    [Tooltip("Daftar background yang diputar bergantian secara loop. Urutan mengikuti isi array.")]
    [SerializeField] private Sprite[] backgrounds;

    [Header("Timing")]
    [Tooltip("Durasi crossfade antar background dalam detik.")]
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;

    [Tooltip("Lama satu background tampil penuh sebelum pindah ke background berikutnya, dalam detik.")]
    [SerializeField, Min(0f)] private float holdDuration = 5f;

    [Tooltip("Jika aktif, slideshow dimulai dari background acak, bukan selalu dari urutan pertama.")]
    [SerializeField] private bool startRandom = false;

    #endregion

    #region Runtime State

    private Image frontLayer;
    private Image backLayer;
    private int currentIndex;

    #endregion

    #region Unity Events

    private void Awake()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        frontLayer = imageA;
        backLayer = imageB;

        currentIndex = startRandom ? Random.Range(0, backgrounds.Length) : 0;

        SetSprite(frontLayer, backgrounds[currentIndex], 1f);
        SetSprite(backLayer, backgrounds[currentIndex], 0f);
    }

    private IEnumerator Start()
    {
        // Satu background saja berarti tidak ada yang perlu di-loop.
        if (backgrounds.Length < 2)
        {
            yield break;
        }

        while (true)
        {
            // Pakai waktu real-time supaya slideshow tetap jalan saat timeScale dimodifikasi.
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return CrossFadeToNext();
        }
    }

    #endregion

    #region Internal Helpers

    private IEnumerator CrossFadeToNext()
    {
        int nextIndex = (currentIndex + 1) % backgrounds.Length;
        SetSprite(backLayer, backgrounds[nextIndex], 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;

            SetAlpha(frontLayer, 1f - progress);
            SetAlpha(backLayer, progress);

            yield return null;
        }

        SetAlpha(frontLayer, 0f);
        SetAlpha(backLayer, 1f);

        // Tukar peran layer supaya background aktif selalu berada di lapisan depan.
        Image previousFront = frontLayer;
        frontLayer = backLayer;
        backLayer = previousFront;

        currentIndex = nextIndex;
    }

    private void SetSprite(Image layer, Sprite sprite, float alpha)
    {
        if (layer == null)
        {
            return;
        }

        layer.sprite = sprite;
        SetAlpha(layer, alpha);
    }

    private void SetAlpha(Image layer, float alpha)
    {
        if (layer == null)
        {
            return;
        }

        Color color = layer.color;
        color.a = alpha;
        layer.color = color;
    }

    private bool ValidateSetup()
    {
        if (imageA == null || imageB == null)
        {
            Debug.LogError($"{nameof(BackgroundSlideshow)} on {name} needs both image layers assigned.", this);
            return false;
        }

        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogError($"{nameof(BackgroundSlideshow)} on {name} needs at least one background sprite.", this);
            return false;
        }

        if (fadeDuration < 0f || holdDuration < 0f)
        {
            Debug.LogError(
                $"{nameof(BackgroundSlideshow)} on {name} requires non-negative fade and hold durations.",
                this
            );
            return false;
        }

        return true;
    }

    #endregion
}
