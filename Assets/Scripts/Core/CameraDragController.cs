using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

/// <summary>
/// Menggeser kamera ortografis dengan tahan-klik-kanan lalu geser mouse. Batas gesernya diambil
/// dari seluruh isi level (Tilemap platform), jadi platform yang berada di luar gambar background
/// tetap bisa dilihat.
///
/// Zoom kamera sengaja konstan (ukuran ortografis tidak berubah saat menggeser). Satu-satunya
/// zoom in adalah saat fokus menanam benih lewat FocusOn/ReleaseFocus.
///
/// Gambar latar tidak diperbesar atau digeser. Kalau area level lebih lebar/tinggi dari gambar,
/// <see cref="LevelBackgroundExtension"/> (dibuat otomatis saat runtime) mengulang gambar ke
/// samping dan menambah gradasi langit/tanah, jadi framing desain awal tetap sama.
///
/// UI (HUD) sengaja tidak ikut terpengaruh: canvas HUD memakai Screen Space - Overlay,
/// jadi tombol pause dan penghitung moves selalu menempel di layar berapa pun kamera digeser.
/// </summary>
public class CameraDragController : MonoBehaviour
{
    #region Nested Types

    /// <summary>Tombol mouse yang dipakai untuk menggeser kamera.</summary>
    public enum DragButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    #endregion

    #region Inspector Fields

    [Header("References")]
    [Tooltip("PlayerController yang dipantau. Selama pemain membidik (aiming), kamera tidak bisa digeser.")]
    [SerializeField] private PlayerController playerController;

    [Header("Drag Input")]
    [Tooltip("Tombol mouse untuk menggeser kamera. Klik kanan dipakai supaya klik kiri tetap bebas untuk membidik.")]
    [SerializeField] private DragButton dragButton = DragButton.Right;

    [Header("Drag Settings")]
    [Tooltip("Pengali kecepatan geser kamera. 1 = mengikuti gerakan mouse apa adanya.")]
    [SerializeField] private float dragSpeed = 1f;

    [Tooltip("Waktu penghalusan gerakan kamera saat digeser. Makin kecil, makin responsif.")]
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Focus Settings")]
    [Tooltip("Waktu penghalusan saat kamera dipaksa fokus ke satu titik (misalnya saat menanam benih).")]
    [SerializeField] private float focusSmoothTime = 0.35f;

    [Tooltip("Jika aktif, titik fokus tetap dikunci di dalam area level. Saat menanam benih, " +
             "matikan opsi ini supaya kamera bisa mendekat ke tumpukan tanah yang ada di tepi level.")]
    public bool clampFocusToLevelBounds = false;

    [Tooltip("Kamera saat fokus/zoom in hanya boleh menampilkan area yang sudah terlihat sebelum fokus. " +
             "Dengan begitu zoom in tidak pernah memperlihatkan area kosong di luar gambar latar level.")]
    public bool keepFocusInsideCurrentView = true;

    [Header("Focus Zoom Settings")]
    [Tooltip("Apakah kamera akan melakukan zoom in saat mode fokus aktif (misalnya saat benih tumbuh di tanah)?")]
    public bool enableFocusZoom = true;

    [Tooltip("Ukuran ortografis kamera saat fokus/zoom in (makin kecil nilainya, makin dekat zoom-nya). Default normal kamera = 12.")]
    public float focusOrthographicSize = 6f;

    [Tooltip("Waktu penghalusan perubahan zoom in/out kamera (dalam detik).")]
    public float zoomSmoothTime = 0.4f;

    [Header("Teleport Focus")]
    [Tooltip("Jika aktif, kamera ikut pindah ke posisi Webi setelah teleport (misalnya lewat portal yang jauh), " +
             "lalu kembali ke kendali geser seperti biasa.")]
    public bool focusCameraOnTeleport = true;

    [Tooltip("Lama kamera bergerak menuju Webi setelah teleport (detik). Kamera dijamin sampai tepat di " +
             "Webi saat waktu ini habis, lalu geser kamera aktif lagi. Untuk teleport yang jauh, naikkan " +
             "nilai ini supaya gerakannya tidak terlalu cepat.")]
    public float teleportFocusDuration = 0.45f;

