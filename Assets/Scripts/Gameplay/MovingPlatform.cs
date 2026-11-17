using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ground/objek yang bergerak bolak-balik di sepanjang lintasan.
/// Lintasan dapat memakai waypoint dunia atau offset lokal untuk satu garis lurus.
/// Cocok ditempel di root chunk grid tilemap (mis. duplikat "Grid Miring").
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class MovingPlatform : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]

    [Tooltip("Daftar waypoint dunia. Jika diisi, platform bergerak melewati waypoint sesuai urutan; " +
             "jika kosong, platform memakai Local Travel Offset. Posisi waypoint disimpan saat scene mulai.")]
    public Transform[] waypoints = new Transform[0];

    public enum WaypointPatrolMode
    {
        PingPong,
        Loop,
        Once,
        Random
    }

    [Tooltip("Pola perjalanan antar waypoint.")]
    public WaypointPatrolMode waypointPatrolMode = WaypointPatrolMode.PingPong;

    [Tooltip("Jika aktif, platform langsung mulai di waypoint pertama saat scene mulai.")]
    public bool startAtFirstWaypoint;

    [Tooltip("Jarak ke waypoint sampai dianggap tercapai.")]
    public float waypointReachDistance = 0.05f;

    [Tooltip("Jika aktif, waypoint dibaca sebagai posisi tengah isi platform yang terlukis (bukan pivot root). " +
             "Jadi platform berhenti tepat di titik waypoint seperti yang terlihat di Scene view.")]
    public bool waypointTargetsContentCenter = true;

    [Tooltip("Jarak dan arah gerak dalam ruang lokal platform. Contoh (0, -6) = turun 6 unit. " +
             "Dipakai hanya jika daftar waypoint kosong. Rotasi platform memutar lintasan ini, " +
             "skala platform ikut menskalakannya.")]
    public Vector2 localTravelOffset = new Vector2(0f, -6f);

    [Tooltip("Kecepatan gerak dalam unit per detik.")]
    public float moveSpeed = 2f;

    [Tooltip("Jeda diam di setiap titik tujuan (detik).")]
    public float dwellTime = 0.5f;

    [Tooltip("Posisi awal lintasan garis lurus: 0 = titik awal, 1 = titik akhir.")]
    [Range(0f, 1f)]
    public float startProgress = 0f;

    [Tooltip("Pola bolak-balik lintasan garis lurus (hanya dipakai jika Waypoints kosong).")]
    public bool pingPong = true;

    [Header("Rider")]

    [Tooltip("Jika aktif, Webi ikut terbawa saat berdiri di atas platform.")]
    public bool carryRiders = true;

    [Tooltip("Jarak kontak (unit) yang dianggap Webi sedang menumpang.")]
    public float carryContactDistance = 0.12f;

    [Header("Audio")]

    [Tooltip("SFX saat platform bergerak. Diputar berulang (loop) selama platform bergerak, " +
             "lalu berhenti saat platform berhenti di ujung lintasan atau saat jeda diam.")]
    public AudioClip moveLoopSfx;

    [Tooltip("Volume SFX gerak saat Webi berada dekat (0 = senyap, 1 = penuh). Sengaja kecil " +
             "supaya suara gerak tidak mengganggu.")]
    [Range(0f, 1f)]
    public float moveLoopSfxVolume = 0.33f;

    [Tooltip("Jika aktif, SFX gerak mengecil saat Webi menjauh dan senyap di luar jarak terjauh. " +
             "Polanya sama dengan proximity SFX musuh (Bug/Fly).")]
    public bool moveSfxFollowsDistance = true;

    [Tooltip("Jarak (unit) saat SFX gerak terdengar paling keras.")]
    public float moveSfxNearDistance = 4f;

    [Tooltip("Jarak (unit) saat SFX gerak tidak terdengar lagi.")]
    public float moveSfxFarDistance = 16f;

    [Tooltip("Kecepatan volume menyusul jarak (per detik). 0 = volume langsung mengikuti jarak.")]
    public float moveSfxSmoothSpeed = 6f;

    [Tooltip("SFX saat platform berhenti, mis. sampai di ujung lintasan atau saat jeda diam. " +
             "Diputar sekali dan volumenya tidak ikut dikecilkan seperti SFX gerak.")]
    public AudioClip moveStopSfx;

    [Tooltip("Volume SFX saat berhenti (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float moveStopSfxVolume = 0.7f;

    [Header("Gizmo (editor)")]

    [Tooltip("Tampilkan garis lintasan, panah arah, dan kotak area lukisan di Scene view.")]
    public bool showPathGizmo = true;

    [Tooltip("Tampilkan label teks waypoint dan pola di Scene view.")]
    public bool showGizmoLabel = true;

    [Header("Path Line (in game)")]

    [Tooltip("Tampilkan garis lintasan saat game berjalan supaya pemain tahu platform bergerak sampai mana. " +
             "Berlaku untuk mode waypoint maupun Local Travel Offset.")]
    public bool showPathLine = true;

    [Tooltip("Warna garis lintasan dan bulatan di ujungnya.")]
    public Color pathLineColor = new Color(1f, 1f, 1f, 0.55f);

    [Tooltip("Tebal garis lintasan (unit dunia).")]
    public float pathLineWidth = 0.08f;

    [Tooltip("Tampilkan bulatan di kedua ujung lintasan sebagai penanda batas gerak platform.")]
    public bool showPathEndDots = true;

    [Tooltip("Jari-jari bulatan ujung (unit dunia).")]
    public float pathDotRadius = 0.18f;

    [Tooltip("Batas atas urutan gambar garis. Angka lebih kecil = di belakang. Garis selalu ditaruh " +
             "satu tingkat di belakang gambar paling belakang milik platform ini (ground/rumput), " +
             "jadi nilai ini hanya dipakai kalau platform belum punya renderer sama sekali.")]
    public int pathSortingOrder = -6;

    [Tooltip("Material khusus untuk garis. Jika kosong, dipakai shader dari tilemap platform (Sprites-Default).")]
    public Material pathLineMaterial;

    #endregion

    #region Public Properties

    /// <summary>Perpindahan platform pada physics step terakhir (ruang dunia).</summary>
    public Vector2 DeltaThisFrame => delta;

    /// <summary>Perpindahan lintasan garis lurus dalam ruang dunia, dari rotasi dan skala saat Awake.</summary>
    public Vector2 WorldTravelOffset => worldTravelOffset;

    #endregion

    private Vector2 worldTravelOffset;
    private Vector2 startPosition;
    private Vector2 contentCenterOffset;
    private Vector2[] waypointPositions;
    private bool[] validWaypoints;
    private Vector2 delta;
    private float progress;
    private float dwellRemaining;
    private int direction = 1;
    private int waypointIndex;
    private bool waypointMovementStopped;
    private bool wasMoving;
    private Collider2D[] platformColliders;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private AudioSource moveLoopSource;
    private Transform proximityTarget;
    private float proximitySearchTimer;

    void Awake()
    {
        startPosition = transform.position;
        platformColliders = GetComponentsInChildren<Collider2D>(true);
        contentCenterOffset = ComputeContentCenterOffset();
        SetupMoveLoopSource();

        CacheWaypoints();
        if (HasWaypoints)
        {
            waypointIndex = FindFirstWaypoint();
            if (waypointIndex < 0)
            {
                waypointMovementStopped = true;
                Debug.LogWarning(
                    $"{name} memiliki daftar waypoint tetapi tidak ada referensi yang valid; platform tidak bergerak.",
                    this
                );
                return;
            }

            if (startAtFirstWaypoint)
            {
                transform.position = waypointPositions[waypointIndex];
            }

            return;
        }

        worldTravelOffset = transform.TransformVector(localTravelOffset);
        progress = Mathf.Clamp01(startProgress);
        transform.position = startPosition + worldTravelOffset * progress;
    }

    void Start()
    {
        BuildPathGuide();
    }

    void Update()
    {
        UpdateMoveLoopAudio();
    }

    void OnDisable()
    {
        StopMoveLoopAudio();
    }

    void FixedUpdate()
    {
        MoveAlongPath();
        PlayMovementSfx();
        CarryRider();
    }

    /// <summary>
    /// SFX berhenti diputar sekali setiap kali platform berhenti. Suara gerak tidak lagi diputar
    /// sebagai one-shot: loop AudioSource di <see cref="UpdateMoveLoopAudio"/> yang mengurus durasi
    /// seluruh perjalanan. <see cref="delta"/> nol menandai platform diam (jeda di ujung lintasan
    /// atau setelah selesai).
    /// </summary>
    void PlayMovementSfx()
    {
        bool isMoving = delta.sqrMagnitude > 0.0000001f;
        if (isMoving == wasMoving)
        {
            return;
        }

        wasMoving = isMoving;
        if (isMoving)
        {
            return;
        }

        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.PlaySfx(moveStopSfx, 1f, 1f, moveStopSfxVolume);
    }

    /// <summary>
    /// Menyiapkan AudioSource khusus untuk loop suara gerak. Sumbernya terpisah dari AudioSource
    /// lain di objek yang sama supaya clip dan volumenya tidak saling menimpa.
    /// </summary>
    void SetupMoveLoopSource()
    {
        moveLoopSource = gameObject.AddComponent<AudioSource>();

        // Diputar 2D dan volumenya dihitung dari jarak ke Webi, sama seperti ProximitySfx musuh.
        moveLoopSource.playOnAwake = false;
        moveLoopSource.loop = true;
        moveLoopSource.spatialBlend = 0f;
        moveLoopSource.dopplerLevel = 0f;
        moveLoopSource.volume = 0f;
    }

    /// <summary>
    /// Suara gerak diputar berulang selama platform bergerak, dengan volume kecil dan mengecil saat
    /// Webi menjauh (proximity), lalu berhenti begitu platform berhenti. Memakai waktu nyata supaya
    /// suara ikut berhenti saat game dibekukan (pause/game over).
    /// </summary>
    void UpdateMoveLoopAudio()
    {
        if (moveLoopSource == null)
        {
            return;
        }

        if (moveLoopSource.clip != moveLoopSfx)
        {
            moveLoopSource.Stop();
            moveLoopSource.clip = moveLoopSfx;
            moveLoopSource.volume = 0f;
        }

        bool isMoving = wasMoving && Time.timeScale > 0f && moveLoopSfx != null;
        float targetVolume = isMoving
            ? moveLoopSfxVolume * GetProximityFactor() * GetGlobalSfxVolume()
            : 0f;

        moveLoopSource.volume = moveSfxSmoothSpeed > 0f
            ? Mathf.MoveTowards(moveLoopSource.volume, targetVolume, moveSfxSmoothSpeed * Time.unscaledDeltaTime)
            : targetVolume;

        if (moveLoopSource.volume > 0.0001f)
        {
            if (!moveLoopSource.isPlaying)
            {
                moveLoopSource.Play();
            }
        }
        else if (moveLoopSource.isPlaying)
        {
            moveLoopSource.Stop();
        }
    }

    void StopMoveLoopAudio()
    {
        if (moveLoopSource == null)
        {
            return;
        }

        moveLoopSource.volume = 0f;
        if (moveLoopSource.isPlaying)
        {
            moveLoopSource.Stop();
        }
    }

    /// <summary>Pengali volume berdasarkan jarak platform ke Webi; 1 kalau fitur proximity dimatikan.</summary>
    float GetProximityFactor()
    {
        if (!moveSfxFollowsDistance)
        {
            return 1f;
        }

        Transform player = ResolveProximityTarget();
        if (player == null)
        {
            return 1f;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        return ProximitySfx.VolumeFactor(distance, moveSfxNearDistance, moveSfxFarDistance);
    }

    /// <summary>Transform Webi; dicari lewat tag Player sekali per setengah detik lalu disimpan.</summary>
    Transform ResolveProximityTarget()
    {
        if (proximityTarget != null)
        {
            return proximityTarget;
        }

        proximitySearchTimer -= Time.unscaledDeltaTime;
        if (proximitySearchTimer > 0f)
        {
            return null;
        }

        proximitySearchTimer = 0.5f;
        GameObject player = GameObject.FindWithTag("Player");
        proximityTarget = player != null ? player.transform : null;
        return proximityTarget;
    }

    /// <summary>Volume SFX global dari Settings; 0 kalau SFX sedang dimatikan atau dibungkam intro.</summary>
    static float GetGlobalSfxVolume()
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            return 1f;
        }

        if (manager.IsSfxMuted || manager.IsSfxSuppressed)
        {
            return 0f;
        }

        return Mathf.Clamp01(manager.SfxVolume);
    }

    void MoveAlongPath()
    {
        delta = Vector2.zero;

        if (HasWaypoints)
        {
            MoveBetweenWaypoints();
            return;
        }

        MoveAlongStraightPath();
    }

    void MoveBetweenWaypoints()
    {
        if (waypointMovementStopped || moveSpeed <= 0f || dwellRemaining > 0f)
        {
            if (dwellRemaining > 0f)
            {
                dwellRemaining -= Time.fixedDeltaTime;
            }

            return;
        }

        Vector2 target = waypointPositions[waypointIndex];
        Vector2 currentPosition = transform.position;
        float distance = Vector2.Distance(currentPosition, target);
        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 newPosition = distance <= step || distance <= waypointReachDistance
            ? target
            : Vector2.MoveTowards(currentPosition, target, step);

        ApplyPosition(newPosition);

        if (Vector2.Distance(newPosition, target) <= waypointReachDistance)
        {
            if (AdvanceWaypoint())
            {
                dwellRemaining = Mathf.Max(0f, dwellTime);
            }
        }
    }

    bool AdvanceWaypoint()
    {
        int nextIndex = -1;

        switch (waypointPatrolMode)
        {
            case WaypointPatrolMode.PingPong:
                nextIndex = FindWaypointInDirection(waypointIndex + direction, direction, false);
                if (nextIndex < 0)
                {
                    direction *= -1;
                    nextIndex = FindWaypointInDirection(waypointIndex + direction, direction, false);
                }
                break;

            case WaypointPatrolMode.Loop:
                nextIndex = FindWaypointInDirection((waypointIndex + 1) % waypoints.Length, 1, true);
                break;

            case WaypointPatrolMode.Once:
                nextIndex = FindWaypointInDirection(waypointIndex + 1, 1, false);
                break;

            case WaypointPatrolMode.Random:
                int[] candidates = GetOtherValidWaypoints();
                if (candidates.Length > 0)
                {
                    nextIndex = candidates[Random.Range(0, candidates.Length)];
                }
                break;
        }

        if (nextIndex < 0)
        {
            waypointMovementStopped = true;
            return false;
        }

        waypointIndex = nextIndex;
        return true;
    }

    int FindWaypointInDirection(int index, int step, bool wrap)
    {
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (wrap)
            {
                index = (index + waypoints.Length) % waypoints.Length;
            }
            else if (index < 0 || index >= waypoints.Length)
            {
                return -1;
            }

            if (validWaypoints[index])
            {
                return index;
            }

            index += step;
        }

        return -1;
    }

    int[] GetOtherValidWaypoints()
    {
        int validCount = 0;
        for (int i = 0; i < validWaypoints.Length; i++)
        {
            if (validWaypoints[i] && i != waypointIndex)
            {
                validCount++;
            }
        }

        int[] candidates = new int[validCount];
        int candidateIndex = 0;
        for (int i = 0; i < validWaypoints.Length; i++)
        {
            if (validWaypoints[i] && i != waypointIndex)
            {
                candidates[candidateIndex++] = i;
            }
        }

        return candidates;
    }

    void MoveAlongStraightPath()
    {
        float length = worldTravelOffset.magnitude;
        if (length < 0.0001f || moveSpeed <= 0f || direction == 0)
        {
            return;
        }

        if (dwellRemaining > 0f)
        {
            dwellRemaining -= Time.fixedDeltaTime;
            return;
        }

        progress += direction * (moveSpeed / length) * Time.fixedDeltaTime;

        if (progress >= 1f)
        {
            progress = 1f;
            if (pingPong)
            {
                direction = -1;
                dwellRemaining = dwellTime;
            }
            else
            {
                direction = 0;
            }
        }
        else if (progress <= 0f)
        {
            progress = 0f;
            if (pingPong)
            {
                direction = 1;
                dwellRemaining = dwellTime;
            }
            else
            {
                direction = 0;
            }
        }

        ApplyPosition(startPosition + worldTravelOffset * progress);
    }

    void ApplyPosition(Vector2 newPosition)
    {
        delta = newPosition - (Vector2)transform.position;
        if (delta.sqrMagnitude <= 0f)
        {
            return;
        }

        // Collider tilemap adalah body statis; sinkronkan setelah memindahkan transform agar raycast
        // Webi pada physics step yang sama sudah melihat posisi baru.
        transform.position = newPosition;
        Physics2D.SyncTransforms();
    }

    void CacheWaypoints()
    {
        int count = waypoints != null ? waypoints.Length : 0;
        waypointPositions = new Vector2[count];
        validWaypoints = new bool[count];
        Vector2 contentOffset = GetContentOffset();

        for (int i = 0; i < count; i++)
        {
            validWaypoints[i] = waypoints[i] != null;
            if (validWaypoints[i])
            {
                // Waypoint menandai letak ground yang terlukis, sedangkan yang digeser adalah root.
                // Karena itu posisi tujuan root = posisi waypoint - selisih root ke tengah isi.
                waypointPositions[i] = (Vector2)waypoints[i].position - contentOffset;
            }
        }
    }

    /// <summary>Selisih dari pivot root ke tengah isi yang terlukis (nol jika isi tidak terbaca).</summary>
    Vector2 GetContentOffset()
    {
        return waypointTargetsContentCenter ? contentCenterOffset : Vector2.zero;
    }

    Vector2 ComputeContentCenterOffset()
    {
        if (!TryGetContentBounds(out Bounds contentBounds))
        {
            return Vector2.zero;
        }

        return (Vector2)contentBounds.center - (Vector2)transform.position;
    }

    int FindFirstWaypoint()
    {
        for (int i = 0; i < validWaypoints.Length; i++)
        {
            if (validWaypoints[i])
            {
                return i;
            }
        }

        return -1;
    }

    bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    void CarryRider()
    {
        if (!carryRiders || delta.sqrMagnitude <= 0f)
        {
            return;
        }

        CachePlayer();
        if (playerBody == null || playerCollider == null)
        {
            return;
        }

        // Webi sudah mengikuti platform lewat PlayerController-nya sendiri (pergeseran posisi tiap
        // physics step), jadi jangan digeser lagi di sini supaya geraknya tidak dobel.
        if (playerBody.GetComponent<PlayerController>() != null)
        {
            return;
        }

        if (!IsTouchingPlatform(playerCollider))
        {
            return;
        }

        // Webi digeser sebelum script-nya sendiri berjalan (execution order -100), jadi raycast dan
        // perhitungan langkahnya pada physics step ini sudah memakai posisi baru.
        playerBody.position += delta;
    }

    bool IsTouchingPlatform(Collider2D rider)
    {
        if (platformColliders == null)
        {
            return false;
        }

        for (int i = 0; i < platformColliders.Length; i++)
        {
            Collider2D platformCollider = platformColliders[i];
            if (platformCollider == null || platformCollider == rider)
            {
                continue;
            }

            ColliderDistance2D distance = rider.Distance(platformCollider);
            if (distance.isValid && distance.distance <= carryContactDistance)
            {
                return true;
            }
        }

        return false;
    }

    void CachePlayer()
    {
        if (playerBody != null && playerCollider != null)
        {
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        playerBody = player.GetComponent<Rigidbody2D>();
        playerCollider = player.GetComponent<Collider2D>();
    }

    // OnDrawGizmos (bukan OnDrawGizmosSelected) supaya lintasan dan label tetap terlihat di
    // Scene view tanpa harus memilih objeknya, sama seperti gizmo musuh.
    void OnDrawGizmos()
    {
        if (!showPathGizmo && !showGizmoLabel)
        {
            return;
        }

        if (HasWaypoints)
        {
            DrawWaypointGizmo();
            return;
        }

        DrawStraightPathGizmo();
    }

    void DrawStraightPathGizmo()
    {
        Vector2 offset = Application.isPlaying
            ? worldTravelOffset
            : (Vector2)transform.TransformVector(localTravelOffset);

        // Gizmo digambar dari tengah isi yang terlukis (bukan pivot root), supaya garis lintasan
        // selalu berada di tengah ground berapa pun sel yang dilukis.
        bool hasContent = TryGetContentBounds(out Bounds contentBounds);
        Vector2 from = hasContent ? (Vector2)contentBounds.center : (Vector2)transform.position;
        Vector2 to = from + offset;

        if (showPathGizmo)
        {
            if (hasContent)
            {
                Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.35f);
                Gizmos.DrawWireCube(contentBounds.center, contentBounds.size);
            }

            Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.9f);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.25f);
            Gizmos.DrawWireSphere(to, 0.25f);

            Vector2 directionToEnd = (to - from).normalized;
            if (directionToEnd.sqrMagnitude > 0.0001f)
            {
                Gizmos.DrawLine(to, to + Rotate(directionToEnd, 150f) * 0.35f);
                Gizmos.DrawLine(to, to + Rotate(directionToEnd, -150f) * 0.35f);
            }

            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0.35f, 1f, 0.5f, 0.9f);
                Gizmos.DrawWireCube(from, Vector3.one * 0.3f);
            }
        }

