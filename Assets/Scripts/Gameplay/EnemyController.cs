using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    public enum EnemyType
    {
        Flying,
        SurfaceAttached,
        Stationary
    }

    public enum PatrolMode
    {
        PingPong,
        Loop,
        Once,
        Random
    }

    [System.Flags]
    public enum SurfaceSides
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        All = Top | Right | Bottom | Left
    }

    public enum GateBehaviour
    {
        PassThrough,
        TurnAround,
        Block
    }

    #region Inspector Fields

    [Header("Patrol")]
    [Tooltip("Tipe gerak musuh: terbang bebas, menempel di permukaan platform, atau diam di tempat.")]
    public EnemyType enemyType = EnemyType.Flying;

    [Tooltip("Pola perjalanan antar waypoint: bolak-balik, memutar, sekali jalan lalu mati, atau acak.")]
    public PatrolMode patrolMode = PatrolMode.PingPong;

    [Tooltip("Daftar titik tujuan musuh. Urutannya menentukan arah patroli.")]
    public Transform[] waypoints;

    [Tooltip("Kecepatan gerak musuh (unit per detik).")]
    public float moveSpeed = 2f;

    [Tooltip("Jarak ke waypoint sampai waypoint dianggap tercapai.")]
    public float waypointReachDistance = 0.05f;

    [Tooltip("Jika aktif, posisi musuh langsung dipindahkan ke waypoint pertama saat scene mulai.")]
    public bool startAtFirstWaypoint = false;

    [Header("Flying")]
    [Tooltip("Layer penghalang untuk musuh terbang. Menabrak layer ini bisa membalik arah patroli.")]
    public LayerMask obstacleLayer;

    [Tooltip("Jarak berhenti sebelum menyentuh penghalang saat terbang.")]
    public float obstacleStopDistance = 0.2f;

    [Tooltip("Jika aktif, arah patroli dibalik ketika musuh terbang menabrak penghalang.")]
    public bool reverseOnObstacle = true;

    [Tooltip("Jeda minimum (detik) sebelum musuh terbang membalik arah lagi. Mencegah arah berbalik " +
             "bolak-balik setiap physics frame saat terjepit di penghalang.")]
    public float obstacleReverseCooldown = 0.25f;

    [Tooltip("Dorongan keluar (unit) saat musuh terbang sudah menempel atau menembus penghalang, " +
             "supaya tidak terjepit permanen.")]
    public float obstacleEscapeDistance = 0.15f;

    [Tooltip("Jika aktif, musuh terbang menembus semua platform: tidak berhenti di tepi platform, " +
             "tidak berbalik, dan tidak bertabrakan secara fisik dengannya.")]
    public bool flyThroughPlatforms = false;

    [Header("Surface Attached")]
    [Tooltip("Jarak pencarian permukaan (unit) saat Bug ditempelkan ke platform di awal level, " +
             "sama seperti Webi. Bug yang spawn sedikit di atas ground langsung menempel, jadi " +
             "tidak meluncur jatuh selama intro kamera. Isi 0 untuk mematikan snap awal.")]
    public float startSnapDistance = 4f;

    [Tooltip("Layer platform yang bisa ditempeli musuh jenis Surface Attached.")]
    public LayerMask platformLayer;

    [Tooltip("Sisi platform mana saja yang boleh dilewati musuh (atas, kanan, bawah, kiri).")]
    public SurfaceSides allowedSurfaceSides = SurfaceSides.All;

    [Tooltip("Jika aktif, badan musuh ikut berputar mengikuti kemiringan permukaan yang dipijak.")]
    public bool rotateToWaypoint = true;

    [Tooltip("Jarak deteksi ke depan dan ke bawah untuk mencari tepi atau tikungan platform.")]
    public float surfaceProbeDistance = 0.2f;

    [Tooltip("Tempel tiap waypoint ke titik terdekat di permukaan platform, jadi waypoint boleh ada di udara/di luar platform.")]
    public bool snapWaypointsToSurface = true;

    [Tooltip("Radius pencarian platform terdekat untuk snap waypoint.")]
    public float waypointSnapRadius = 10f;

    [Tooltip("Jarak (dari titik kontak bug ke waypoint yang sudah di-snap) supaya waypoint dianggap tercapai.")]
    public float surfaceWaypointTolerance = 0.3f;

    [Tooltip("Tampilkan panel status debug dan garis bantu di Scene view saat Play Mode.")]
    public bool debugMode;

    [Header("Scene Gizmos")]
    [Tooltip("Tampilkan jalur patroli dan panah arah gerak musuh di Scene view, juga saat tidak Play.")]
    public bool showGizmoDirection = true;

    [Tooltip("Tampilkan label waypoint (W1, W2, dst.) dan ringkasan pola gerak musuh di Scene view.")]
    public bool showGizmoLabels = true;

    [Header("Player Contact")]
    [Tooltip("Jika aktif, musuh ikut mati setelah menabrak pemain.")]
    public bool stopOnPlayerHit = true;

    [Tooltip("Jika aktif, animasi musuh dihentikan saat musuh mati.")]
    public bool stopAnimationOnDeath = true;

    [Header("Hazard")]
    [Tooltip("Jika aktif, musuh tidak tertahan duri/hazard: collider musuh diabaikan terhadap semua objek " +
             "ber-HazardSparkleEffect, jadi musuh bisa lewat di depan duri tanpa nyangkut. " +
             "Matikan kalau ada musuh yang memang harus terhalang duri.")]
    public bool passThroughHazards = true;

    [Header("Fellow Enemies")]
    [Tooltip("Jika aktif, musuh ini bisa menembus musuh lain (Bug atau Fly): collider musuh saling " +
             "diabaikan, jadi dua Bug yang berpapasan di jalur yang sama tidak saling mendorong dan " +
             "tersangkut. Matikan kalau ada musuh yang memang harus terhalang musuh lain.")]
    public bool passThroughEnemies = true;

    [Header("Gate")]
    [Tooltip("Apa yang dilakukan musuh saat ada pintu gerbang (objek ber-GateLeverController) di jalurnya. " +
             "Pass Through = musuh menembus gerbang, Turn Around = musuh berbalik sebelum menyentuhnya, " +
             "Block = gerbang tetap menghalangi seperti biasa.")]
    public GateBehaviour gateBehaviour = GateBehaviour.PassThrough;

    [Tooltip("Jarak pandang (unit) untuk mengenali gerbang tertutup di depan. Hanya dipakai saat " +
             "Gate Behaviour = Turn Around.")]
    public float gateLookAheadDistance = 1.2f;

    [Header("Audio")]
    [Tooltip("Suara langkah kaki musuh. Diputar per jeda selama musuh berjalan di permukaan.")]
    public AudioClip walkSfx;

    [Tooltip("Jeda antar suara langkah (detik).")]
    public float walkStepInterval = 0.45f;

    [Tooltip("Volume suara langkah kaki.")]
    public float walkSfxVolume = 0.7f;

    [Tooltip("Jika aktif, volume langkah mengecil mengikuti jarak ke Webi dan hilang saat Webi jauh.")]
    public bool walkSfxFollowsDistance = true;

    [Tooltip("Jarak (unit) saat langkah masih terdengar penuh.")]
    public float walkSfxNearDistance = 2f;

    [Tooltip("Jarak (unit) saat langkah sudah tidak terdengar.")]
    public float walkSfxFarDistance = 12f;

    #endregion

    #region Public Properties

    /// <summary>True setelah musuh mati (mis. tertabrak Webi) sehingga berhenti bergerak.</summary>
    public bool IsDead => isDead;

    #endregion

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform cachedPlayer;
    private ContactFilter2D platformFilter;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[8];
    private readonly RaycastHit2D[] surfaceHits = new RaycastHit2D[8];
    private readonly RaycastHit2D[] transitionHits = new RaycastHit2D[8];
    private int waypointIndex;
    private int patrolDirection = 1;
    private bool isDead;
    private bool canDriveBody;
    private bool hasLandedOnPlatform;
    private Vector2 currentSurfaceNormal = Vector2.up;
    private Vector2 currentForward = Vector2.right;
    private Collider2D currentSurfaceCollider;
    private Collider2D transitionTargetCollider;
    private Vector2 transitionStartPosition;
    private Vector2 transitionTargetPosition;
    private Vector2 transitionTargetNormal;
    private float transitionStartRotation;
    private float transitionTargetRotation;
    private float transitionElapsed;
    private bool isTransitioningSurface;
    private const float SurfaceTransitionDuration = 0.16f;
    private Vector2[] snappedWaypoints;
    private Collider2D[] snappedColliders;
    private Vector2[] snappedFaceNormals; // sisi platform tempat waypoint "menghadap" (zero = bebas)
    private int travelSign = 1; // +1 = searah jarum jam (forward), -1 = berlawanan
    private bool needsDirectionSelection = true;
    private float previousAhead = float.NaN;
    private string debugState = "Initializing";
    private string debugProbe = "Not checked";
    private string debugSupport = "Not checked";
    private Vector2 debugMoveDirection;
    private Vector2 debugProbePoint;
    private float walkStepTimer;
    private float lastObstacleReverseTime = float.NegativeInfinity;
    private int obstacleReverseCount;
    private string debugObstacle = "Not checked";
    private string debugGate = "Not checked";
    private readonly List<Collider2D> gateColliders = new List<Collider2D>();
    private float lastGateTurnTime = float.NegativeInfinity;
    private Vector2[] flyingWaypoints;
    private readonly List<Vector2> pathBuffer = new List<Vector2>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Body Static (misal Duri di Level 1) tidak bisa digerakkan lewat velocity / MovePosition.
        // Semua penulisan gerak dilewati untuk body seperti ini supaya tidak memunculkan error
        // "Cannot use 'linearVelocity' on a static body".
        canDriveBody = rb != null && rb.bodyType != RigidbodyType2D.Static;

        if (enemyType != EnemyType.SurfaceAttached && canDriveBody)
        {
            rb.gravityScale = 0f;
        }
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (enemyType == EnemyType.Flying)
        {
            rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        platformFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = platformLayer,
            useTriggers = false
        };
        obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleLayer,
            useTriggers = false
        };

        if (enemyType == EnemyType.Flying && flyThroughPlatforms)
        {
            ApplyFlyThroughPlatforms();
        }

        ApplyHazardPassThrough();
        ApplyEnemyPassThrough();
        ApplyGateBehaviour();

        if (enemyType == EnemyType.Stationary)
        {
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            enabled = false;
            return;
        }

        if (startAtFirstWaypoint)
        {
            rb.position = waypoints[0].position;
            if (enemyType == EnemyType.SurfaceAttached && rotateToWaypoint)
            {
                rb.rotation = waypoints[0].eulerAngles.z;
            }
        }

        // Di-cache setelah posisi awal diatur, supaya posisi dunia waypoint (yang mungkin masih anak
        // musuh) sama dengan posisi akhir saat scene mulai.
        if (enemyType == EnemyType.Flying)
        {
            CacheFlyingWaypoints();
        }

        waypointIndex = enemyType == EnemyType.SurfaceAttached ? 0 : waypoints.Length > 1 ? 1 : 0;

        if (enemyType == EnemyType.SurfaceAttached)
        {
            SnapToSurfaceAtStart();
        }
    }

    void FixedUpdate()
    {
        if (isDead || enemyType == EnemyType.Stationary || !canDriveBody)
        {
            StopBody();
            return;
        }

        if (enemyType == EnemyType.SurfaceAttached && !hasLandedOnPlatform)
        {
            debugState = "Waiting for top-side platform contact";
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        if (waypoints.Length == 1)
        {
            StopBody();
            return;
        }

        if (enemyType == EnemyType.SurfaceAttached)
        {
            HandleSurfacePatrol();
            return;
        }

        Transform waypoint = waypoints[waypointIndex];
        if (waypoint == null)
        {
            AdvanceWaypoint();
            return;
        }

        Vector2 targetPosition = GetFlyingTarget(waypointIndex);
        Vector2 toWaypoint = targetPosition - rb.position;
        if (toWaypoint.magnitude <= waypointReachDistance)
        {
            rb.position = targetPosition;
            AdvanceWaypoint();
            return;
        }

        Vector2 direction = toWaypoint.normalized;
        if (enemyType == EnemyType.Flying)
        {
            debugState = $"Flying to {waypoint.name}";
        }

        if (enemyType == EnemyType.Flying && spriteRenderer != null && !Mathf.Approximately(direction.x, 0f))
        {
            spriteRenderer.flipX = direction.x > 0f;
        }

        float moveDistance = moveSpeed * Time.fixedDeltaTime;

        if (enemyType == EnemyType.Flying && obstacleStopDistance > 0f)
        {
            int hitCount = enemyCollider.Cast(direction, obstacleFilter, obstacleHits, moveDistance + obstacleStopDistance);
            debugObstacle = "Clear";
            for (int i = 0; i < hitCount; i++)
            {
                if (obstacleHits[i].collider == null)
                {
                    continue;
                }

                RaycastHit2D hit = obstacleHits[i];
                float allowedDistance = hit.distance - obstacleStopDistance;
                debugObstacle = $"{hit.collider.name} at {hit.distance:F2} (allowed {allowedDistance:F2})";
                if (allowedDistance <= 0f)
                {
                    HandleFlyingObstacle(hit, toWaypoint.magnitude);
                    return;
                }

                moveDistance = Mathf.Min(moveDistance, allowedDistance);
                break;
            }
        }

        rb.MovePosition(rb.position + direction * moveDistance);

    }

    /// <summary>
    /// Musuh terbang yang menembus platform: layer platform dibuang dari deteksi penghalang, dan
    /// interaksi fisik dengan layer itu dimatikan per objek. Layer Collision Matrix global tidak
    /// diubah, jadi musuh lain (Bug) tetap menabrak platform seperti biasa.
    /// </summary>
    void ApplyFlyThroughPlatforms()
    {
        int platformBits = platformLayer.value != 0
            ? platformLayer.value
            : LayerMask.GetMask("Platform");

        if (platformBits == 0)
        {
            Debug.LogWarning(
                $"{name}: Fly Through Platforms aktif, tetapi layer platform tidak diketahui. " +
                "Isi Platform Layer di Inspector supaya musuh terbang bisa menembusnya.",
                this
            );
            return;
        }

        obstacleFilter.layerMask = obstacleLayer.value & ~platformBits;

        if (enemyCollider != null)
        {
            enemyCollider.excludeLayers |= platformBits;
        }

        if (rb != null)
        {
            rb.excludeLayers |= platformBits;
        }
    }

    /// <summary>
    /// Musuh terbang yang menabrak penghalang. Kalau sudah menempel atau menembus collider, body
    /// didorong keluar dulu; tanpa ini musuh terjepit permanen karena cast ke segala arah langsung
    /// "kena 0" sehingga arahnya berbalik tiap physics frame tanpa pernah bergerak.
    /// </summary>
    /// <summary>
    /// Mengabaikan benturan musuh terhadap semua hazard (duri). Tanpa ini musuh menempel/mendorong
    /// collider duri dan berhenti walau jalurnya masih terbuka.
    /// </summary>
    void ApplyHazardPassThrough()
    {
        if (!passThroughHazards || enemyCollider == null)
        {
            return;
        }

        HazardSparkleEffect[] hazards = FindObjectsByType<HazardSparkleEffect>(FindObjectsSortMode.None);
        for (int i = 0; i < hazards.Length; i++)
        {
            HazardSparkleEffect hazard = hazards[i];
            if (hazard == null ||
                hazard.transform == transform ||
                hazard.transform.IsChildOf(transform))
            {
                continue;
            }

            Collider2D dangerArea = hazard.dangerArea != null
                ? hazard.dangerArea
                : hazard.GetComponent<Collider2D>();
            if (dangerArea != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, dangerArea, true);
            }

            // Jaga-jaga kalau satu hazard punya lebih dari satu collider.
            Collider2D[] hazardColliders = hazard.GetComponents<Collider2D>();
            for (int c = 0; c < hazardColliders.Length; c++)
            {
                if (hazardColliders[c] != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, hazardColliders[c], true);
                }
            }
        }
    }

    /// <summary>
    /// Mengabaikan benturan musuh ini terhadap semua collider musuh lain (Bug maupun Fly). Dua Bug
    /// yang berjalan berlawanan arah tadinya saling mendorong lewat physics karena body-nya Dynamic
    /// dan didorong dengan MovePosition, sehingga keduanya berhenti di tempat; dengan opsi ini mereka
    /// lewat begitu saja. Kontak dengan Webi tidak berubah.
    /// </summary>
    void ApplyEnemyPassThrough()
    {
        if (!passThroughEnemies || enemyCollider == null)
        {
            return;
        }

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyController other = enemies[i];
            if (other == null || other == this)
            {
                continue;
            }

            // Satu musuh bisa punya lebih dari satu collider (mis. badan + trigger).
            Collider2D[] colliders = other.GetComponentsInChildren<Collider2D>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                Collider2D candidate = colliders[c];
                if (candidate == null || candidate == enemyCollider)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(enemyCollider, candidate, true);
            }
        }
    }

    /// <summary>
    /// Mendata collider gerbang (objek ber-GateLeverController) di scene, lalu kalau Gate Behaviour =
    /// Pass Through, mengabaikan benturannya terhadap musuh. Gerbang tertutup adalah collider fisik
    /// biasa, bukan platform dan bukan hazard, jadi tanpa ini Bug berhenti menempel di depan pintu.
    /// </summary>
    void ApplyGateBehaviour()
    {
        gateColliders.Clear();

        GateLeverController[] levers = FindObjectsByType<GateLeverController>(FindObjectsSortMode.None);
        for (int i = 0; i < levers.Length; i++)
        {
            GateLeverController lever = levers[i];
            if (lever == null || lever.gate == null)
            {
                continue;
            }

            Collider2D[] colliders = lever.gate.GetComponentsInChildren<Collider2D>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                RegisterGateCollider(colliders[c]);
            }

            // Collider gate bisa dibuat otomatis saat Awake (tidak selalu ada di prefab), jadi
            // rujukan dari controller-nya juga didata.
            RegisterGateCollider(lever.gateCollider);
        }

        debugGate = $"{gateColliders.Count} gate collider(s)";

        if (gateBehaviour != GateBehaviour.PassThrough || enemyCollider == null)
        {
            return;
        }

        for (int i = 0; i < gateColliders.Count; i++)
        {
            Physics2D.IgnoreCollision(enemyCollider, gateColliders[i], true);
        }
    }

    void RegisterGateCollider(Collider2D candidate)
    {
        if (candidate == null ||
            candidate.isTrigger ||
            candidate == enemyCollider ||
            gateColliders.Contains(candidate))
        {
            return;
        }

        gateColliders.Add(candidate);
    }

    /// <summary>
    /// Gerbang tertutup di depan jalur musuh. Dipakai Gate Behaviour = Turn Around supaya Bug berbalik
    /// sebelum menyentuh pintu, bukan menekan collider-nya terus-menerus. Gerbang yang sudah terbuka
    /// tidak dihitung karena collider-nya dimatikan.
    /// </summary>
    bool TryGetGateAhead(Vector2 moveDirection)
    {
        if (enemyCollider == null)
        {
            return false;
        }

        float lookAhead = Mathf.Max(gateLookAheadDistance, 0.01f);
        for (int i = 0; i < gateColliders.Count; i++)
        {
            Collider2D gateCollider = gateColliders[i];
            if (gateCollider == null ||
                !gateCollider.enabled ||
                !gateCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            ColliderDistance2D gap = enemyCollider.Distance(gateCollider);
            if (!gap.isOverlapped && gap.distance > lookAhead)
            {
                continue;
            }

            // Gerbang hanya dihitung kalau benar-benar di ARAH jalan musuh; gerbang di belakang
            // atau di samping tidak boleh dibalik.
            if (gap.isOverlapped)
            {
                debugGate = $"{gateCollider.name} (overlapping)";
                return true;
            }

            Vector2 toGate = gap.pointB - gap.pointA;
            if (toGate.sqrMagnitude < 0.0001f || Vector2.Dot(toGate.normalized, moveDirection) < 0.5f)
            {
                continue;
            }

            debugGate = $"{gateCollider.name} at {gap.distance:F2}";
            return true;
        }

        debugGate = "Clear";
        return false;
    }

    /// <summary>
    /// Balik arah di permukaan: arah jalan dibalik dan target dipindah ke waypoint sebelumnya, supaya
    /// Bug benar-benar menjauh dari gerbang, bukan berbalik lalu kembali ke target yang sama.
    /// </summary>
    void TurnAroundAtGate()
    {
        if (Time.time - lastGateTurnTime < Mathf.Max(obstacleReverseCooldown, 0.05f))
        {
            // Tunggu sebentar di tempat supaya Bug tidak bergetar di depan gerbang.
            debugState = "Gate ahead: waiting before turning";
            StopBody();
            return;
        }

        lastGateTurnTime = Time.time;
        travelSign *= -1;
        needsDirectionSelection = false;
        previousAhead = float.NaN;

        int previousIndex = waypointIndex + travelSign;
        if (waypointIndex >= 0 &&
            waypointIndex < waypoints.Length &&
            waypoints[waypointIndex] != null &&
            previousIndex >= 0 &&
            previousIndex < waypoints.Length &&
            waypoints[previousIndex] != null)
        {
            waypointIndex = previousIndex;
        }

        debugState = "Gate ahead: turned around";
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = travelSign < 0f;
        }
    }

    void HandleFlyingObstacle(RaycastHit2D hit, float targetDistance)
    {
        Vector2 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector2.zero;
        debugState = $"Blocked by {hit.collider.name}";

        if (hit.distance <= 0.01f && normal != Vector2.zero)
        {
            rb.MovePosition(rb.position + normal * Mathf.Max(obstacleEscapeDistance, 0.01f));
            debugObstacle += " | touching, pushed out";
        }

        if (!reverseOnObstacle || waypoints.Length < 2)
        {
            StopBody();
            return;
        }

        if (Time.time - lastObstacleReverseTime < Mathf.Max(obstacleReverseCooldown, 0f))
        {
            debugObstacle += " | waiting for reverse cooldown";
            return;
        }

        lastObstacleReverseTime = Time.time;
        obstacleReverseCount++;
        debugObstacle += $" | reversed #{obstacleReverseCount} (target distance {targetDistance:F2})";
        ReversePatrolDirection();
    }

    void HandleSurfacePatrol()
    {
        if (isTransitioningSurface)
        {
            HandleSurfaceTransition();
            return;
        }

        waypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        if (waypoints[waypointIndex] == null)
        {
            AdvanceWaypoint();
            return;
        }

        currentForward = GetForward(currentSurfaceNormal);
        Vector2 contactPoint = GetContactPoint();

        if (needsDirectionSelection)
        {
            SelectTravelDirection(contactPoint);
        }

        if (HasReachedWaypoint(contactPoint))
        {
            debugState = $"Reached waypoint {waypointIndex}";
            AdvanceWaypoint();
            return;
        }

        float moveSign = travelSign;
        Vector2 moveDirection = currentForward * moveSign;
        debugMoveDirection = moveDirection;
        debugState = "Walking on current surface";
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = moveSign < 0f;
        }

        if (gateBehaviour == GateBehaviour.TurnAround && TryGetGateAhead(moveDirection))
        {
            TurnAroundAtGate();
            return;
        }

        if (TryTransitionToNextSurface(moveDirection, out Vector2 nextNormal, out Vector2 nextPosition))
        {
            BeginSurfaceTransition(nextNormal, nextPosition);
            return;
        }

        int hitCount = enemyCollider.Cast(
            moveDirection,
            platformFilter,
            surfaceHits,
            Mathf.Max(surfaceProbeDistance, 0.05f)
        );
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = surfaceHits[i];
            if (hit.collider == null ||
                Vector2.Angle(currentSurfaceNormal, hit.normal) >= 20f)
            {
                continue;
            }

            currentSurfaceNormal = hit.normal.normalized;
            currentForward = GetForward(currentSurfaceNormal);
            break;
        }

        RaycastHit2D supportHit = default;
        float closestSupportDistance = float.PositiveInfinity;
        int supportHitCount = enemyCollider.Cast(
            -currentSurfaceNormal,
            platformFilter,
            surfaceHits,
            2f
        );
        bool rejectedByCollider = false;
        bool rejectedByNormal = false;
        for (int i = 0; i < supportHitCount; i++)
        {
            RaycastHit2D hit = surfaceHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (currentSurfaceCollider != null && hit.collider != currentSurfaceCollider)
            {
                rejectedByCollider = true;
                continue;
            }

            if (Vector2.Dot(hit.normal.normalized, currentSurfaceNormal) < 0.8f)
            {
                rejectedByNormal = true;
                continue;
            }

            if (hit.distance >= closestSupportDistance)
            {
                continue;
            }

            supportHit = hit;
            closestSupportDistance = hit.distance;
        }

        if (supportHit.collider == null)
        {
            // Cast collider tidak melaporkan platform yang sudah ditempel (hit jarak 0 dengan
            // normal nol), jadi ray pendek dari titik kontak dipakai seperti cadangan Webi.
            // Tanpa ini Bug kehilangan pijakan di ground miring lalu berhenti di tengah jalan.
            RaycastHit2D raySupport = ProbeSupportRay(currentSurfaceNormal);
            if (raySupport.collider != null &&
                Vector2.Dot(raySupport.normal.normalized, currentSurfaceNormal) >= 0.8f)
            {
                supportHit = raySupport;
                currentSurfaceCollider = raySupport.collider;
            }
        }

        if (supportHit.collider == null)
        {
            debugSupport = supportHitCount == 0
                ? "Support cast returned no hits"
                : $"Rejected {supportHitCount} hit(s); collider mismatch={rejectedByCollider}, " +
                  $"normal mismatch={rejectedByNormal}, expected={currentSurfaceCollider?.name ?? "none"}";
            debugState = "Stopped: platform support not found";
            rb.linearVelocity = Vector2.zero;
            return;
        }

        debugSupport = $"{supportHit.collider.name} ({supportHit.normal})";
        currentForward = GetForward(currentSurfaceNormal);
        if (rotateToWaypoint)
        {
            rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));
        }

        Vector2 snappedPosition = GetSnappedPosition(currentSurfaceNormal);
        snappedPosition += currentForward * (moveSign * moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(snappedPosition);
        rb.linearVelocity = Vector2.zero;
        PlayWalkStep();
    }

    void BeginSurfaceTransition(Vector2 nextNormal, Vector2 nextPosition)
    {
        transitionStartPosition = rb.position;
        transitionTargetPosition = nextPosition;
        transitionTargetNormal = nextNormal;
        transitionStartRotation = rb.rotation;
        transitionTargetRotation = GetSurfaceAngle(nextNormal);
        transitionElapsed = 0f;
        isTransitioningSurface = true;
        debugState = $"Transitioning to surface normal {nextNormal}";
        debugProbe = $"Transition target: {transitionTargetCollider?.name ?? "unknown"} at {nextPosition}";
    }

    void HandleSurfaceTransition()
    {
        transitionElapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(transitionElapsed / SurfaceTransitionDuration);
        float easedProgress = progress * progress * (3f - 2f * progress);
        rb.MovePosition(Vector2.Lerp(transitionStartPosition, transitionTargetPosition, easedProgress));
        if (rotateToWaypoint)
        {
            rb.MoveRotation(Mathf.LerpAngle(transitionStartRotation, transitionTargetRotation, easedProgress));
        }

        rb.linearVelocity = Vector2.zero;

        if (progress >= 1f)
        {
            currentSurfaceNormal = transitionTargetNormal;
            currentSurfaceCollider = transitionTargetCollider;
            currentForward = GetForward(currentSurfaceNormal);
            isTransitioningSurface = false;
            debugState = $"Transition complete; support {currentSurfaceCollider?.name ?? "unknown"}";
        }
    }

    bool TryTransitionToNextSurface(Vector2 moveDirection, out Vector2 nextNormal, out Vector2 nextPosition)
    {
        nextNormal = currentSurfaceNormal;
        nextPosition = rb.position;
        transitionTargetCollider = null;
        Vector2 candidateDirection = moveDirection.normalized;
        int hitCount = enemyCollider.Cast(
            candidateDirection,
            platformFilter,
            transitionHits,
            Mathf.Max(surfaceProbeDistance, 0.05f)
        );
        debugProbe = $"Forward cast: {hitCount} hit(s)";

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = transitionHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            Vector2 candidateNormal = hit.normal.normalized;
            if (Vector2.Angle(currentSurfaceNormal, candidateNormal) < 20f ||
                Vector2.Dot(candidateNormal, candidateDirection) > -0.8f ||
                !IsAllowedSurface(candidateNormal))
            {
                continue;
            }

            nextNormal = candidateNormal;
            nextPosition = hit.point + candidateNormal * GetTargetSurfaceOffset(candidateNormal);
            transitionTargetCollider = hit.collider;
            debugProbe = $"Forward cast found {hit.collider.name}, normal {candidateNormal}";
            return true;
        }

        Vector2 contactPoint = GetContactPoint();
        float probeDistance = Mathf.Max(surfaceProbeDistance, 0.05f);
        Vector2 pointAhead = contactPoint + candidateDirection * probeDistance;
        debugProbePoint = pointAhead;
        RaycastHit2D surfaceAhead = Physics2D.Raycast(
            pointAhead,
            -currentSurfaceNormal,
            2f,
            platformLayer
        );

        if (surfaceAhead.collider == null)
        {
            debugProbe = "No support ahead; probing around corner";
            Vector2 cornerProbeOrigin = pointAhead -
                currentSurfaceNormal * Mathf.Max(GetSurfaceOffset(currentSurfaceNormal), 0.01f);
            debugProbePoint = cornerProbeOrigin;
            RaycastHit2D cornerHit = Physics2D.Raycast(
                cornerProbeOrigin,
                -candidateDirection,
                2f,
                platformLayer
            );

            if (cornerHit.collider != null &&
                IsAllowedSurface(cornerHit.normal))
            {
                nextNormal = cornerHit.normal.normalized;
                nextPosition = cornerHit.point + nextNormal * GetTargetSurfaceOffset(nextNormal);
                transitionTargetCollider = cornerHit.collider;
                debugProbe = $"Corner probe found {cornerHit.collider.name}, normal {nextNormal}";
                return true;
            }

            debugProbe = cornerHit.collider == null
                ? "Corner probe missed platform"
                : $"Corner normal not allowed: {cornerHit.normal}";
        }
        else
        {
            debugProbe = $"Support still ahead: {surfaceAhead.collider.name} ({surfaceAhead.distance:F2}); " +
                $"probe {probeDistance:F2}";
        }

        return false;
    }

    Vector2 GetSnappedPosition(Vector2 surfaceNormal)
    {
        int hitCount = enemyCollider.Cast(
            -surfaceNormal,
            platformFilter,
            surfaceHits,
            2f
        );
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = surfaceHits[i];
            if (hit.collider == null ||
                (currentSurfaceCollider != null && hit.collider != currentSurfaceCollider) ||
                Vector2.Dot(hit.normal.normalized, surfaceNormal.normalized) < 0.8f)
            {
                continue;
            }

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        if (float.IsPositiveInfinity(closestDistance))
        {
            RaycastHit2D raySupport = ProbeSupportRay(surfaceNormal);
            if (raySupport.collider == null)
            {
                return rb.position;
            }

            float lift = Mathf.Max(surfaceProbeDistance, 0.1f);
            closestDistance = Mathf.Max(0f, raySupport.distance - lift);
        }

        return rb.position - surfaceNormal * closestDistance;
    }

    float GetSurfaceOffset(Vector2 surfaceNormal)
    {
        Bounds bounds = enemyCollider.bounds;
        float probeDistance = Vector2.Distance(rb.position, bounds.center) + bounds.extents.magnitude + 1f;
        Vector2 probePoint = rb.position - surfaceNormal * probeDistance;
        Vector2 supportPoint = enemyCollider.ClosestPoint(probePoint);
        return Vector2.Dot(rb.position - supportPoint, surfaceNormal);
    }

    Vector2 GetContactPoint()
    {
        return rb.position - currentSurfaceNormal * GetSurfaceOffset(currentSurfaceNormal);
    }

    // Collider ikut berputar bersama body, jadi jarak pivot -> permukaan (sisi "bawah" lokal bug)
    // selalu sama di permukaan mana pun. GetSurfaceOffset(nextNormal) memakai orientasi LAMA
    // (belum berputar), jadi hasilnya salah untuk collider yang tidak simetris seperti kapsul 6x4.5.
    float GetTargetSurfaceOffset(Vector2 targetNormal)
    {
        return rotateToWaypoint
            ? GetSurfaceOffset(currentSurfaceNormal)
            : GetSurfaceOffset(targetNormal);
    }

    void BuildSurfaceWaypoints()
    {
        snappedWaypoints = new Vector2[waypoints.Length];
        snappedColliders = new Collider2D[waypoints.Length];
        snappedFaceNormals = new Vector2[waypoints.Length];
        Physics2D.SyncTransforms();

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Vector2 raw = waypoints[i].position;
            snappedWaypoints[i] = raw;
            if (!snapWaypointsToSurface)
            {
                continue;
            }

            Collider2D[] nearby = Physics2D.OverlapCircleAll(
                raw,
                Mathf.Max(waypointSnapRadius, 0.1f),
                platformLayer.value
            );
            float bestSqrDistance = float.PositiveInfinity;
            for (int c = 0; c < nearby.Length; c++)
            {
                Collider2D candidate = nearby[c];
                if (candidate == null || candidate.isTrigger || candidate == enemyCollider)
                {
                    continue;
                }

                Vector2 closest = SnapPointToBoundary(candidate, raw, out Vector2 outsidePoint);
                float sqrDistance = (closest - raw).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    snappedWaypoints[i] = closest;
                    snappedColliders[i] = candidate;
                    snappedFaceNormals[i] = GetWaypointFace(candidate, outsidePoint, closest);
                }
            }

            if (snappedColliders[i] == null)
            {
                Debug.LogWarning(
                    $"{name}: {waypoints[i].name} tidak menemukan platform dalam radius {waypointSnapRadius}. " +
                    "Dekatkan waypoint ke platform atau naikkan Waypoint Snap Radius.",
                    this
                );
            }
        }
    }

    // Satu CompositeCollider2D bisa menggabungkan SEMUA gumpalan tanah level jadi satu collider
    // dengan banyak path. bounds-nya lalu mencakup seluruh level, bukan platform yang sedang dipijak,
    // sehingga sisi waypoint dan arah keliling jadi kacau (musuh berbalik arah di tengah platform).
    // Jadi pakai bounds path yang paling dekat dengan titik yang sedang diperiksa.
    Bounds GetSurfaceBounds(Collider2D collider, Vector2 point)
    {
        CompositeCollider2D composite = collider as CompositeCollider2D;
        if (composite == null || composite.pathCount <= 1)
        {
            return collider.bounds;
        }

        Bounds result = collider.bounds;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < composite.pathCount; i++)
        {
            pathBuffer.Clear();
            composite.GetPath(i, pathBuffer);
            if (pathBuffer.Count == 0)
            {
                continue;
            }

            Vector2 min = pathBuffer[0];
            Vector2 max = pathBuffer[0];
            for (int p = 1; p < pathBuffer.Count; p++)
            {
                min = Vector2.Min(min, pathBuffer[p]);
                max = Vector2.Max(max, pathBuffer[p]);
            }

            Vector2 closestOnPath = new Vector2(
                Mathf.Clamp(point.x, min.x, max.x),
                Mathf.Clamp(point.y, min.y, max.y)
            );
            float distance = (closestOnPath - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                result = new Bounds((Vector3)((min + max) * 0.5f), (Vector3)(max - min));
            }
        }

        return result;
    }

    Vector2 SnapPointToBoundary(Collider2D collider, Vector2 point, out Vector2 outsidePoint)
    {
        if (collider.OverlapPoint(point))
        {
            // Waypoint ada DI DALAM collider: dorong keluar lewat sisi bounds path terdekat.
            Bounds b = GetSurfaceBounds(collider, point);
            float toLeft = point.x - b.min.x;
            float toRight = b.max.x - point.x;
            float toBottom = point.y - b.min.y;
            float toTop = b.max.y - point.y;
            float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
            const float push = 0.05f;
            if (nearest == toLeft) point.x = b.min.x - push;
            else if (nearest == toRight) point.x = b.max.x + push;
            else if (nearest == toBottom) point.y = b.min.y - push;
            else point.y = b.max.y + push;
        }

        outsidePoint = point;
        return collider.ClosestPoint(point);
    }

    // Sisi platform yang "dihadapi" waypoint. Untuk platform lurus (rotasi kelipatan 90 derajat) dipilih
    // sisi yang paling jauh dilewati waypoint, dinormalisasi terhadap ukuran platform. Jadi waypoint di
    // pojok kiri-bawah yang cuma menjorok sedikit ke kiri tapi jauh di bawah tetap dihitung sisi BAWAH.
    Vector2 GetWaypointFace(Collider2D collider, Vector2 outsidePoint, Vector2 closest)
    {
        float rotZ = collider.transform.eulerAngles.z;
        bool axisAligned = Mathf.Abs(Mathf.DeltaAngle(rotZ, Mathf.Round(rotZ / 90f) * 90f)) < 1f;

        if (!axisAligned)
        {
            Vector2 away = outsidePoint - closest;
            return away.magnitude > 0.05f ? away.normalized : Vector2.zero;
        }

        Bounds b = GetSurfaceBounds(collider, closest);
        float halfX = Mathf.Max(b.extents.x, 0.01f);
        float halfY = Mathf.Max(b.extents.y, 0.01f);
        float left = (b.min.x - outsidePoint.x) / halfX;
        float right = (outsidePoint.x - b.max.x) / halfX;
        float down = (b.min.y - outsidePoint.y) / halfY;
        float up = (outsidePoint.y - b.max.y) / halfY;

        float best = Mathf.Max(Mathf.Max(left, right), Mathf.Max(down, up));
        if (best <= 0f)
        {
            return Vector2.zero;
        }

        if (best == up) return Vector2.up;
        if (best == down) return Vector2.down;
        if (best == left) return Vector2.left;
        return Vector2.right;
    }

    // Waypoint hanya dianggap tercapai kalau bug ada di sisi platform yang "dihadapi" waypoint
    // (mis. waypoint di bawah platform -> bug harus sudah di sisi bawah, bukan masih di dinding kiri).
    bool IsOnWaypointFace(int index)
    {
        if (snappedFaceNormals == null || index < 0 || index >= snappedFaceNormals.Length)
        {
            return true;
        }

        Vector2 face = snappedFaceNormals[index];
        if (face == Vector2.zero || !IsAllowedSurface(face))
        {
            return true;
        }

        return Vector2.Dot(currentSurfaceNormal, face) > 0.5f;
    }

    Vector2 GetTargetPosition(int index)
    {
        if (snappedWaypoints != null && index >= 0 && index < snappedWaypoints.Length)
        {
            return snappedWaypoints[index];
        }

        return waypoints[index].position;
    }

    bool IsSameSurfaceBody(Collider2D a, Collider2D b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return a == b || (a.attachedRigidbody != null && a.attachedRigidbody == b.attachedRigidbody);
    }

    // Selisih sudut (derajat) dari posisi bug ke waypoint, diukur mengelilingi pusat platform.
    // Negatif = waypoint ada di arah searah jarum jam, positif = berlawanan.
    bool TryGetPerimeterDelta(Vector2 contactPoint, int index, out float delta)
    {
        delta = 0f;
        if (snappedColliders == null ||
            index < 0 || index >= snappedColliders.Length ||
            !IsSameSurfaceBody(snappedColliders[index], currentSurfaceCollider))
        {
            return false;
        }

        Bounds targetBounds = GetSurfaceBounds(snappedColliders[index], GetTargetPosition(index));
        Bounds contactBounds = GetSurfaceBounds(snappedColliders[index], contactPoint);
        if ((targetBounds.center - contactBounds.center).sqrMagnitude > 0.25f)
        {
            // Waypoint ada di gumpalan tanah lain yang kebetulan satu composite collider.
            return false;
        }

        Vector2 center = targetBounds.center;
        Vector2 toContact = contactPoint - center;
        Vector2 toTarget = GetTargetPosition(index) - center;
        if (toContact.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float contactAngle = Mathf.Atan2(toContact.y, toContact.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        delta = Mathf.DeltaAngle(contactAngle, targetAngle);
        return true;
    }

    // Dipanggil SEKALI tiap target waypoint berganti. Setelah itu arah jalan ditahan (momentum),
    // jadi bug tidak balik arah di tengah tikungan hanya karena sumbu "forward"-nya berubah.
    void SelectTravelDirection(Vector2 contactPoint)
    {
        needsDirectionSelection = false;
        previousAhead = float.NaN;

        if (TryGetPerimeterDelta(contactPoint, waypointIndex, out float delta))
        {
            if (Mathf.Abs(delta) > 1f)
            {
                travelSign = delta < 0f ? 1 : -1;
            }

            return;
        }

        // Fallback (waypoint tidak menempel ke platform yang sedang dipijak): pakai proyeksi.
        float along = Vector2.Dot(GetTargetPosition(waypointIndex) - contactPoint, currentForward);
        if (Mathf.Abs(along) > Mathf.Max(surfaceWaypointTolerance, 0.5f))
        {
            travelSign = along > 0f ? 1 : -1;
        }
    }

    bool HasReachedWaypoint(Vector2 contactPoint)
    {
        float tolerance = Mathf.Max(
            waypointReachDistance,
            surfaceWaypointTolerance,
            moveSpeed * Time.fixedDeltaTime * 1.5f
        );

        bool onFace = IsOnWaypointFace(waypointIndex);
        if (onFace && Vector2.Distance(contactPoint, GetTargetPosition(waypointIndex)) <= tolerance)
        {
            return true;
        }

        // Cadangan: kalau waypoint terlewat (mis. ada di sisi yang tidak diizinkan), anggap tercapai.
        if (TryGetPerimeterDelta(contactPoint, waypointIndex, out float delta))
        {
            float ahead = travelSign > 0 ? -delta : delta;
            bool passed = !float.IsNaN(previousAhead) &&
                previousAhead > 0f && previousAhead < 90f &&
                ahead <= 0f && ahead > -90f;
            previousAhead = ahead;
            return passed && onFace;
        }

        previousAhead = float.NaN;
        return false;
    }

    Vector2 GetForward(Vector2 surfaceNormal)
    {
        return new Vector2(surfaceNormal.y, -surfaceNormal.x).normalized;
    }

    float GetSurfaceAngle(Vector2 surfaceNormal)
    {
        return Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg - 90f;
    }

    // SFX langkah diputar per jeda lewat AudioManager (bukan loop AudioSource) supaya ikut
    // toggle SFX di Settings dan berhenti sendiri saat musuh berhenti atau mati.
    // Volumenya ikut mengecil saat Webi menjauh, jadi suara hanya terdengar di dekat musuh.
    void PlayWalkStep()
    {
        if (walkSfx == null)
        {
            return;
        }

        walkStepTimer -= Time.fixedDeltaTime;
        if (walkStepTimer > 0f)
        {
            return;
        }

        walkStepTimer = Mathf.Max(0.05f, walkStepInterval);

        float volume = walkSfxVolume;
        if (walkSfxFollowsDistance)
        {
            volume *= GetPlayerProximityFactor(walkSfxNearDistance, walkSfxFarDistance);
        }

        if (volume <= 0.001f || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(walkSfx, 1f, 1f, volume);
    }

    /// <summary>Pengali volume (0..1) berdasarkan jarak musuh ke Webi.</summary>
    float GetPlayerProximityFactor(float near, float far)
    {
        Transform player = GetPlayerTransform();
        if (player == null)
        {
            return 1f;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        return ProximitySfx.VolumeFactor(distance, near, far);
    }

    /// <summary>Transform Webi; dicari lewat tag Player sekali lalu disimpan.</summary>
    Transform GetPlayerTransform()
    {
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        GameObject player = GameObject.FindWithTag("Player");
        cachedPlayer = player != null ? player.transform : null;
        return cachedPlayer;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsPlayer(collision.gameObject))
        {
            HandlePlayerContact(collision.gameObject);
            return;
        }

        TryLandOnPlatform(collision);

        if (enemyType == EnemyType.Flying && IsInLayer(collision.gameObject, obstacleLayer))
        {
            ReversePatrolDirection();
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryLandOnPlatform(collision);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            HandlePlayerContact(other.gameObject);
        }
    }

    void ReversePatrolDirection()
    {
        if (!reverseOnObstacle || waypoints.Length < 2)
        {
            StopBody();
            return;
        }

        patrolDirection *= -1;
        waypointIndex = Mathf.Clamp(waypointIndex + patrolDirection, 0, waypoints.Length - 1);
    }

    void TryLandOnPlatform(Collision2D collision)
    {
        if (enemyType != EnemyType.SurfaceAttached
            || hasLandedOnPlatform
            || !IsInLayer(collision.gameObject, platformLayer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (!IsAllowedSurface(contact.normal))
            {
                continue;
            }

            BuildSurfaceWaypoints();

            // Patroli selalu mulai dari waypoint PERTAMA (W1), bukan dari waypoint terdekat.
            // Dulu target pertama diambil dari yang terdekat lalu dimajukan satu langkah, jadi
            // musuh yang mendarat di dekat W4 langsung jalan mundur (W4 -> W3 -> W2 -> W1).
            waypointIndex = 0;
            patrolDirection = 1;

            needsDirectionSelection = true;

            walkStepTimer = 0f;
            hasLandedOnPlatform = true;
            currentSurfaceNormal = contact.normal.normalized;
            currentSurfaceCollider = collision.collider;
            currentForward = GetForward(currentSurfaceNormal);
            if (canDriveBody)
            {
                rb.gravityScale = 0f;
                StopBody();
                if (rotateToWaypoint)
                {
                    rb.MoveRotation(GetSurfaceAngle(currentSurfaceNormal));
                }
            }
            debugSupport = $"{collision.gameObject.name} ({currentSurfaceNormal})";
            debugState = $"Landed on {collision.gameObject.name}";
            return;
        }
    }

    /// <summary>
    /// Menempelkan Bug ke permukaan platform di bawahnya saat level mulai, seperti Webi
    /// (PlayerController.SnapToSurfaceBelow). Tanpa ini Bug dijatuhkan gravitasi dulu: di ground
    /// miring ia meluncur turun sebelum sempat menempel, dan karena intro kamera menutup awal
    /// level, hasilnya terlihat seperti Bug sudah jatuh begitu permainan dimulai.
    /// </summary>
    void SnapToSurfaceAtStart()
    {
        if (enemyCollider == null || rb == null || !canDriveBody || startSnapDistance <= 0f)
        {
            return;
        }

        RaycastHit2D closestHit = FindStartSurface();

        if (closestHit.collider == null)
        {
            // Tidak ada permukaan dekat: biarkan gravitasi menjatuhkan Bug, lalu pendaratan biasa
            // (TryLandOnPlatform) yang menempelkannya.
            return;
        }

        Vector2 surfaceNormal = closestHit.normal.normalized;
        rb.position = closestHit.point;
        if (rotateToWaypoint)
        {
            rb.rotation = GetSurfaceAngle(surfaceNormal);
        }

        // Collider ikut berputar, jadi jarak pivot ke permukaan baru bisa diukur setelah rotasi
        // diterapkan ke physics.
        Physics2D.SyncTransforms();
        rb.position = closestHit.point + surfaceNormal * GetSurfaceOffset(surfaceNormal);
        rb.gravityScale = 0f;
        StopBody();

        currentSurfaceNormal = surfaceNormal;
        currentSurfaceCollider = closestHit.collider;
        currentForward = GetForward(surfaceNormal);
        hasLandedOnPlatform = true;
        waypointIndex = 0;
        patrolDirection = 1;
        needsDirectionSelection = true;
        previousAhead = float.NaN;
        walkStepTimer = 0f;

        // Waypoint adalah anak Bug, jadi posisi dunianya baru benar setelah Bug dipindahkan.
        BuildSurfaceWaypoints();

        debugSupport = closestHit.collider.name;
        debugState = $"Snapped to {closestHit.collider.name} at start";
    }

    /// <summary>
    /// Mencari permukaan tempat Bug ditempel saat level mulai. Pertama dengan cast collider ke
    /// bawah; kalau tidak ada (Bug spawn menembus ground sehingga cast mulai di dalam collider),
    /// ray pendek di dalam telapak collider dipakai. Hit dengan normal nol dibuang karena tidak
    /// bisa dipakai sebagai arah permukaan.
    /// </summary>
    RaycastHit2D FindStartSurface()
    {
        RaycastHit2D closestHit = default;
        int hitCount = enemyCollider.Cast(
            Vector2.down,
            platformFilter,
            surfaceHits,
            Mathf.Max(startSnapDistance, 0.1f)
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = surfaceHits[i];
            if (hit.collider == null ||
                hit.normal.sqrMagnitude < 0.0001f ||
                !IsAllowedSurface(hit.normal.normalized))
            {
                continue;
            }

            if (closestHit.collider == null || hit.distance < closestHit.distance)
            {
                closestHit = hit;
            }
        }

        if (closestHit.collider != null)
        {
            return closestHit;
        }

        Bounds bounds = enemyCollider.bounds;
        RaycastHit2D embeddedHit = Physics2D.Raycast(
            new Vector2(bounds.center.x, bounds.max.y + 0.5f),
            Vector2.down,
            bounds.size.y + 1f,
            platformLayer
        );

        if (embeddedHit.collider != null &&
            embeddedHit.normal.sqrMagnitude > 0.0001f &&
            IsAllowedSurface(embeddedHit.normal.normalized))
        {
            return embeddedHit;
        }

        return default;
    }

    /// <summary>
    /// Ray pendek dari titik kontak menembus permukaan, cadangan saat cast collider tidak
    /// melaporkan platform yang sedang dipijak (collider yang menempel atau tertanam
    /// menghasilkan hit jarak 0 dengan normal nol).
    /// </summary>
    RaycastHit2D ProbeSupportRay(Vector2 surfaceNormal)
    {
        Vector2 normal = surfaceNormal.normalized;
        float lift = Mathf.Max(surfaceProbeDistance, 0.1f);
        Vector2 origin = GetContactPoint() + normal * lift;
        return Physics2D.Raycast(origin, -normal, lift * 4f, platformLayer);
    }

    bool IsAllowedSurface(Vector2 normal)
    {
        SurfaceSides side;
        if (Mathf.Abs(normal.y) >= Mathf.Abs(normal.x))
        {
            side = normal.y >= 0f ? SurfaceSides.Top : SurfaceSides.Bottom;
        }
        else
        {
            side = normal.x >= 0f ? SurfaceSides.Right : SurfaceSides.Left;
        }

        return (allowedSurfaceSides & side) != 0;
    }

    void AdvanceWaypoint()
    {
        needsDirectionSelection = true;
        previousAhead = float.NaN;

        switch (patrolMode)
        {
            case PatrolMode.PingPong:
                int nextIndex = waypointIndex + patrolDirection;
                if (nextIndex < 0 || nextIndex >= waypoints.Length)
                {
                    patrolDirection *= -1;
                    nextIndex = waypointIndex + patrolDirection;
                }

                waypointIndex = Mathf.Clamp(nextIndex, 0, waypoints.Length - 1);
                break;

            case PatrolMode.Loop:
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                break;

            case PatrolMode.Once:
                if (waypointIndex == waypoints.Length - 1)
                {
                    Die();
                }
                else
                {
                    waypointIndex++;
                }
                break;

            case PatrolMode.Random:
                int randomIndex = Random.Range(0, waypoints.Length - 1);
                if (randomIndex >= waypointIndex)
                {
                    randomIndex++;
                }

                waypointIndex = randomIndex;
                break;
        }
    }

    void HandlePlayerContact(GameObject player)
    {
        // Webi memegang seluruh alur kematian (memunculkan popup lewat event Died);
        // fallback lama tetap dipakai kalau objek pemain tidak punya PlayerController.
        PlayerController playerController = player.GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerController.Die();
        }
        else
        {
            player.SetActive(false);
        }

        if (stopOnPlayerHit)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        walkStepTimer = 0f;
        StopBody();

        if (stopAnimationOnDeath && animator != null)
        {
            animator.speed = 0f;
        }
    }

    /// <summary>
    /// Hentikan gerak body. Dilewati untuk body Static (misal Duri) karena set velocity ke body
    /// Static memunculkan error "Cannot use 'linearVelocity' on a static body".
    /// </summary>
    void StopBody()
    {
        if (!canDriveBody)
        {
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    bool IsPlayer(GameObject other)
    {
        return other.CompareTag("Player");
    }

    bool IsInLayer(GameObject other, LayerMask layerMask)
    {
        return (layerMask.value & (1 << other.layer)) != 0;
    }

    void OnGUI()
    {
        if (!debugMode || !Application.isPlaying)
        {
            return;
        }

        string waypointName = waypoints != null &&
            waypointIndex >= 0 &&
            waypointIndex < waypoints.Length &&
            waypoints[waypointIndex] != null
                ? waypoints[waypointIndex].name
                : "None";

        GUILayout.BeginArea(new Rect(10f, 10f, 470f, 300f), GUI.skin.box);
        GUILayout.Label($"Enemy: {name} | Type: {enemyType} | Mode: {patrolMode} | State: {debugState}");
        GUILayout.Label($"Position: {rb.position} | Velocity: {rb.linearVelocity} | " +
            $"Body: {(rb != null ? rb.bodyType.ToString() : "none")} | CanDrive: {canDriveBody}");
        GUILayout.Label($"Waypoint: {waypointIndex} ({waypointName}) | Direction: {patrolDirection} | " +
            $"Travel: {(travelSign > 0 ? "CW" : "CCW")} | Target distance: {GetTargetDistance():F2}");

        if (enemyType == EnemyType.Flying)
        {
            string lastReverse = float.IsNegativeInfinity(lastObstacleReverseTime)
                ? "never"
                : $"{Time.time - lastObstacleReverseTime:F2}s ago";
            GUILayout.Label($"Obstacle: {debugObstacle} | Reversals: {obstacleReverseCount} ({lastReverse})");
        }

        GUILayout.Label($"Surface normal: {currentSurfaceNormal} | Forward: {currentForward}");
        GUILayout.Label($"Move direction: {debugMoveDirection} | Transition: {isTransitioningSurface}");
        GUILayout.Label($"Support: {debugSupport}");
        if (snappedFaceNormals != null && waypointIndex >= 0 && waypointIndex < snappedFaceNormals.Length)
        {
            GUILayout.Label($"Target face: {snappedFaceNormals[waypointIndex]} | On face: {IsOnWaypointFace(waypointIndex)} | " +
                $"Dist: {Vector2.Distance(GetContactPoint(), GetTargetPosition(waypointIndex)):F2}");
        }
        GUILayout.Label($"Probe: {debugProbe}");
        GUILayout.Label($"Platform mask: {platformLayer.value} | Obstacle mask: {obstacleLayer.value}");
        GUILayout.Label($"Gate: {gateBehaviour} | Ahead: {debugGate}");
        GUILayout.EndArea();
    }

    /// <summary>
    /// Posisi dunia waypoint musuh terbang disimpan sekali saat Start. Waypoint boleh tetap menjadi
    /// anak musuh (hierarki tetap rapi), tetapi kalau posisinya dibaca ulang tiap frame, targetnya ikut
    /// bergerak bersama musuh sehingga jaraknya tidak pernah menyusut. Bug memakai cara yang sama lewat
    /// BuildSurfaceWaypoints().
    /// </summary>
    void CacheFlyingWaypoints()
    {
        flyingWaypoints = new Vector2[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            flyingWaypoints[i] = waypoints[i] != null ? (Vector2)waypoints[i].position : Vector2.zero;
        }
    }

    /// <summary>Posisi tujuan musuh terbang. Memakai cache dunia; kalau belum ada, pakai posisi live.</summary>
    Vector2 GetFlyingTarget(int index)
    {
        if (flyingWaypoints != null && index >= 0 && index < flyingWaypoints.Length)
        {
            return flyingWaypoints[index];
        }

        return waypoints[index] != null ? (Vector2)waypoints[index].position : (Vector2)transform.position;
    }

    /// <summary>Jarak body ke waypoint yang sedang dituju (0 kalau waypoint belum diisi).</summary>
    float GetTargetDistance()
    {
        if (waypoints == null ||
            waypointIndex < 0 ||
            waypointIndex >= waypoints.Length ||
            waypoints[waypointIndex] == null ||
            rb == null)
        {
            return 0f;
        }

        Vector2 target = enemyType == EnemyType.Flying
            ? GetFlyingTarget(waypointIndex)
            : (Vector2)waypoints[waypointIndex].position;
        return Vector2.Distance(rb.position, target);
    }

    void OnDrawGizmos()
    {
        bool hasWaypoints = waypoints != null && waypoints.Length > 0;

        if (!hasWaypoints)
        {
            // Musuh tanpa waypoint mematikan dirinya sendiri di Start (diam total). Tanpa tanda ini
            // kelihatannya cuma "musuh diam" dan baru ketahuan setelah Play Mode.
            if (enemyType != EnemyType.Stationary)
            {
                DrawGizmoMissingWaypoint();
            }

            return;
        }

        if (showGizmoDirection || showGizmoLabels)
        {
            DrawGizmoRoute();
        }

        DrawGizmoLiveDebug();
    }

    /// <summary>Tanda merah untuk musuh yang belum diisi waypoint sama sekali.</summary>
    void DrawGizmoMissingWaypoint()
    {
        Vector3 origin = transform.position;
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(origin, 0.6f);
        Gizmos.DrawLine(origin + Vector3.left * 0.45f, origin + Vector3.right * 0.45f);
        Gizmos.DrawLine(origin + Vector3.up * 0.45f, origin + Vector3.down * 0.45f);
        DrawGizmoLabel(origin + Vector3.up * 0.85f, $"{name}: belum ada waypoint ({enemyType})");
    }

    /// <summary>
    /// Jalur patroli di Scene view: urutan waypoint, panah arah, dan target yang sedang dituju.
    /// Saat Play, bug memakai waypoint yang sudah ditempel ke permukaan platform (snappedWaypoints)
    /// dan fly memakai cache posisi dunia (flyingWaypoints), jadi yang digambar adalah tujuan asli.
    /// </summary>
    void DrawGizmoRoute()
    {
        int count = waypoints.Length;
        Vector3[] points = new Vector3[count];
        bool[] valid = new bool[count];
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            valid[i] = waypoints[i] != null;
            if (!valid[i])
            {
                continue;
            }

            points[i] = GetGizmoWaypointPosition(i);
            validCount++;
        }

        if (validCount == 0)
        {
            DrawGizmoMissingWaypoint();
            return;
        }

        Color routeColor = enemyType == EnemyType.SurfaceAttached
            ? new Color(1f, 0.62f, 0.15f)
            : new Color(0.2f, 0.9f, 1f);

        if (showGizmoDirection)
        {
            Gizmos.color = routeColor;

            if (patrolMode == PatrolMode.Random)
            {
                // Acak: semua pasangan bisa dituju, jadi digambar pudar dua arah.
                Color faint = routeColor;
                faint.a = 0.45f;
                Gizmos.color = faint;
                for (int i = 0; i < count; i++)
                {
                    if (!valid[i])
                    {
                        continue;
                    }

                    for (int j = i + 1; j < count; j++)
                    {
                        if (valid[j])
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
                    if (!valid[i] || !valid[i + 1])
                    {
                        continue;
                    }

                    // PingPong: panah di kedua ujung segmen karena arahnya bolak-balik.
                    DrawGizmoArrow(points[i], points[i + 1], patrolMode == PatrolMode.PingPong);
                }

                if (patrolMode == PatrolMode.Loop && count > 2 && valid[count - 1] && valid[0])
                {
                    DrawGizmoArrow(points[count - 1], points[0], false);
                }
            }
        }

        int activeIndex = Application.isPlaying ? waypointIndex : -1;

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
                // Waypoint pertama selalu jadi titik mulai (patroli dimulai dari sini).
                Gizmos.DrawSphere(points[i], 0.08f);
            }
        }

        if (activeIndex >= 0 && activeIndex < count && valid[activeIndex])
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, points[activeIndex]);
        }

        if (!showGizmoLabels)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!valid[i])
            {
                DrawGizmoLabel(points[i] + Vector3.up * 0.3f, $"W{i + 1} kosong");
                continue;
            }

            string label = $"W{i + 1}";
            string objectName = waypoints[i].name;
            if (!string.IsNullOrEmpty(objectName) && objectName != label)
            {
                label += $" / {objectName}";
            }

            DrawGizmoLabel(points[i] + Vector3.up * 0.32f, label);
        }

        if (patrolMode == PatrolMode.Once && valid[count - 1])
        {
            DrawGizmoLabel(points[count - 1] + Vector3.up * 0.75f, "Tujuan terakhir: musuh mati");
        }

        DrawGizmoLabel(transform.position + Vector3.up * 0.9f, BuildGizmoSummary(validCount));
    }

    /// <summary>Ringkasan tipe, pola, dan status musuh yang ditempel di atas objeknya.</summary>
    string BuildGizmoSummary(int validCount)
    {
        string summary = $"{name} / {enemyType} / {patrolMode} / {validCount} dari {waypoints.Length} waypoint";

        if (enemyType == EnemyType.SurfaceAttached)
        {
            if (Application.isPlaying)
            {
                summary += $"\n{(isTransitioningSurface ? "Pindah sisi platform" : "Berjalan di sisi platform")}";
                summary += $", arah {(travelSign > 0 ? "searah jarum jam" : "berlawanan jarum jam")}";
            }
            else
            {
                summary += snapWaypointsToSurface
                    ? "\nWaypoint ditempel ke tepi platform saat Play"
                    : "\nWaypoint dipakai apa adanya (snap mati)";
            }
        }
        else
        {
            summary += Application.isPlaying
                ? $"\nTerbang ke W{waypointIndex + 1} / {debugState}"
                : $"\nTerbang lurus antar waypoint, berhenti {obstacleStopDistance:F2} unit sebelum penghalang";
        }

        return summary;
    }

    /// <summary>Posisi waypoint yang sedang dipakai musuh (cache saat Play, posisi asli di luar Play).</summary>
    Vector3 GetGizmoWaypointPosition(int index)
    {
        if (enemyType == EnemyType.Flying && Application.isPlaying &&
            flyingWaypoints != null && index < flyingWaypoints.Length)
        {
            return flyingWaypoints[index];
        }

        if (enemyType == EnemyType.SurfaceAttached && Application.isPlaying &&
            snappedWaypoints != null && index < snappedWaypoints.Length)
        {
            return snappedWaypoints[index];
        }

        return waypoints[index] != null ? waypoints[index].position : transform.position;
    }

    static void DrawGizmoArrow(Vector3 from, Vector3 to, bool bothDirections)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.001f)
        {
            return;
        }

        Vector3 direction = delta / length;
        float headSize = Mathf.Clamp(length * 0.18f, 0.12f, 0.6f);

        Gizmos.DrawLine(from, to);
        DrawGizmoArrowHead(to, direction, headSize);
        if (bothDirections)
        {
            DrawGizmoArrowHead(from, -direction, headSize);
        }
    }

    static void DrawGizmoArrowHead(Vector3 tip, Vector3 direction, float headSize)
    {
        Vector3 side = Vector3.Cross(direction, Vector3.forward) * headSize * 0.5f;
        Vector3 back = tip - direction * headSize;
        Gizmos.DrawLine(tip, back + side);
        Gizmos.DrawLine(tip, back - side);
    }

    /// <summary>Garis bantu tambahan saat Play Mode kalau Debug Mode aktif.</summary>
    void DrawGizmoLiveDebug()
    {
        if (!debugMode || !Application.isPlaying || rb == null)
        {
            return;
        }

        Vector3 position = rb.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(position, position + (Vector3)(currentSurfaceNormal * 1.5f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, position + (Vector3)(debugMoveDirection * 1.5f));
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(debugProbePoint, 0.08f);
    }

#if UNITY_EDITOR
    static void DrawGizmoLabel(Vector3 position, string text)
    {
        Handles.Label(position, text);
    }
#else
    // Tidak ada Handles di build; label gizmo hanya untuk Scene view di editor.
    static void DrawGizmoLabel(Vector3 position, string text)
    {
    }
#endif

}