    [Tooltip("Kalau aktif, kamera mendeteksi sendiri saat Webi berpindah mendadak (mis. lewat portal atau respawn) " +
             "lalu menyusul ke posisi Webi. Pengaman kalau teleport terjadi tanpa memanggil fokus kamera.")]
    public bool followTeleportJumps = true;

    [Tooltip("Jarak perpindahan Webi dalam satu frame (unit) yang dianggap teleport. Jalan biasa hanya beberapa " +
             "persepuluh unit, jadi 3 aman.")]
    public float teleportJumpThreshold = 3f;

    [Header("Level Bounds")]
    [Tooltip("Gambar latar dunia (Background (World)). Dipakai sebagai sumber batas cadangan kalau " +
             "level tidak punya Tilemap, dan sebagai sumber pola/warna untuk perluasan latar.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("Jika aktif, area level diisi dari ukuran background saat belum ada batas dari isi level.")]
    [SerializeField] private bool useBackgroundAsBounds = true;

    [Tooltip("Area level dalam koordinat dunia (x = kiri, y = bawah, width/height = ukuran). " +
             "Dipakai hanya kalau Use Background As Bounds dimatikan.")]
    [SerializeField] private Rect levelBounds;

    [Header("Level Content Bounds")]
    [Tooltip("Jika aktif, batas geser kamera diambil dari seluruh isi level (semua Tilemap platform), " +
             "bukan cuma dari gambar background. Platform yang keluar dari background tetap bisa dijangkau kamera.")]
    [SerializeField] private bool useLevelContentAsBounds = true;

    [Tooltip("Tambahan ruang kosong di sekitar isi level (unit dunia), supaya kamera tidak berhenti " +
             "persis di ujung platform.")]
    [SerializeField] private float contentPadding = 1.5f;

    [Tooltip("Opsional. Kalau diisi, hanya isi level di bawah objek ini yang dihitung sebagai batas kamera. " +
             "Kosongkan untuk memakai semua Tilemap yang ada di scene.")]
    [SerializeField] private Transform levelContentRoot;

    [Header("Background Extension")]
    [Tooltip("Jika aktif, gambar latar diperluas otomatis mengikuti area level: ke samping dengan mengulang " +
             "gambar (dicerminkan) dan ke atas/bawah dengan gradasi langit serta tanah. Gambar asli tidak " +
             "diubah ukurannya, jadi framing desain tetap sama.")]
    [SerializeField] private bool extendBackground = true;

    [Tooltip("Jika aktif, gambar yang diulang ke samping dicerminkan supaya sambungannya tidak terlihat.")]
    [SerializeField] private bool mirrorBackgroundTiles = true;

    #endregion

    #region Runtime State

    private Camera targetCamera;
    private Vector3 targetPosition;
    private Vector3 focusPosition;
    private Vector3 smoothVelocity;
    private bool wasAiming;
    private bool isFocusing;
    private float defaultOrthographicSize;
    private float targetOrthographicSize;
    private float zoomVelocity;
    private Rect focusViewArea;
    private Vector3 authoredPosition;
    private LevelBackgroundExtension backgroundExtension;
    private bool scriptedControl;
    private Vector3 teleportFocusPosition;
    private Vector3 teleportFocusStartPosition;
    private float teleportFocusElapsed;
    private float teleportFocusDurationTotal;
    private Transform teleportFocusTarget;
    private PlayerController teleportWatchPlayer;
    private Vector3 lastPlayerPosition;
    private bool hasLastPlayerPosition;

    #endregion

    #region Unity Events

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            defaultOrthographicSize = targetCamera.orthographicSize;
            targetOrthographicSize = defaultOrthographicSize;
        }

        // Posisi kamera dari scene dipakai sebagai patokan untuk sumbu yang tidak perlu digeser
        // (isinya lebih kecil dari layar), supaya framing desain awal tidak berubah.
        authoredPosition = transform.position;
        ApplyBackgroundBounds();
        if (useLevelContentAsBounds)
        {
            ApplyLevelContentBounds();
        }

        targetPosition = ClampToBounds(transform.position);
    }