#if UNITY_EDITOR
        if (showGizmoLabel)
        {
            UnityEditor.Handles.color = new Color(1f, 0.75f, 0.3f, 1f);
            UnityEditor.Handles.Label(
                from + Vector2.up * 0.6f,
                $"Moving Platform\n{Mathf.Abs(offset.magnitude):0.#} unit"
            );
        }
#endif
    }

    void DrawWaypointGizmo()
    {
        int count = waypoints.Length;
        Vector3[] points = new Vector3[count];
        bool[] valid = new bool[count];
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            valid[i] = Application.isPlaying
                ? validWaypoints != null && i < validWaypoints.Length && validWaypoints[i]
                : waypoints[i] != null;
            if (!valid[i])
            {
                continue;
            }

            points[i] = Application.isPlaying
                ? waypointPositions[i]
                : waypoints[i].position;
            validCount++;
        }

        if (validCount == 0)
        {
#if UNITY_EDITOR
            if (showPathGizmo)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Gizmos.DrawWireSphere(transform.position, 0.4f);
                Gizmos.DrawLine(transform.position + Vector3.left * 0.3f, transform.position + Vector3.right * 0.3f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, transform.position + Vector3.down * 0.3f);
            }

            if (showGizmoLabel)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.6f,
                    $"{name}: belum ada waypoint yang valid"
                );
            }
