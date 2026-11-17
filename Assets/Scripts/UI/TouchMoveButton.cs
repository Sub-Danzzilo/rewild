using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tombol tahan untuk jalan kiri/kanan di layar sentuh. Selama jari menekan, state jalan diteruskan
/// ke <see cref="TouchControlsView"/>; begitu jari dilepas, Webi berhenti.
///
/// Skala gambar sedikit mengecil saat ditekan sebagai umpan balik sentuh. Tombol dilepas lewat
/// <see cref="OnPointerUpHandler"/>: UGUI sudah menangkap pointer yang menekan, jadi melepas jari
/// di luar gambar tombol tetap menurunkan state-nya.
/// </summary>
[DisallowMultipleComponent]
public class TouchMoveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    #region Inspector Fields

    [Header("Behaviour")]
    [Tooltip("Arah gerak tombol ini: -1 = kiri, 1 = kanan.")]
    [SerializeField] private int direction = -1;

    [Tooltip("Skala gambar saat tombol ditekan, supaya terasa seperti tombol fisik. 1 = tanpa efek.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float pressedScale = 0.94f;

    [Tooltip("Kecepatan perubahan skala saat ditekan/dilepas (makin besar makin cepat).")]
    [Min(1f)]
    [SerializeField] private float scaleSpeed = 14f;

    #endregion

    #region Runtime State

    private TouchControlsView view;
    private Vector3 baseScale;
    private bool pressed;

    #endregion

    #region Unity Events

    private void Awake()
    {
        view = GetComponentInParent<TouchControlsView>();
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        Release();
    }

    private void Update()
    {
        // Jari yang masih menempel saat game di-pause tidak boleh meninggalkan tombol "ditekan".
        if (pressed && Time.timeScale <= 0f)
        {
            Release();
        }

        Vector3 target = pressed ? baseScale * pressedScale : baseScale;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            target,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    #endregion

    #region Event System Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        pressed = true;

        if (view != null)
        {
            view.SetMoveButton(direction, true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    #endregion

    #region Internal Helpers

    private void Release()
    {
        if (!pressed)
        {
            return;
        }

        pressed = false;

        if (view != null)
        {
            view.SetMoveButton(direction, false);
        }
    }

    #endregion
}