    private void Start()
    {
        // Tilemap level bisa baru selesai dibangun di OnEnable objek lain, jadi batas isi level
        // dihitung ulang di Start sebelum kamera dipakai.
        if (useLevelContentAsBounds)
        {
            ApplyLevelContentBounds();
        }

        SetUpBackgroundExtension();

        // Pemantau perpindahan Webi untuk fokus teleport otomatis. Dipisah dari playerController karena
        // referensi itu boleh kosong (hanya dipakai untuk memblokir drag saat membidik).
        teleportWatchPlayer = playerController != null
            ? playerController
            : FindFirstObjectByType<PlayerController>();

        targetPosition = ClampToBounds(transform.position);
        transform.position = targetPosition;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        // Mode script (intro kamera awal level): posisi dan zoom kamera diatur sepenuhnya
        // dari luar, jadi input geser dan pengembalian zoom diabaikan sampai mode dilepas.
        if (scriptedControl)
        {
            return;
        }

        // Deteksi teleport dari jalur mana pun (portal, respawn, script lain), bukan hanya dari PortalController.
        DetectPlayerTeleportJump();

        // Setelah teleport: kamera mengejar posisi Webi sebentar, lalu kendali geser aktif lagi
        // dari posisi terakhirnya (bukan meloncat balik ke tampilan awal level).
        if (teleportFocusElapsed < teleportFocusDurationTotal)
        {
            teleportFocusElapsed += Time.unscaledDeltaTime;

            // Target dicek ulang tiap frame supaya kamera tetap mengikuti Webi kalau ia langsung
            // berjalan setelah mendarat, bukan hanya menuju posisi saat teleport terjadi.
            Vector3 rawTarget = teleportFocusTarget != null
                ? teleportFocusTarget.position
                : teleportFocusPosition;
            Vector3 teleportTarget = GetTeleportFocusPosition(rawTarget);

            // Interpolasi dengan easing (bukan SmoothDamp) supaya di akhir durasi kamera benar-benar
            // sampai di Webi. SmoothDamp selalu menyisakan jarak, jadi untuk teleport jauh kamera
            // terlihat berhenti di tengah jalan.
            float progress = Mathf.Clamp01(teleportFocusElapsed / teleportFocusDurationTotal);
            float easedProgress = progress * progress * (3f - 2f * progress);
            transform.position = Vector3.Lerp(
                teleportFocusStartPosition,
                teleportTarget,
                easedProgress
            );

            if (Mathf.Abs(targetCamera.orthographicSize - defaultOrthographicSize) > 0.01f)
            {
                targetCamera.orthographicSize = Mathf.SmoothDamp(
                    targetCamera.orthographicSize,
                    defaultOrthographicSize,
                    ref zoomVelocity,
                    zoomSmoothTime
                );
            }

            if (progress >= 1f)
            {
                teleportFocusTarget = null;
                ExpandLevelBoundsTo(transform.position);
                targetPosition = ClampToBounds(transform.position);
                wasAiming = false;
            }

            return;
        }

        // Mode fokus: kamera hanya bergerak menuju titik fokus, input geser diabaikan.
        if (isFocusing)
        {
            // Fokus sengaja tidak ikut dibatasi area level, karena titik fokus (misalnya tumpukan
            // tanah) sering berada di tepi level. Kalau dibatasi, kamera tertahan di tengah level
            // dan terlihat seperti tidak mau fokus sama sekali. Pengamannya diganti: kamera tetap
            // dikurung di dalam area yang sudah tampil sebelum fokus, jadi zoom in tidak membuka
            // area baru di luar gambar latar level.
            targetPosition = ClampFocusPosition(focusPosition);
            transform.position = ClampFocusPosition(Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref smoothVelocity,
                focusSmoothTime
            ));

            if (enableFocusZoom)
            {
                targetCamera.orthographicSize = Mathf.SmoothDamp(
                    targetCamera.orthographicSize,
                    targetOrthographicSize,
                    ref zoomVelocity,
                    zoomSmoothTime
                );
            }
            return;
        }