#endif
            return;
        }

        Color routeColor = new Color(0.35f, 0.9f, 1f, 0.9f);
        if (showPathGizmo)
        {
            if (TryGetContentBounds(out Bounds contentBounds))
            {
                Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.35f);
                Gizmos.DrawWireCube(contentBounds.center, contentBounds.size);
            }

            Gizmos.color = routeColor;
            if (waypointPatrolMode == WaypointPatrolMode.Random)
            {
                Color faint = routeColor;
                faint.a = 0.4f;
                Gizmos.color = faint;
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        if (valid[i] && valid[j])
                        {
                            DrawGizmoArrow(points[i], points[j], true);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < count - 1; i++)
                {
                    if (valid[i] && valid[i + 1])
                    {
                        DrawGizmoArrow(
                            points[i],
                            points[i + 1],
                            waypointPatrolMode == WaypointPatrolMode.PingPong
                        );
                    }
                }

                if (waypointPatrolMode == WaypointPatrolMode.Loop &&
                    count > 2 && valid[count - 1] && valid[0])
                {
                    DrawGizmoArrow(points[count - 1], points[0], false);
                }
            }
        }

        int activeIndex = Application.isPlaying && !waypointMovementStopped ? waypointIndex : -1;
        for (int i = 0; i < count; i++)
        {
            if (!valid[i])
            {
                continue;
            }

            bool isActive = i == activeIndex;
            Gizmos.color = isActive ? Color.green : routeColor;
            Gizmos.DrawWireSphere(points[i], isActive ? 0.3f : 0.2f);
            if (i == 0)
            {
                Gizmos.DrawSphere(points[i], 0.08f);
            }
#if UNITY_EDITOR
            if (showGizmoLabel)
            {
                string label = $"W{i + 1}";
                string waypointName = waypoints[i] != null ? waypoints[i].name : "kosong";
                if (waypointName != label)
                {
                    label += $" / {waypointName}";
                }

                UnityEditor.Handles.Label(points[i] + Vector3.up * 0.32f, label);
            }
#endif
        }

