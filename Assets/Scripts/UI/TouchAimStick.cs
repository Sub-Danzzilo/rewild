using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Analog bidik gaya MOBA: tahan pad di kanan bawah, geser ke arah target, lalu lepas untuk
/// menembak. Melepas jari di atas area silang (kanan atas) membatalkan bidikan tanpa menembak.
///
/// Arah dan statusnya diteruskan ke <see cref="TouchControlsInput"/>, jadi
/// <see cref="PlayerController"/> cukup membaca satu sumber itu untuk menggambar garis bidik dan
/// menembakkan jaring. Area silang sendiri diatur <see cref="TouchControlsView"/>: hanya muncul
/// selama analog ditahan.
/// </summary>
[DisallowMultipleComponent]
public class TouchAimStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Gambar knob yang ikut bergerak bersama jari. Kalau kosong, dipakai anak pertama pad.")]
    [SerializeField] private RectTransform knob;

    [Tooltip("Area silang untuk membatalkan bidikan. Jari yang dilepas di atas area ini tidak menembak.")]
    [SerializeField] private RectTransform cancelZone;

    [Tooltip("CanvasGroup bagian tali + jaring kecil. Bagian ini hanya muncul selama analog ditahan, " +
             "jadi saat diam tombolnya hanya menampilkan jaring besar.")]
    [SerializeField] private CanvasGroup tailGroup;

    [Tooltip("Kecepatan muncul/hilangnya bagian tali saat analog ditahan/dilepas (makin besar makin cepat).")]
    [Min(1f)]
    [SerializeField] private float tailFadeSpeed = 10f;

    [Tooltip("Canvas tempat UI ini berada. Kosongkan untuk mencari otomatis di induknya.")]
    [SerializeField] private Canvas canvas;

    [Header("Aim")]
    [Tooltip("Jarak maksimum knob dari pusat pad (satuan UI). Jari yang lebih jauh tetap dihitung " +
             "sejauh ini, jadi arahnya tidak berubah mendadak.")]
    [Min(10f)]
    [SerializeField] private float knobRadius = 88f;

    [Tooltip("Jarak minimum jari dari pusat pad sebelum arah bidik dianggap sah (satuan UI). Jari yang " +
             "masih di tengah berarti belum mengarah ke mana pun.")]
    [Min(0f)]
    [SerializeField] private float deadZone = 30f;

    [Tooltip("Kecepatan knob kembali ke tengah setelah jari dilepas.")]
    [Min(1f)]
    [SerializeField] private float knobReturnSpeed = 18f;

    [Header("Cancel Highlight")]
    [Tooltip("Perbesaran area silang saat jari berada di atasnya, sebagai tanda bidikan akan batal.")]
    [Min(1f)]
    [SerializeField] private float cancelHighlightScale = 1.08f;

    #endregion

    #region Runtime State

    private RectTransform padRect;
    private int activePointerId = -1;
    private Vector3 cancelBaseScale = Vector3.one;
    private Vector2 knobTarget;
    private Vector2 lastPointerPosition;
    private Camera lastEventCamera;
    private bool holding;
    private bool cancelHovered;

    #endregion

    #region Unity Events

    private void Awake()
    {
        padRect = transform as RectTransform;

        if (knob == null && transform.childCount > 0)
        {
            knob = transform.GetChild(0) as RectTransform;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (cancelZone != null)
        {
            cancelBaseScale = cancelZone.localScale;
        }
    }

    private void Update()
    {
        // Scene bisa di-pause saat jari masih menempel; jangan tinggalkan bidikan menggantung.
        if (holding && Time.timeScale <= 0f)
        {
            FinishAim(true);
        }

        UpdateKnob();
        UpdateTail();
        UpdateCancelHighlight();
    }

    #endregion

    #region Event System Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        holding = true;
        cancelHovered = false;

        TouchControlsInput.BeginAim();
        UpdateAim(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!holding || eventData.pointerId != activePointerId)
        {
            return;
        }

        UpdateAim(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!holding || eventData.pointerId != activePointerId)
        {
            return;
        }

        bool overCancelZone = IsOverCancelZone(eventData.position, eventData.pressEventCamera);
        FinishAim(overCancelZone);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Membaca posisi jari terhadap pusat pad: knob digeser (dibatasi radius) dan arahnya dikirim
    /// ke gameplay. Jari di dalam dead zone dianggap belum mengarah ke mana pun.
    /// </summary>
    private void UpdateAim(Vector2 screenPosition, Camera eventCamera)
    {
        if (padRect == null)
        {
            return;
        }

        lastPointerPosition = screenPosition;
        lastEventCamera = eventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                padRect,
                screenPosition,
                GetUiCamera(eventCamera),
                out Vector2 localPoint))
        {
            return;
        }

        float distance = localPoint.magnitude;
        if (distance > knobRadius)
        {
            localPoint *= knobRadius / distance;
        }

        knobTarget = localPoint;
        cancelHovered = IsOverCancelZone(screenPosition, eventCamera);

        if (distance >= deadZone)
        {
            TouchControlsInput.SetAimDirection(localPoint);
        }
        else
        {
            TouchControlsInput.ClearAimDirection();
        }
    }

    private void FinishAim(bool cancelled)
    {
        holding = false;
        activePointerId = -1;
        cancelHovered = false;
        knobTarget = Vector2.zero;

        TouchControlsInput.ReleaseAim(cancelled);
    }

    private void UpdateKnob()
    {
        if (knob == null)
        {
            return;
        }

        if (holding)
        {
            knob.anchoredPosition = knobTarget;
        }
        else
        {
            knob.anchoredPosition = Vector2.MoveTowards(
                knob.anchoredPosition,
                Vector2.zero,
                knobRadius * knobReturnSpeed * Time.unscaledDeltaTime
            );
        }

        // Knob diputar mengikuti arah bidik supaya ujung jaring terlihat menunjuk target.
        Quaternion targetRotation = holding && knobTarget.sqrMagnitude > 0.01f
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(knobTarget.y, knobTarget.x) * Mathf.Rad2Deg)
            : Quaternion.identity;
        knob.localRotation = Quaternion.Slerp(
            knob.localRotation,
            targetRotation,
            Time.unscaledDeltaTime * 18f
        );
    }

    /// <summary>
    /// Memunculkan bagian tali + jaring kecil hanya selama analog ditahan. Saat diam tombolnya
    /// hanya berupa jaring besar, jadi terlihat lebih bersih di layar level.
    /// </summary>
    private void UpdateTail()
    {
        if (tailGroup == null)
        {
            return;
        }

        float target = holding ? 1f : 0f;
        tailGroup.alpha = Mathf.MoveTowards(
            tailGroup.alpha,
            target,
            tailFadeSpeed * Time.unscaledDeltaTime
        );
    }

    private void UpdateCancelHighlight()
    {
        if (cancelZone == null)
        {
            return;
        }

        Vector3 targetScale = cancelHovered ? cancelBaseScale * cancelHighlightScale : cancelBaseScale;
        cancelZone.localScale = Vector3.Lerp(
            cancelZone.localScale,
            targetScale,
            Time.unscaledDeltaTime * 16f
        );
    }

    private bool IsOverCancelZone(Vector2 screenPosition, Camera eventCamera)
    {
        if (cancelZone == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            cancelZone,
            screenPosition,
            GetUiCamera(eventCamera)
        );
    }

    /// <summary>
    /// Kamera yang dipakai UGUI untuk membaca koordinat layar. Overlay memakai null; mode lain
    /// memakai kamera canvas-nya (atau kamera event kalau canvas belum mengisinya).
    /// </summary>
    private Camera GetUiCamera(Camera eventCamera)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : eventCamera;
    }

    #endregion
}