        // Jika tidak sedang fokus, kembalikan ukuran zoom secara halus ke ukuran semula
        if (Mathf.Abs(targetCamera.orthographicSize - defaultOrthographicSize) > 0.01f)
        {
            targetCamera.orthographicSize = Mathf.SmoothDamp(
                targetCamera.orthographicSize,
                defaultOrthographicSize,
                ref zoomVelocity,
                zoomSmoothTime
            );
        }

        // Analog bidik Android ikut mengunci kamera, sama seperti membidik dengan mouse.
        bool isAiming = (playerController != null && playerController.IsAiming) ||
                        TouchControlsInput.AimHeld;
        if (isAiming)
        {
            wasAiming = true;
            targetPosition = transform.position;
            return;
        }

        // Frame pertama setelah berhenti membidik: kunci posisi supaya kamera tidak meloncat.
        if (wasAiming)
        {
            wasAiming = false;
            targetPosition = transform.position;
            return;
        }

        // Layar sentuh: delta dari area kosong layar (di luar tombol dan HUD) menggeser kamera.
        if (TouchControlsInput.ConsumeCameraDrag(out Vector2 touchDelta) && targetCamera.pixelHeight > 0)
        {
            float worldUnitsPerTouchPixel = targetCamera.orthographicSize * 2f / targetCamera.pixelHeight;
            Vector3 touchMovement = new Vector3(
                -touchDelta.x * worldUnitsPerTouchPixel,
                -touchDelta.y * worldUnitsPerTouchPixel,
                0f
            );

            targetPosition = ClampToBounds(targetPosition + touchMovement * dragSpeed);
        }

        // Windows tetap memakai tombol mouse yang diatur per level.
        ButtonControl dragControl = Mouse.current != null ? GetDragControl() : null;
        if (dragControl != null)
        {
            if (dragControl.wasPressedThisFrame)
            {
                targetPosition = transform.position;
            }

            if (dragControl.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                float worldUnitsPerPixel = targetCamera.orthographicSize * 2f / targetCamera.pixelHeight;
                Vector3 movement = new Vector3(
                    -mouseDelta.x * worldUnitsPerPixel,
                    -mouseDelta.y * worldUnitsPerPixel,
                    0f
                );

                targetPosition = ClampToBounds(targetPosition + movement * dragSpeed);
            }
        }

