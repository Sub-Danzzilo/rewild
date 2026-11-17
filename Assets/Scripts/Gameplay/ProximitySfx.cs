using UnityEngine;

/// <summary>
/// Memutar satu SFX secara berulang dan mengatur volumenya mengikuti jarak ke Webi:
/// makin dekat makin keras, makin jauh makin pelan, lalu berhenti saat di luar jangkauan.
/// Dipakai musuh yang suaranya hanya ingin terdengar saat pemain mendekat (mis. Fly).
/// Volume tetap mengikuti slider SFX dan tombol mute di Settings lewat <see cref="AudioManager"/>.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProximitySfx : MonoBehaviour
{
    #region Inspector Fields

    [Header("Clip")]
    [Tooltip("Suara yang diputar berulang selama Webi berada dalam jangkauan jarak.")]
    public AudioClip sfx;

    [Tooltip("Volume saat Webi berada di jarak terdekat (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float maxVolume = 0.8f;

    [Header("Distance")]
    [Tooltip("Jarak (unit) saat suara sudah terdengar penuh.")]
    public float nearDistance = 2.5f;

    [Tooltip("Jarak (unit) saat suara tidak terdengar lagi.")]
    public float farDistance = 12f;

    [Tooltip("Kecepatan volume menyusul jarak (per detik). 0 = volume langsung mengikuti jarak.")]
    public float volumeSmoothSpeed = 4f;

    [Tooltip("Objek acuan jarak. Kosongkan agar Webi dicari otomatis lewat tag Player.")]
    public Transform target;

    [Tooltip("Jika aktif, suara langsung berhenti ketika musuh mati.")]
    public bool stopWhenDead = true;

    #endregion

    #region Runtime State

    private AudioSource source;
    private EnemyController enemy;
    private Transform playerTransform;
    private float currentVolume;
    private float targetSearchTimer;

    #endregion

    #region Unity Events

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        // Suara diputar 2D dan volumenya dihitung dari jarak ke Webi, bukan dari posisi AudioListener.
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;

        enemy = GetComponent<EnemyController>();
    }

    void OnEnable()
    {
        currentVolume = 0f;

        if (source != null && sfx != null && !source.isPlaying)
        {
            source.clip = sfx;
            source.volume = 0f;
            source.Play();
        }
    }

    void OnDisable()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    void Update()
    {
        if (source == null)
        {
            return;
        }

        if (sfx == null)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }

            return;
        }

        if (source.clip != sfx)
        {
            source.clip = sfx;
        }

        float wantedVolume = 0f;
        Transform listener = ResolvePlayerTransform();
        bool isAlive = !stopWhenDead || enemy == null || !enemy.IsDead;
        if (listener != null && isAlive)
        {
            float distance = Vector2.Distance(transform.position, listener.position);
            wantedVolume = maxVolume * VolumeFactor(distance, nearDistance, farDistance);
        }

        currentVolume = volumeSmoothSpeed > 0f
            ? Mathf.MoveTowards(currentVolume, wantedVolume, volumeSmoothSpeed * Time.unscaledDeltaTime)
            : wantedVolume;

        source.volume = currentVolume * GetGlobalSfxVolume();

        if (currentVolume > 0.0001f)
        {
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    void OnValidate()
    {
        maxVolume = Mathf.Clamp01(maxVolume);
        nearDistance = Mathf.Max(0f, nearDistance);
        farDistance = Mathf.Max(nearDistance, farDistance);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Pengali volume (0..1) untuk jarak tertentu. Di dalam <paramref name="near"/> hasilnya 1,
    /// di luar <paramref name="far"/> hasilnya 0, dan di antaranya turun halus (smoothstep).
    /// </summary>
    public static float VolumeFactor(float distance, float near, float far)
    {
        if (far <= near)
        {
            return distance <= near ? 1f : 0f;
        }

        float t = Mathf.InverseLerp(near, far, distance);
        return 1f - t * t * (3f - 2f * t);
    }

    /// <summary>Mencari Webi lewat tag Player sekali per setengah detik, lalu menyimpannya.</summary>
    Transform ResolvePlayerTransform()
    {
        if (target != null)
        {
            return target;
        }

        if (playerTransform != null)
        {
            return playerTransform;
        }

        targetSearchTimer -= Time.unscaledDeltaTime;
        if (targetSearchTimer > 0f)
        {
            return null;
        }

        targetSearchTimer = 0.5f;
        GameObject player = GameObject.FindWithTag("Player");
        playerTransform = player != null ? player.transform : null;
        return playerTransform;
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

    #endregion
}
