#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PortalController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Portal")]
    [Tooltip("Pasangkan dengan portal tujuan, lalu tautkan kembali dari portal tujuan.")]
    public PortalController linkedPortal;
    [Tooltip("Collider pemicu yang dipaskan ke sprite portal saat runtime.")]
    public BoxCollider2D portalCollider;
    [Tooltip("Arah referensi portal dalam koordinat lokal. Sisi depan dan belakang sama-sama bisa dilewati. Contoh: BluePortal (-1, 0) menghadap kiri.")]
    public Vector2 localFrontDirection = Vector2.left;
    [Tooltip("Arah keluar untuk lintasan yang masuk dari sisi depan. Lintasan dari belakang keluar ke arah sebaliknya. Rotasi portal ikut memutar arah ini.")]
    public Vector2 localExitDirection = Vector2.up;
    [Tooltip("Jarak untuk mengeluarkan bidikan dan Webi dari portal tujuan.")]
    public float exitOffset = 0.05f;
    [Tooltip("Jarak maksimum mencari platform aman dari posisi keluar.")]
    public float exitSearchDistance = 50f;
    [Tooltip("SpriteRenderer portal. Jika kosong, diambil otomatis dari objek ini.")]
    public SpriteRenderer portalRenderer;

    [Header("Collider")]

    [Tooltip("Bagian sprite yang dipakai sebagai collider, sebagai fraksi ukuran sprite. " +
             "Dipakai untuk memotong bingkai dan sisa ruang kosong di sekitar portal.")]
    public Vector2 colliderSizeFraction = new Vector2(0.33f, 0.6f);

    [Tooltip("Geseran collider dari tengah sprite, sebagai fraksi ukuran sprite. 0 = tepat di tengah.")]
    public Vector2 colliderOffsetFraction = Vector2.zero;

    [Header("Camera")]

    [Tooltip("Kamera level yang difokuskan sebentar ke Webi setelah teleport. Kalau kosong, dicari otomatis di scene.")]
    public CameraDragController cameraDragController;

    [Header("Audio")]

    [Tooltip("SFX saat Webi berhasil melewati portal (diputar di portal tujuan).")]
    public AudioClip teleportSfx;

    [Tooltip("Volume SFX portal (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float teleportSfxVolume = 0.8f;

    [Header("Gizmos (editor)")]

    [Tooltip("Tampilkan kotak collider dan panah arah portal di Scene view.")]
    public bool showDirectionGizmos = true;

    [Tooltip("Tampilkan teks keterangan arah saat portal dipilih.")]
    public bool showDirectionLabels = true;

    #endregion

    void OnValidate()
    {
        if (portalCollider == null)
        {
            portalCollider = GetComponent<BoxCollider2D>();
        }

        if (portalRenderer == null)
        {
            portalRenderer = GetComponent<SpriteRenderer>();
        }

        FitColliderToPortal();
    }

    void Awake()
    {
        if (portalCollider == null)
        {
            portalCollider = GetComponent<BoxCollider2D>();
        }

        if (portalCollider == null)
        {
            Debug.LogError($"Portal {name} needs a BoxCollider2D.", this);
            enabled = false;
            return;
        }

        if (portalRenderer == null)
        {
            portalRenderer = GetComponent<SpriteRenderer>();
        }

        portalCollider.isTrigger = true;
        FitColliderToPortal();
    }

    void FitColliderToPortal()
    {
        if (portalCollider == null || portalRenderer == null || portalRenderer.sprite == null)
        {
            return;
        }

        // localBounds dipakai (bukan bounds dunia), jadi nilai ini tetap benar walau portal
        // diputar, digeser, atau diskalakan. Collider ada di ruang lokal, jadi skalanya otomatis
        // mengikuti transform seperti sprite-nya.
        Bounds spriteBounds = portalRenderer.localBounds;
        Vector2 sizeFraction = new Vector2(
            Mathf.Abs(colliderSizeFraction.x),
            Mathf.Abs(colliderSizeFraction.y)
        );

        portalCollider.size = new Vector2(
            spriteBounds.size.x * sizeFraction.x,
            spriteBounds.size.y * sizeFraction.y
        );
        portalCollider.offset = (Vector2)spriteBounds.center + new Vector2(
            spriteBounds.size.x * colliderOffsetFraction.x,
            spriteBounds.size.y * colliderOffsetFraction.y
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (!IsLinked)
        {
            Debug.LogError($"Portal {name} needs a valid linked portal.", this);
            return;
        }

        Vector2 portalCenter = portalCollider.bounds.center;
        Vector2 mappedPosition = TransformPointToLinkedPortal(player.transform.position);
        Vector2 playerSide = (Vector2)player.transform.position - portalCenter;
        Vector2 entryDirection = Vector2.Dot(playerSide, GetFrontDirection()) >= 0f
            ? -GetFrontDirection()
            : GetFrontDirection();
        Vector2 exitDirection = TransformDirectionToLinkedPortal(entryDirection);
        bool teleported = player.TryTeleportThroughPortal(
            this,
            mappedPosition,
            exitDirection,
            linkedPortal.exitSearchDistance
        );

        if (teleported)
        {
            if (teleportSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(teleportSfx, 1f, 1f, teleportSfxVolume);
            }

            FocusCameraOnPlayer(player);
        }
    }

    void FocusCameraOnPlayer(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        if (cameraDragController == null)
        {
            cameraDragController = FindFirstObjectByType<CameraDragController>();
        }

        if (cameraDragController == null)
        {
            return;
        }

        // Portal memindahkan Webi lewat Rigidbody2D.position. Transform-nya baru ikut setelah physics
        // sync, jadi membaca player.transform.position di sini masih posisi LAMA (di pintu masuk)
        // dan kamera terlihat tidak pindah sama sekali. Rigidbody2D.position sudah berisi posisi baru.
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        Vector2 focusPoint = playerBody != null
            ? playerBody.position
            : (Vector2)player.transform.position;

        cameraDragController.PlayTeleportFocus(focusPoint, 0f, player.transform);
    }

    public bool TryGetExit(
        Vector2 entryPoint,
        Vector2 incomingDirection,
        out Vector2 exitPoint,
        out Vector2 exitDirection
    )
    {
        exitPoint = default;
        exitDirection = default;
        if (!IsLinked)
        {
            return false;
        }

        exitDirection = TransformDirectionToLinkedPortal(incomingDirection.normalized);
        exitPoint = TransformPointToLinkedPortal(entryPoint) +
            exitDirection * Mathf.Max(linkedPortal.exitOffset, 0f);
        return true;
    }

    public PortalController LinkedPortal => linkedPortal;

    public Vector2 TransformPointToLinkedPortal(Vector2 point)
    {
        Vector2 sourceCenter = portalCollider != null ? portalCollider.bounds.center : transform.position;
        Vector2 targetCenter = linkedPortal.portalCollider != null
            ? linkedPortal.portalCollider.bounds.center
            : linkedPortal.transform.position;
        float pointRotation = GetRotationOffset();
        return targetCenter + RotateDirection(point - sourceCenter, pointRotation);
    }

    public bool IsReady => IsLinked;

    public Vector2 TransformDirectionToLinkedPortal(Vector2 direction)
    {
        return linkedPortal != null
            ? RotateDirection(direction, GetRotationOffset())
            : direction;
    }

    public float TransformRotationToLinkedPortal(float rotation)
    {
        return rotation + GetRotationOffset();
    }

    public Vector2 GetFrontDirection()
    {
        Vector2 direction = localFrontDirection.sqrMagnitude > 0.0001f
            ? localFrontDirection.normalized
            : Vector2.left;
        return transform.TransformDirection(direction).normalized;
    }

    float GetRotationOffset()
    {
        Vector2 sourceInwardDirection = -GetFrontDirection();
        return Vector2.SignedAngle(sourceInwardDirection, linkedPortal.GetExitDirection());
    }

    public Vector2 GetExitDirection()
    {
        Vector2 direction = localExitDirection.sqrMagnitude > 0.0001f
            ? localExitDirection.normalized
            : Vector2.up;
        return transform.TransformDirection(direction).normalized;
    }

    static Vector2 RotateDirection(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0f, 0f, angle) * direction;
    }

    bool IsLinked =>
        linkedPortal != null &&
        linkedPortal != this &&
        linkedPortal.isActiveAndEnabled &&
        linkedPortal.portalCollider != null;

    #region Gizmos

#if UNITY_EDITOR
    static readonly Color FrontColor = new Color(0.25f, 0.85f, 1f, 0.95f);
    static readonly Color BackColor = new Color(0.65f, 0.65f, 0.65f, 0.9f);
    static readonly Color ExitColor = new Color(0.45f, 1f, 0.35f, 0.95f);
    static readonly Color ColliderColor = new Color(1f, 0.62f, 0.15f, 0.9f);
    static readonly Color LinkColor = new Color(1f, 1f, 1f, 0.35f);

    void OnDrawGizmos()
    {
        DrawPortalGizmos(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawPortalGizmos(true);
    }

    /// <summary>
    /// Pratinjau arah portal di Scene view (tanpa Play mode).
    /// Biru = sisi depan (lintasan yang masuk dari sini), abu-abu = sisi belakang,
    /// hijau = arah Webi keluar dari portal ini kalau datang dari depan portal pasangannya.
    /// </summary>
    void DrawPortalGizmos(bool detailed)
    {
        if (portalCollider == null)
        {
            portalCollider = GetComponent<BoxCollider2D>();
        }

        if (portalCollider == null || !showDirectionGizmos)
        {
            return;
        }

        Transform ct = portalCollider.transform;
        Vector2 colliderHalfSize = portalCollider.size * 0.5f;
        Vector2 colliderOffset = portalCollider.offset;
        Vector3 center = ct.TransformPoint(colliderOffset);
        float lossyScale = Mathf.Max(
            Mathf.Abs(ct.lossyScale.x),
            Mathf.Abs(ct.lossyScale.y),
            0.0001f
        );

        Gizmos.color = ColliderColor;
        DrawColliderBox(ct, colliderOffset, colliderHalfSize);

        Vector2 frontDirection = GetFrontDirection();
        Vector2 exitDirection = GetExitDirection();
        float halfThickness = DistanceToBoxEdge(frontDirection, ct, colliderHalfSize) * lossyScale;

        // Sisi depan: keluar dari badan portal, jadi kelihatan sisi mana yang "pintu masuk".
        Gizmos.color = FrontColor;
        Vector2 frontStart = (Vector2)center + frontDirection * (halfThickness + 0.05f);
        DrawArrow(frontStart, frontDirection, 0.9f, 0.28f);

        Gizmos.color = BackColor;
        Vector2 backStart = (Vector2)center - frontDirection * (halfThickness + 0.05f);
        DrawArrow(backStart, -frontDirection, 0.55f, 0.2f);

        // Arah keluar portal ini (dipakai lintasan yang datang dari portal pasangan).
        Gizmos.color = ExitColor;
        float exitEdge = DistanceToBoxEdge(exitDirection, ct, colliderHalfSize) * lossyScale;
        Vector2 exitStart = (Vector2)center + exitDirection * (exitEdge + 0.05f);
        DrawArrow(exitStart, exitDirection, 1.5f, 0.4f);

        if (detailed)
        {
            if (showDirectionLabels)
            {
                Handles.Label(frontStart + frontDirection * 1.05f, "DEPAN  (masuk)");
                Handles.Label(backStart - frontDirection * 0.75f, "BELAKANG");
                Handles.Label(exitStart + exitDirection * 1.7f, "KELUAR  (dari depan pasangan)");
            }

            // Lintasan yang masuk dari belakang keluar ke arah sebaliknya (ditandai lebih redup).
            Gizmos.color = new Color(ExitColor.r, ExitColor.g, ExitColor.b, 0.4f);
            Vector2 reverseExitStart = (Vector2)center - exitDirection * (exitEdge + 0.05f);
            DrawArrow(reverseExitStart, -exitDirection, 0.9f, 0.28f);

            if (showDirectionLabels)
            {
                Handles.Label(
                    reverseExitStart - exitDirection * 1.05f,
                    "keluar (dari belakang pasangan)"
                );
            }
        }

        if (linkedPortal == null || linkedPortal == this)
        {
            if (detailed && showDirectionLabels)
            {
                Handles.Label(center + Vector3.up * 0.35f, "Linked Portal kosong!");
            }

            return;
        }

        Gizmos.color = LinkColor;
        Gizmos.DrawLine(center, linkedPortal.transform.position);
    }

    static void DrawColliderBox(Transform colliderTransform, Vector2 offset, Vector2 halfSize)
    {
        Vector3 bottomLeft = colliderTransform.TransformPoint(offset + new Vector2(-halfSize.x, -halfSize.y));
        Vector3 bottomRight = colliderTransform.TransformPoint(offset + new Vector2(halfSize.x, -halfSize.y));
        Vector3 topRight = colliderTransform.TransformPoint(offset + new Vector2(halfSize.x, halfSize.y));
        Vector3 topLeft = colliderTransform.TransformPoint(offset + new Vector2(-halfSize.x, halfSize.y));

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    static void DrawArrow(Vector2 from, Vector2 direction, float length, float headLength)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        Vector2 tip = from + dir * length;

        Gizmos.DrawLine(from, tip);
        Gizmos.DrawLine(tip, tip + RotateDirection(-dir, 150f) * headLength);
        Gizmos.DrawLine(tip, tip + RotateDirection(-dir, -150f) * headLength);
    }

    static float DistanceToBoxEdge(Vector2 worldDirection, Transform colliderTransform, Vector2 halfSize)
    {
        Vector2 localDirection = colliderTransform.InverseTransformDirection(worldDirection).normalized;
        float distanceX = Mathf.Abs(localDirection.x) > 0.0001f
            ? halfSize.x / Mathf.Abs(localDirection.x)
            : float.PositiveInfinity;
        float distanceY = Mathf.Abs(localDirection.y) > 0.0001f
            ? halfSize.y / Mathf.Abs(localDirection.y)
            : float.PositiveInfinity;
        return Mathf.Min(distanceX, distanceY);
    }
#endif

    #endregion
}