        transform.position = ClampToBounds(Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref smoothVelocity,
            smoothTime
        ));
    }

    private void OnDrawGizmosSelected()
    {
        // Di Edit Mode, batas isi level belum tersimpan ke levelBounds, jadi dihitung langsung
        // supaya kotak hijau bisa dipakai sebagai panduan saat mendesain level baru.
        if (!Application.isPlaying && TryGetLevelContentBounds(out Rect contentBounds))
        {
            Gizmos.color = new Color(0.25f, 0.85f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(
                contentBounds.center,
                new Vector3(contentBounds.width, contentBounds.height, 0f)
            );
        }

        if (useBackgroundAsBounds && backgroundRenderer != null)
        {
            Gizmos.color = new Color(0.25f, 0.65f, 1f, 0.5f);
            Gizmos.DrawWireCube(backgroundRenderer.bounds.center, backgroundRenderer.bounds.size);
        }

        if (levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.85f, 0.25f, 0.8f);
        Gizmos.DrawWireCube(levelBounds.center, new Vector3(levelBounds.width, levelBounds.height, 0f));
    }

    #endregion

    #region Public Properties

    /// <summary>Kamera ortografis yang dikendalikan script ini.</summary>
    public Camera TargetCamera => targetCamera;

    /// <summary>Ukuran ortografis bawaan kamera (tampilan normal sebelum zoom apa pun).</summary>
    public float DefaultOrthographicSize => defaultOrthographicSize;

    /// <summary>Posisi kamera untuk tampilan default, sudah dibatasi ke dalam area level.</summary>
    public Vector3 DefaultViewPosition => ClampToBounds(authoredPosition, defaultOrthographicSize);

    /// <summary>True saat kamera sedang dikendalikan script (misalnya intro kamera awal level).</summary>
    public bool IsScriptedControlActive => scriptedControl;

    #endregion

    #region Public Methods

    /// <summary>
    /// Mengambil alih kamera dari input pemain. Selama mode ini aktif, posisi dan zoom kamera
    /// diatur lewat <see cref="GetClampedViewPosition"/> dan tidak digeser LateUpdate.
    /// </summary>
    public void BeginScriptedControl()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        scriptedControl = true;
        isFocusing = false;
        wasAiming = false;
        smoothVelocity = Vector3.zero;
        zoomVelocity = 0f;
        targetPosition = transform.position;
    }

    /// <summary>
    /// Mengembalikan kamera ke kendali pemain dengan framing default: posisi dan ukuran
    /// ortografis kembali ke nilai desain awal, lalu geser kamera aktif lagi.
    /// </summary>
    public void EndScriptedControl()
    {
        scriptedControl = false;
        isFocusing = false;
        wasAiming = false;
        smoothVelocity = Vector3.zero;
        zoomVelocity = 0f;
        targetOrthographicSize = defaultOrthographicSize;

        Vector3 defaultView = DefaultViewPosition;
        transform.position = new Vector3(defaultView.x, defaultView.y, transform.position.z);
        targetPosition = defaultView;

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = defaultOrthographicSize;
        }
    }

    /// <summary>
    /// Fokus sebentar ke posisi Webi setelah teleport. Dipakai portal supaya Webi tidak berpindah
    /// jauh di luar layar. Kalau <paramref name="duration"/> 0, dipakai <see cref="teleportFocusDuration"/>.
    /// </summary>
    /// <param name="worldPosition">Posisi awal fokus (mis. posisi Rigidbody Webi saat mendarat).</param>
    /// <param name="duration">Lama gerak kamera; 0 = pakai Teleport Focus Duration di Inspector.</param>
    /// <param name="followTarget">Opsional. Kalau diisi, kamera terus mengikuti objek ini selama durasi.</param>
    public void PlayTeleportFocus(Vector3 worldPosition, float duration = 0f, Transform followTarget = null)
    {
        if (!focusCameraOnTeleport || scriptedControl || targetCamera == null)
        {
            return;
        }

        teleportFocusPosition = worldPosition;
        teleportFocusTarget = followTarget;
        teleportFocusStartPosition = transform.position;
        teleportFocusElapsed = 0f;
        teleportFocusDurationTotal = duration > 0f ? duration : Mathf.Max(0.05f, teleportFocusDuration);
        isFocusing = false;
        wasAiming = false;
        smoothVelocity = Vector3.zero;
    }

    /// <summary>
    /// Posisi kamera saat fokus teleport. Batas level sementara diperluas seperlunya supaya tujuan
    /// portal selalu bisa dicapai: batas isi level hanya dihitung dari Tilemap, sedangkan platform
    /// tujuan portal bisa berupa prefab Platform yang berada di luar area itu. Tanpa ini kamera
    /// berhenti di tepi batas dan terlihat tidak sampai ke Webi.
    /// </summary>
    private Vector3 GetTeleportFocusPosition(Vector2 worldPosition)
    {
        Vector3 position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        if (targetCamera == null || levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            return position;
        }

        float halfHeight = Mathf.Max(0.01f, targetCamera.orthographicSize);
        float halfWidth = halfHeight * targetCamera.aspect;

        float xMin = Mathf.Min(levelBounds.xMin, position.x - halfWidth);
        float xMax = Mathf.Max(levelBounds.xMax, position.x + halfWidth);
        float yMin = Mathf.Min(levelBounds.yMin, position.y - halfHeight);
        float yMax = Mathf.Max(levelBounds.yMax, position.y + halfHeight);

        return new Vector3(
            ClampAxis(position.x, xMin + halfWidth, xMax - halfWidth, position.x),
            ClampAxis(position.y, yMin + halfHeight, yMax - halfHeight, position.y),
            position.z
        );
    }

    /// <summary>
    /// Memperluas batas level sampai titik yang sudah dicapai kamera setelah teleport. Tanpa ini,
    /// kendali geser sesudah teleport langsung menarik kamera balik ke dalam batas lama.
    /// </summary>
    private void ExpandLevelBoundsTo(Vector3 position)
    {
        if (levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            return;
        }

        float halfHeight = targetCamera != null
            ? Mathf.Max(0.01f, targetCamera.orthographicSize)
            : Mathf.Max(0.01f, defaultOrthographicSize);
        float halfWidth = targetCamera != null ? halfHeight * targetCamera.aspect : halfHeight;

        levelBounds = Rect.MinMaxRect(
            Mathf.Min(levelBounds.xMin, position.x - halfWidth),
            Mathf.Min(levelBounds.yMin, position.y - halfHeight),
            Mathf.Max(levelBounds.xMax, position.x + halfWidth),
            Mathf.Max(levelBounds.yMax, position.y + halfHeight)
        );

        // Area latar ikut diperluas, kalau tidak bagian layar yang baru terbuka akan menampilkan
        // warna clear kamera (strip polos di bawah level).
        SetUpBackgroundExtension();
    }

    /// <summary>
    /// Mendeteksi perpindahan mendadak Webi dalam satu frame lalu memicu fokus kamera. Portal sudah
    /// memanggil <see cref="PlayTeleportFocus"/> sendiri; deteksi ini pengaman supaya kamera tetap
    /// menyusul walau teleport-nya datang dari script lain (respawn, pintu, dsb.).
    /// </summary>
    private void DetectPlayerTeleportJump()
    {
        if (teleportWatchPlayer == null)
        {
            teleportWatchPlayer = FindFirstObjectByType<PlayerController>();
            if (teleportWatchPlayer == null)
            {
                return;
            }
        }

        Vector3 playerPosition = teleportWatchPlayer.transform.position;
        if (!hasLastPlayerPosition)
        {
            lastPlayerPosition = playerPosition;
            hasLastPlayerPosition = true;
            return;
        }

        Vector2 jump = (Vector2)playerPosition - (Vector2)lastPlayerPosition;
        lastPlayerPosition = playerPosition;

        if (!followTeleportJumps || isFocusing || teleportFocusElapsed < teleportFocusDurationTotal)
        {
            return;
        }

        if (jump.magnitude < Mathf.Max(teleportJumpThreshold, 0.01f))
        {
            return;
        }

        if (!focusCameraOnTeleport)
        {
            Debug.LogWarning(
                $"{name}: Webi berpindah {jump.magnitude:F1} unit, tetapi Focus Camera On Teleport dimatikan. " +
                "Aktifkan di Inspector supaya kamera ikut menyusul.",
                this
            );
            return;
        }

        PlayTeleportFocus(playerPosition, 0f, teleportWatchPlayer.transform);
    }

    /// <summary>
    /// Langsung mengembalikan kamera ke framing default tanpa animasi. Dipakai tombol Retry supaya
    /// level langsung tampil normal, tanpa fase zoom ke tumpukan tanah dan Webi.
    /// </summary>
    public void SnapToDefaultView()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        isFocusing = false;
        wasAiming = false;
        teleportFocusTarget = null;
        teleportFocusElapsed = teleportFocusDurationTotal;
        smoothVelocity = Vector3.zero;
        zoomVelocity = 0f;
        targetOrthographicSize = defaultOrthographicSize;
        targetPosition = DefaultViewPosition;
        transform.position = targetPosition;

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = defaultOrthographicSize;
        }
    }

    /// <summary>
    /// Posisi kamera yang sudah dikurung di dalam area level untuk ukuran zoom tertentu.
    /// Dipakai mode script supaya zoom in di tepi level tidak memperlihatkan area kosong.
    /// </summary>
    public Vector3 GetClampedViewPosition(Vector3 worldPosition, float orthographicSize)
    {
        return ClampToBounds(
            new Vector3(worldPosition.x, worldPosition.y, transform.position.z),
            orthographicSize
        );
    }

    /// <summary>
    /// Isi area level dari ukuran gambar latar, supaya batas geser kamera sama persis dengan tepi
    /// background: kamera berhenti di sana dan tidak pernah menampilkan area di luar gambar.
    /// Dipanggil otomatis saat Awake, dan bisa dipanggil lagi kalau background berubah.
    /// </summary>
    public void ApplyBackgroundBounds()
    {
        if (!useBackgroundAsBounds || backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        Bounds background = backgroundRenderer.bounds;
        levelBounds = new Rect(background.min.x, background.min.y, background.size.x, background.size.y);
        targetPosition = ClampToBounds(targetPosition);
    }

    /// <summary>
    /// Mengisi area level dari seluruh isi level (Tilemap platform), bukan dari gambar background.
    /// Ini yang membuat platform di luar gambar tetap bisa dijangkau kamera. Kalau tidak ada
    /// Tilemap di scene, batas dari background tetap dipakai.
    /// </summary>
    public bool ApplyLevelContentBounds()
    {
        if (!TryGetLevelContentBounds(out Rect content))
        {
            return false;
        }

        levelBounds = content;
        targetPosition = ClampToBounds(targetPosition);

        if (backgroundExtension != null)
        {
            SetUpBackgroundExtension();
        }

        return true;
    }

    /// <summary>Mengunci kamera agar perlahan bergerak ke posisi dunia tertentu dan melakukan zoom in opsional.</summary>
    public void FocusOn(Vector3 worldPosition, float? customZoomSize = null)
    {
        focusPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        targetOrthographicSize = customZoomSize ?? focusOrthographicSize;

        // Tampilan yang sedang terlihat saat ini dicatat sebagai batas aman: kamera saat fokus
        // tidak boleh membuka area baru (misalnya area tanpa gambar latar).
        focusViewArea = GetViewRect();
        isFocusing = true;
    }

    /// <summary>Melepas mode fokus dan mengembalikan kendali geser ke pemain.</summary>
    public void ReleaseFocus()
    {
        if (!isFocusing)
        {
            return;
        }

        isFocusing = false;
        targetPosition = transform.position;
        smoothVelocity = Vector3.zero;
        wasAiming = false;
    }

    /// <summary>Mengganti area level saat runtime (misalnya setelah level dibuat dari data).</summary>
    public void SetLevelBounds(Rect bounds)
    {
        levelBounds = bounds;
        targetPosition = ClampToBounds(targetPosition);
        SetUpBackgroundExtension();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Menyiapkan perluasan gambar latar mengikuti area level. Komponen bantuannya dibuat otomatis
    /// saat runtime, jadi level baru tidak perlu diatur manual di scene.
    /// </summary>
    private void SetUpBackgroundExtension()
    {
        if (!extendBackground ||
            backgroundRenderer == null ||
            levelBounds.width <= 0f ||
            levelBounds.height <= 0f)
        {
            return;
        }

        if (backgroundExtension == null)
        {
            backgroundExtension = GetComponent<LevelBackgroundExtension>();
            if (backgroundExtension == null)
            {
                backgroundExtension = gameObject.AddComponent<LevelBackgroundExtension>();
            }
        }

        backgroundExtension.Configure(backgroundRenderer, mirrorBackgroundTiles);
        backgroundExtension.SetArea(levelBounds, targetCamera, authoredPosition);
    }

    /// <summary>
    /// Menghitung batas dunia dari seluruh isi level: semua Tilemap (tanah dan lapisan permukaan)
    /// di scene, atau hanya yang ada di bawah Level Content Root kalau diisi.
    /// </summary>
    private bool TryGetLevelContentBounds(out Rect result)
    {
        result = default;

        Bounds combined = default;
        bool hasContent = false;

        if (levelContentRoot != null)
        {
            Renderer[] renderers = levelContentRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                TryIncludeRenderer(renderer, ref combined, ref hasContent);
            }
        }
        else
        {
            TilemapRenderer[] tilemaps = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (TilemapRenderer tilemap in tilemaps)
            {
                TryIncludeRenderer(tilemap, ref combined, ref hasContent);
            }
        }

        if (!hasContent)
        {
            return false;
        }

        float padding = Mathf.Max(0f, contentPadding);
        result = new Rect(
            combined.min.x - padding,
            combined.min.y - padding,
            combined.size.x + padding * 2f,
            combined.size.y + padding * 2f
        );
        return result.width > 0f && result.height > 0f;
    }

    private void TryIncludeRenderer(Renderer renderer, ref Bounds combined, ref bool hasContent)
    {
        // Background punya batasnya sendiri; jangan ikut dihitung sebagai isi level.
        if (renderer == null || renderer == backgroundRenderer)
        {
            return;
        }

        // Gambar bantuan hasil perluasan background juga bukan isi level.
        if (backgroundExtension != null && backgroundExtension.ContainsRenderer(renderer))
        {
            return;
        }

        Bounds bounds = renderer.bounds;
        if (bounds.size.x <= 0.001f || bounds.size.y <= 0.001f)
        {
            return;
        }

        if (!hasContent)
        {
            combined = bounds;
            hasContent = true;
            return;
        }

        combined.Encapsulate(bounds);
    }

    /// <summary>Tombol mouse yang sedang dipakai untuk menggeser kamera.</summary>
    private ButtonControl GetDragControl()
    {
        if (Mouse.current == null)
        {
            return null;
        }

        switch (dragButton)
        {
            case DragButton.Left:
                return Mouse.current.leftButton;
            case DragButton.Middle:
                return Mouse.current.middleButton;
            default:
                return Mouse.current.rightButton;
        }
    }

    /// <summary>Area dunia yang sedang terlihat oleh kamera sekarang.</summary>
    private Rect GetViewRect()
    {
        if (targetCamera == null)
        {
            return new Rect();
        }

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;
        Vector3 cameraPosition = transform.position;
        return new Rect(
            cameraPosition.x - halfWidth,
            cameraPosition.y - halfHeight,
            halfWidth * 2f,
            halfHeight * 2f
        );
    }

    /// <summary>
    /// Menahan posisi kamera saat fokus supaya seluruh tampilan kamera tetap berada di dalam
    /// area yang sudah terlihat sebelum fokus. Kalau area lebih kecil dari tampilan kamera,
    /// kamera dipusatkan di area itu. Kalau pengaman ini dimatikan, dipakai pembatas area level.
    /// </summary>
    private Vector3 ClampFocusPosition(Vector3 position)
    {
        if (targetCamera == null)
        {
            return position;
        }

        if (!keepFocusInsideCurrentView)
        {
            return clampFocusToLevelBounds ? ClampToBounds(position) : position;
        }

        if (focusViewArea.width <= 0f || focusViewArea.height <= 0f)
        {
            return position;
        }

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 clamped = position;
        clamped.x = ClampAxis(
            position.x,
            focusViewArea.xMin + halfWidth,
            focusViewArea.xMax - halfWidth,
            focusViewArea.center.x
        );
        clamped.y = ClampAxis(
            position.y,
            focusViewArea.yMin + halfHeight,
            focusViewArea.yMax - halfHeight,
            focusViewArea.center.y
        );
        return clamped;
    }

    /// <summary>
    /// Memastikan seluruh tampilan kamera (bukan cuma titik tengahnya) tetap di dalam area level.
    /// Kalau isi level lebih kecil dari layar pada satu sumbu, kamera tidak digeser pada sumbu itu
    /// dan posisi desain awal dipertahankan supaya framing tidak berubah.
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        return ClampToBounds(
            position,
            targetCamera != null ? targetCamera.orthographicSize : defaultOrthographicSize
        );
    }

    private Vector3 ClampToBounds(Vector3 position, float orthographicSize)
    {
        if (targetCamera == null || levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            return position;
        }

        float halfHeight = Mathf.Max(0.01f, orthographicSize);
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 clamped = position;
        clamped.x = ClampAxis(
            position.x,
            levelBounds.xMin + halfWidth,
            levelBounds.xMax - halfWidth,
            authoredPosition.x
        );
        clamped.y = ClampAxis(
            position.y,
            levelBounds.yMin + halfHeight,
            levelBounds.yMax - halfHeight,
            authoredPosition.y
        );
        return clamped;
    }

    private static float ClampAxis(float value, float min, float max, float fallbackValue)
    {
        return min > max ? fallbackValue : Mathf.Clamp(value, min, max);
    }

    #endregion
}
