using UnityEngine;

/// <summary>
/// Penghubung antara tombol layar sentuh dan gameplay. Tombol/analog di layar hanya menulis ke
/// sini, sedangkan <see cref="PlayerController"/> dan <see cref="CameraDragController"/> membacanya.
///
/// Dipisah dari komponen UI supaya script gameplay tidak bergantung pada tombol mana yang dipakai:
/// kalau tata letak tombol Android berubah, yang disentuh hanya prefab UI-nya.
///
/// Semua isinya milik scene yang sedang aktif. <see cref="TouchControlsView"/> memanggil
/// <see cref="SetOverlayActive"/> saat hidup/mati, jadi state tidak terbawa ke scene berikutnya.
/// </summary>
public static class TouchControlsInput
{
    #region State

    private static bool overlayActive;
    private static int moveDirection;
    private static bool aimHeld;
    private static bool hasAimDirection;
    private static Vector2 aimScreenDirection;
    private static bool aimStartRequested;
    private static bool shootRequested;
    private static bool aimCancelRequested;
    private static Vector2 pendingCameraDrag;

    #endregion

    #region Public Properties

    /// <summary>
    /// True kalau overlay kontrol sentuh hidup di scene ini. Kalau false, gameplay memakai
    /// keyboard/mouse seperti di build Windows.
    /// </summary>
    public static bool Active => overlayActive;

    /// <summary>Arah jalan dari tombol layar: -1 kiri, 0 diam, 1 kanan.</summary>
    public static int MoveDirection => moveDirection;

    /// <summary>True selama analog bidik masih menempel di jari.</summary>
    public static bool AimHeld => aimHeld;

    /// <summary>True kalau analog sudah cukup jauh dari pusat sehingga arahnya sah dipakai.</summary>
    public static bool HasAimDirection => hasAimDirection;

    #endregion

    #region Overlay Lifetime

    /// <summary>Dipanggil <see cref="TouchControlsView"/> saat overlay hidup atau mati.</summary>
    public static void SetOverlayActive(bool active)
    {
        overlayActive = active;

        if (!active)
        {
            ResetAll();
        }
    }

    /// <summary>Mengosongkan semua state. Dipakai saat overlay dimatikan atau scene berganti.</summary>
    public static void ResetAll()
    {
        moveDirection = 0;
        aimHeld = false;
        hasAimDirection = false;
        aimScreenDirection = Vector2.zero;
        aimStartRequested = false;
        shootRequested = false;
        aimCancelRequested = false;
        pendingCameraDrag = Vector2.zero;
    }

    #endregion

    #region Movement

    /// <summary>Menyetel arah jalan. Nilai di luar -1..1 dijepit.</summary>
    public static void SetMoveDirection(int direction)
    {
        moveDirection = Mathf.Clamp(direction, -1, 1);
    }

    #endregion

    #region Aiming

    /// <summary>Jari mulai menekan analog bidik. Gameplay memakai ini sebagai "F ditekan".</summary>
    public static void BeginAim()
    {
        aimHeld = true;
        aimStartRequested = true;
        shootRequested = false;
        aimCancelRequested = false;

        // Arah lama dibuang: mengetuk analog tanpa menggeser berarti belum mengarah ke mana pun,
        // bukan mengulang arah tembakan sebelumnya.
        ClearAimDirection();
    }

    /// <summary>
    /// Arah bidik dalam koordinat layar (x kanan, y atas), biasanya hasil geser jari dari pusat
    /// analog. Panjangnya tidak dipakai, jadi boleh vektor apa pun yang bukan nol.
    /// </summary>
    public static void SetAimDirection(Vector2 screenDirection)
    {
        if (screenDirection.sqrMagnitude <= 0.0001f)
        {
            ClearAimDirection();
            return;
        }

        aimScreenDirection = screenDirection.normalized;
        hasAimDirection = true;
    }

    /// <summary>Menghapus arah bidik. Gameplay kembali memakai arah badan Webi.</summary>
    public static void ClearAimDirection()
    {
        hasAimDirection = false;
        aimScreenDirection = Vector2.zero;
    }

    /// <summary>
    /// Jari dilepas. Kalau dilepas di atas area silang (atau arahnya belum sah), bidikan batal;
    /// selain itu gameplay akan menembakkan jaring ke arah terakhir.
    /// </summary>
    public static void ReleaseAim(bool overCancelZone)
    {
        aimHeld = false;

        if (overCancelZone || !hasAimDirection)
        {
            aimCancelRequested = true;
            ClearAimDirection();
            return;
        }

        shootRequested = true;
    }

    /// <summary>
    /// Mengosongkan permintaan satu-frame (mulai bidik, tembak, batal) tanpa mengubah tombol yang
    /// sedang ditahan. Dipakai saat gerak Webi dikunci, supaya permintaan yang masuk selama intro
    /// kamera tidak langsung dieksekusi begitu kunci dibuka.
    /// </summary>
    public static void ClearRequests()
    {
        aimStartRequested = false;
        shootRequested = false;
        aimCancelRequested = false;
    }

    /// <summary>True sekali saat analog bidik baru ditekan.</summary>
    public static bool ConsumeAimStart()
    {
        bool requested = aimStartRequested;
        aimStartRequested = false;
        return requested;
    }

    /// <summary>True sekali saat analog bidik dilepas untuk menembak.</summary>
    public static bool ConsumeShoot()
    {
        bool requested = shootRequested;
        shootRequested = false;
        return requested;
    }

    /// <summary>True sekali saat bidikan dibatalkan dari analog (dilepas di area silang).</summary>
    public static bool ConsumeAimCancel()
    {
        bool requested = aimCancelRequested;
        aimCancelRequested = false;
        return requested;
    }

    /// <summary>
    /// Mengubah arah bidik layar menjadi arah dunia. Kamera level tidak diputar, jadi hasilnya
    /// sama dengan arah layar; perhitungan lewat transform kamera supaya tetap benar kalau nanti
    /// kamera miring.
    /// </summary>
    public static Vector2 ToWorldDirection(Camera camera)
    {
        if (camera == null)
        {
            return aimScreenDirection;
        }

        Vector3 direction = camera.transform.right * aimScreenDirection.x +
                            camera.transform.up * aimScreenDirection.y;
        return new Vector2(direction.x, direction.y).normalized;
    }

    #endregion

    #region Camera Drag

    /// <summary>Menambah delta geser kamera dari jari (satuan piksel layar).</summary>
    public static void AddCameraDrag(Vector2 deltaPixels)
    {
        pendingCameraDrag += deltaPixels;
    }

    /// <summary>
    /// Mengambil delta geser yang terkumpul sejak frame sebelumnya. Mengembalikan false kalau
    /// tidak ada geseran, jadi kamera tidak perlu dihitung ulang tiap frame.
    /// </summary>
    public static bool ConsumeCameraDrag(out Vector2 deltaPixels)
    {
        deltaPixels = pendingCameraDrag;
        pendingCameraDrag = Vector2.zero;
        return deltaPixels.sqrMagnitude > 0.0001f;
    }

    #endregion
}
