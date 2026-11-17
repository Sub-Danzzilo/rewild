using UnityEngine;

/// <summary>
/// Akar overlay kontrol layar sentuh. Mengatur kapan tombol muncul, seberapa transparan saat diam
/// vs saat dipakai, dan meneruskan state tombol ke <see cref="TouchControlsInput"/>.
///
/// Prefab <c>Resources/UI/Touch Controls</c> dimuat otomatis oleh <see cref="LevelHudController"/>
/// di setiap scene level, jadi sepuluh scene level tidak perlu menyimpan salinan UI yang sama.
/// Semua angka tata letak dan transparansi diatur di prefab/Inspector, bukan di script lain.
/// </summary>
[DisallowMultipleComponent]
public class TouchControlsView : MonoBehaviour
{
    #region Inspector Fields

    [Header("Visibility")]
    [Tooltip("Tampilkan kontrol sentuh di build Windows/Mac/Linux. Biasanya mati karena di sana " +
             "pemain memakai keyboard dan mouse.")]
    [SerializeField] private bool showOnDesktop = false;

    [Tooltip("Tampilkan kontrol sentuh di Editor supaya bisa dicoba memakai mouse tanpa build Android.")]
    [SerializeField] private bool showInEditor = true;

    [Header("Transparency")]
    [Tooltip("Transparansi tombol saat tidak disentuh. 0,4 = cukup samar agar level tetap terlihat, " +
             "tapi tombolnya masih terbaca.")]
    [Range(0f, 1f)]
    [SerializeField] private float idleAlpha = 0.4f;

    [Tooltip("Transparansi tombol saat ditekan/dipakai. Sengaja tidak 1 supaya level di bawah tombol " +
             "tetap kelihatan.")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 0.9f;

    [Tooltip("Lama perubahan transparansi dari diam ke aktif (detik, waktu nyata).")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.12f;

    [Header("Groups")]
    [Tooltip("CanvasGroup berisi tombol jalan kiri/kanan.")]
    [SerializeField] private CanvasGroup moveGroup;

    [Tooltip("CanvasGroup berisi analog bidik.")]
    [SerializeField] private CanvasGroup aimGroup;

    [Tooltip("CanvasGroup berisi area batal bidik. Hanya terlihat selama pemain membidik.")]
    [SerializeField] private CanvasGroup cancelGroup;

    #endregion

    #region Runtime State

    private int heldMoveDirection;

    #endregion

    #region Unity Events

    private void Awake()
    {
        bool visible = Application.isEditor
            ? showInEditor
            : (Application.isMobilePlatform || showOnDesktop);

        if (!visible)
        {
            gameObject.SetActive(false);
            return;
        }

        // Mulai dari transparan supaya tombol tidak "berkedip" pekat di frame pertama.
        SetGroupAlpha(moveGroup, 0f);
        SetGroupAlpha(aimGroup, 0f);
        SetGroupAlpha(cancelGroup, 0f);
    }

    private void OnEnable()
    {
        heldMoveDirection = 0;
        TouchControlsInput.SetOverlayActive(true);
    }

    private void OnDisable()
    {
        heldMoveDirection = 0;
        TouchControlsInput.SetOverlayActive(false);
    }

    private void Update()
    {
        // Saat game di-pause (timeScale 0) tombol disembunyikan total: yang dipegang pemain popup,
        // bukan tombol level.
        bool hidden = Time.timeScale <= 0f;

        float moveTarget = hidden ? 0f : (heldMoveDirection != 0 ? activeAlpha : idleAlpha);
        float aimTarget = hidden ? 0f : (TouchControlsInput.AimHeld ? activeAlpha : idleAlpha);
        float cancelTarget = hidden || !TouchControlsInput.AimHeld ? 0f : activeAlpha;

        FadeGroup(moveGroup, moveTarget, hidden);
        FadeGroup(aimGroup, aimTarget, hidden);

        // Area batal bukan tombol: alpha-nya saja yang berubah, raycast tetap dimatikan.
        FadeGroup(cancelGroup, cancelTarget, true);
    }

    #endregion

    #region Public Methods

    /// <summary>Dipanggil tombol kiri/kanan saat jari menekan atau melepas.</summary>
    public void SetMoveButton(int direction, bool held)
    {
        if (held)
        {
            heldMoveDirection = Mathf.Clamp(direction, -1, 1);
        }
        else if (heldMoveDirection == direction)
        {
            heldMoveDirection = 0;
        }

        if (Time.timeScale > 0f)
        {
            TouchControlsInput.SetMoveDirection(heldMoveDirection);
        }
    }

    #endregion

    #region Internal Helpers

    private void FadeGroup(CanvasGroup group, float target, bool blockInteraction)
    {
        if (group == null)
        {
            return;
        }

        float step = fadeDuration <= 0.0001f ? 1f : Time.unscaledDeltaTime / fadeDuration;
        group.alpha = Mathf.MoveTowards(group.alpha, target, step);

        bool solid = !blockInteraction && group.alpha > 0.05f;
        group.blocksRaycasts = solid;
        group.interactable = solid;
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0.05f;
        group.interactable = alpha > 0.05f;
    }

    #endregion
}