#if UNITY_EDITOR
        if (showGizmoLabel)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.9f,
                $"{name} / {waypointPatrolMode} / {validCount} dari {count} waypoint"
            );
        }
#endif
    }

    static void DrawGizmoArrow(Vector3 from, Vector3 to, bool bothDirections)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.001f)
        {
            return;
        }

        Vector3 directionToEnd = delta / length;
        float headSize = Mathf.Clamp(length * 0.18f, 0.12f, 0.6f);
        Gizmos.DrawLine(from, to);
        DrawGizmoArrowHead(to, directionToEnd, headSize);
        if (bothDirections)
        {
            DrawGizmoArrowHead(from, -directionToEnd, headSize);
        }
    }

    static void DrawGizmoArrowHead(Vector3 tip, Vector3 directionToEnd, float headSize)
    {
        Vector3 side = Vector3.Cross(directionToEnd, Vector3.forward) * headSize * 0.5f;
        Vector3 back = tip - directionToEnd * headSize;
        Gizmos.DrawLine(tip, back + side);
        Gizmos.DrawLine(tip, back - side);
    }

    /// <summary>Kotak gabungan isi yang terlukis, dihitung dari sel yang benar-benar berisi tile.</summary>
    bool TryGetContentBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        // Tilemap: pakai sel yang berisi tile. Renderer tilemap bisa melaporkan area kosong
        // (mis. tilemap yang sudah dihapus isinya), jadi jangan dipakai sebagai acuan.
        foreach (Tilemap tilemap in GetComponentsInChildren<Tilemap>(true))
        {
            if (tilemap == null)
            {
                continue;
            }

            BoundsInt cellBounds = tilemap.cellBounds;
            Vector3 halfCell = (Vector3)tilemap.cellSize * 0.5f;

            foreach (Vector3Int cell in cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                Vector3 center = tilemap.GetCellCenterWorld(cell);
                Vector3 cellMin = center - halfCell;
                Vector3 cellMax = center + halfCell;

                if (!hasBounds)
                {
                    min = cellMin;
                    max = cellMax;
                    hasBounds = true;
                }
                else
                {
                    min = Vector3.Min(min, cellMin);
                    max = Vector3.Max(max, cellMax);
                }
            }
        }

        // Chunk tanpa tilemap (mis. deretan sprite): pakai bounds renderer seperti biasa.
        if (!hasBounds)
        {
            foreach (Renderer contentRenderer in GetComponentsInChildren<Renderer>(true))
            {
                if (contentRenderer == null || !contentRenderer.enabled || contentRenderer is TilemapRenderer)
                {
                    continue;
                }

                if (hasBounds)
                {
                    min = Vector3.Min(min, contentRenderer.bounds.min);
                    max = Vector3.Max(max, contentRenderer.bounds.max);
                }
                else
                {
                    min = contentRenderer.bounds.min;
                    max = contentRenderer.bounds.max;
                    hasBounds = true;
                }
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        bounds.SetMinMax(min, max);
        return true;
    }

    /// <summary>
    /// Titik lintasan dalam ruang dunia yang diukur dari <b>tengah isi platform</b>, bukan pivot root.
    /// Dipakai garis lintasan di dalam game; hasilnya dua titik untuk Local Travel Offset dan
    /// beberapa titik untuk mode waypoint.
    /// </summary>
    public Vector2[] GetTravelPathPoints()
    {
        if (HasWaypoints)
        {
            // Saat runtime CacheWaypoints sudah berisi posisi root tujuan (sudah dikurangi selisih
            // isi), jadi ditambah lagi supaya garisnya jatuh di tengah ground. Di editor cache belum
            // ada dan posisi waypoint itu sendiri memang sudah tengah ground.
            bool cached = waypointPositions != null && waypointPositions.Length == waypoints.Length;
            int count = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    count++;
                }
            }

            Vector2[] waypointPath = new Vector2[count];
            int index = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                waypointPath[index++] = cached
                    ? waypointPositions[i] + contentCenterOffset
                    : (Vector2)waypoints[i].position;
            }

            return waypointPath;
        }

        Vector2 start = Application.isPlaying
            ? startPosition + contentCenterOffset
            : (Vector2)transform.position + contentCenterOffset;
        Vector2 offset = Application.isPlaying
            ? worldTravelOffset
            : (Vector2)transform.TransformVector(localTravelOffset);

        return new[] { start, start + offset };
    }

    void BuildPathGuide()
    {
        if (!showPathLine)
        {
            return;
        }

        Vector2[] path = GetTravelPathPoints();
        if (path == null || path.Length < 2)
        {
            return;
        }

        Material pathMaterial = ResolvePathMaterial();
        if (pathMaterial == null)
        {
            return;
        }

        LineRenderer line = CreatePathLine("Moving Ground Path", pathMaterial);
        line.positionCount = path.Length;
        for (int i = 0; i < path.Length; i++)
        {
            line.SetPosition(i, path[i]);
        }

        if (!showPathEndDots)
        {
            return;
        }

        CreatePathEndDot("Moving Ground Path Start", path[0], pathMaterial);
        CreatePathEndDot("Moving Ground Path End", path[path.Length - 1], pathMaterial);
    }

    LineRenderer CreatePathLine(string objectName, Material pathMaterial)
    {
        GameObject guide = new GameObject(objectName);
        LineRenderer line = guide.AddComponent<LineRenderer>();
        line.sharedMaterial = pathMaterial;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.startWidth = pathLineWidth;
        line.endWidth = pathLineWidth;
        line.startColor = pathLineColor;
        line.endColor = pathLineColor;

        // Urutan gambar dihitung dari renderer platform ini sendiri, supaya garis selalu berada di
        // belakang ground dan rumputnya walau tiap instance punya urutan yang berbeda di scene.
        line.sortingLayerID = ResolvePathSortingLayer();
        line.sortingOrder = ResolvePathSortingOrder();
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // Bulatan dibuat dari LineRenderer yang di-loop. Lebar garis = diameter, jari-jari loop = setengah
    // jari-jari tampil, sehingga lingkarannya terisi penuh (bukan cincin).
    void CreatePathEndDot(string objectName, Vector2 center, Material pathMaterial)
    {
        const int segments = 20;

        LineRenderer dot = CreatePathLine(objectName, pathMaterial);
        dot.loop = true;
        dot.numCapVertices = 0;
        dot.startWidth = pathDotRadius;
        dot.endWidth = pathDotRadius;
        dot.positionCount = segments;

        float ringRadius = pathDotRadius * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            dot.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius);
        }
    }

    /// <summary>
    /// Urutan gambar untuk garis lintasan: satu tingkat di belakang renderer paling belakang milik
    /// platform ini (tilemap ground/rumput). Jadi garis tidak pernah menimpa gambar platform sendiri.
    /// </summary>
    int ResolvePathSortingOrder()
    {
        int minOrder = int.MaxValue;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is SpriteRenderer) && !(renderer is TilemapRenderer))
            {
                continue;
            }

            if (renderer.sortingOrder < minOrder)
            {
                minOrder = renderer.sortingOrder;
            }
        }

        if (minOrder == int.MaxValue)
        {
            return pathSortingOrder;
        }

        return Mathf.Min(pathSortingOrder, minOrder - 1);
    }

    /// <summary>Sorting layer garis disamakan dengan renderer paling belakang milik platform ini.</summary>
    int ResolvePathSortingLayer()
    {
        int layerId = 0;
        int minOrder = int.MaxValue;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is SpriteRenderer) && !(renderer is TilemapRenderer))
            {
                continue;
            }

            if (renderer.sortingOrder < minOrder)
            {
                minOrder = renderer.sortingOrder;
                layerId = renderer.sortingLayerID;
            }
        }

        return layerId;
    }

    Material ResolvePathMaterial()
    {
        if (pathLineMaterial != null)
        {
            return pathLineMaterial;
        }

        // Tilemap memakai material sprite bawaan (Sprites-Default, unlit). Shader yang sama dipakai
        // supaya garis ikut pipeline render yang sedang aktif tanpa perlu setel material manual.
        TilemapRenderer tilemapRenderer = GetComponentInChildren<TilemapRenderer>(true);
        if (tilemapRenderer != null && tilemapRenderer.sharedMaterial != null)
        {
            return new Material(tilemapRenderer.sharedMaterial.shader) { name = "Moving Ground Path (runtime)" };
        }

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
        {
            return new Material(spriteRenderer.sharedMaterial.shader) { name = "Moving Ground Path (runtime)" };
        }

        Shader fallback = Shader.Find("Sprites/Default");
        return fallback != null ? new Material(fallback) { name = "Moving Ground Path (runtime)" } : null;
    }

    static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }
}
