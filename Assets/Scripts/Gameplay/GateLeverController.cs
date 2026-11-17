using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(BoxCollider2D))]
public class GateLeverController : MonoBehaviour
{
    public enum GateOpenDirection
    {
        Up,
        Down,
        Right,
        Left
    }

    #region Inspector Fields

    [Header("References")]
    [Tooltip("Transform pintu gerbang yang akan digeser saat tuas ditarik.")]
    public Transform gate;

    [Header("Gate")]
    [Tooltip("Arah gerbang terbuka: naik, turun, ke kanan, atau ke kiri. Arah ini mengikuti rotasi gate.")]
    public GateOpenDirection openDirection = GateOpenDirection.Up;

    [Tooltip("Collider penghalang pada pintu. Jika kosong, dicari otomatis dari objek gate.")]
    public BoxCollider2D gateCollider;

    [Tooltip("Jarak gerak gate saat dibuka, dalam unit dunia. Isi 0 untuk memakai ukuran gate sendiri " +
             "plus 0,5 supaya seluruh sprite hilang di balik batasnya.")]
    public float gateTravelDistance;

    [Tooltip("Sorting order sprite gate supaya gerbang tampil di belakang atau depan objek lain.")]
    public int gateSortingOrder = -1;

    [Header("Animation")]
    [Tooltip("Lama animasi gerbang bergeser masuk ke balik batasnya, dalam detik. 0 = langsung hilang.")]
    public float animationDuration = 0.5f;

    [Tooltip("SpriteRenderer tuas. Jika kosong, dicari otomatis di anak objek ini.")]
    public SpriteRenderer leverRenderer;

    [Tooltip("Animation Clip gerakan tuas saat ditarik.")]
    public AnimationClip leverAnimation;

    [Header("Audio")]

    [Tooltip("SFX saat tuas ditarik: gerbang mulai terbuka.")]
    public AudioClip gateOpenSfx;

    [Tooltip("Volume SFX tuas dan gerbang (0 = senyap, 1 = penuh).")]
    [Range(0f, 1f)]
    public float gateOpenSfxVolume = 0.8f;

    #endregion

    private bool isOpening;
    private Vector3 gateStartPosition;
    private float resolvedGateTravelDistance;
    private SpriteRenderer gateRenderer;
    private SpriteMask gateMask;
    private Texture2D gateMaskTexture;
    private Sprite gateMaskSprite;
    private Animator leverAnimator;
    private PlayableGraph leverGraph;
    private AnimationClipPlayable leverClipPlayable;

    void OnValidate()
    {
        ConfigureLeverTrigger();
    }

    void Awake()
    {
        ConfigureLeverTrigger();

        if (gate == null)
        {
            Debug.LogError($"{nameof(GateLeverController)} on {name} needs a Gate reference.", this);
            enabled = false;
            return;
        }

        if (gateCollider == null)
        {
            gateCollider = gate.GetComponent<BoxCollider2D>();
        }

        if (gateCollider == null)
        {
            gateRenderer = gate.GetComponentInChildren<SpriteRenderer>();
            if (gateRenderer == null || gateRenderer.sprite == null)
            {
                Debug.LogError($"Gate {gate.name} needs a SpriteRenderer to create its blocking collider.", gate);
                enabled = false;
                return;
            }

            gateCollider = gateRenderer.gameObject.AddComponent<BoxCollider2D>();
            gateCollider.size = gateRenderer.localBounds.size;
            gateCollider.offset = gateRenderer.localBounds.center;
        }

        gateCollider.isTrigger = false;
        if (gateRenderer == null)
        {
            gateRenderer = gate.GetComponentInChildren<SpriteRenderer>();
        }

        if (gateRenderer != null)
        {
            gateRenderer.sortingOrder = gateSortingOrder;
        }

        if (leverRenderer == null)
        {
            leverRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (leverRenderer == null)
        {
            Debug.LogError($"Lever {name} needs a SpriteRenderer to animate.", this);
        }
        else
        {
            leverAnimator = GetComponent<Animator>();
            if (leverAnimator == null)
            {
                Debug.LogError($"Lever {name} needs an Animator to play its animation clip.", this);
            }
            else if (leverAnimation == null)
            {
                Debug.LogError($"Assign the lever animation clip on {name}.", this);
            }
            else
            {
                InitializeLeverAnimation();
            }
        }

        gateStartPosition = gate.position;
        resolvedGateTravelDistance = gateTravelDistance > 0f
            ? gateTravelDistance
            : GetGateSizeAlongOpeningAxis(gate) + 0.5f;
        ConfigureGateMask();
    }

    /// <summary>
    /// Membuat mask yang menutupi seluruh gate beserta jalur gesernya, dengan ujung depan tepat di
    /// sisi depan gate saat masih tertutup. Garis itu tetap di tempatnya di dunia, jadi ketika gate
    /// bergeser ke arah buka, bagian yang melewatinya otomatis tidak terlihat.
    /// </summary>
    void ConfigureGateMask()
    {
        if (gateRenderer == null)
        {
            Debug.LogError($"Gate {gate.name} needs a SpriteRenderer for gate clipping.", gate);
            return;
        }

        // Semua ukuran dihitung pada sumbu gate sendiri (arah buka gate), lalu diputar mengikuti
        // rotasi gate. Dengan begitu Up/Down/Right/Left tetap presisi di gate yang dimiringkan.
        Vector2 maskDirection = GetOpeningDirection();
        float gateHalfAlongAxis = GetGateSizeAlongOpeningAxis(gate) * 0.5f;
        float maskCrossSize = GetGateCrossSize() + 1f;

        float maskAxisStart = -gateHalfAlongAxis - resolvedGateTravelDistance - 1f;
        float maskAxisEnd = gateHalfAlongAxis;
        float maskAxisSize = Mathf.Max(maskAxisEnd - maskAxisStart, 0.01f);
        float axisCenter = maskAxisStart + maskAxisSize * 0.5f;
        Vector2 maskCenter = (Vector2)gate.position + maskDirection * axisCenter;

        GameObject maskObject = new GameObject($"{name} Gate Clip Mask");
        maskObject.transform.position = new Vector3(maskCenter.x, maskCenter.y, gate.position.z);
        maskObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, maskDirection);
        maskObject.transform.localScale = new Vector3(maskCrossSize, maskAxisSize, 1f);

        gateMask = maskObject.AddComponent<SpriteMask>();
        gateMask.sprite = CreateGateMaskSprite();
        gateMask.isCustomRangeActive = true;
        gateMask.backSortingLayerID = gateRenderer.sortingLayerID;
        gateMask.frontSortingLayerID = gateRenderer.sortingLayerID;
        gateMask.backSortingOrder = -32768;
        gateMask.frontSortingOrder = 32767;
        gateRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    Sprite CreateGateMaskSprite()
    {
        gateMaskTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = $"{name} Gate Clip Mask Texture",
            hideFlags = HideFlags.HideAndDontSave
        };
        gateMaskTexture.SetPixel(0, 0, Color.white);
        gateMaskTexture.Apply();

        gateMaskSprite = Sprite.Create(
            gateMaskTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
        gateMaskSprite.name = $"{name} Gate Clip Mask Sprite";
        gateMaskSprite.hideFlags = HideFlags.HideAndDontSave;
        return gateMaskSprite;
    }

    void InitializeLeverAnimation()
    {
        leverAnimator.runtimeAnimatorController = null;
        leverGraph = PlayableGraph.Create($"{name} Lever Animation");
        leverGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        leverClipPlayable = AnimationClipPlayable.Create(leverGraph, leverAnimation);
        leverClipPlayable.SetApplyFootIK(false);
        leverClipPlayable.SetDuration(leverAnimation.length);
        leverClipPlayable.SetSpeed(0d);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            leverGraph,
            "Lever Animation",
            leverAnimator
        );
        output.SetSourcePlayable(leverClipPlayable);
        leverGraph.Play();
        leverGraph.Evaluate(0f);
    }

    void ConfigureLeverTrigger()
    {
        BoxCollider2D leverTrigger = GetComponent<BoxCollider2D>();
        SpriteRenderer rendererToUse = leverRenderer != null
            ? leverRenderer
            : GetComponentInChildren<SpriteRenderer>();
        if (leverTrigger == null || rendererToUse == null || rendererToUse.sprite == null)
        {
            return;
        }

        Bounds spriteBounds = rendererToUse.localBounds;
        leverTrigger.isTrigger = true;
        leverTrigger.offset = spriteBounds.center;
        leverTrigger.size = spriteBounds.size;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpening || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        isOpening = true;
        if (gateOpenSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(gateOpenSfx, 1f, 1f, gateOpenSfxVolume);
        }

        StartCoroutine(OpenGate());
        StartCoroutine(AnimateLever());
    }

    IEnumerator OpenGate()
    {
        Vector3 gateDirection = GetOpeningDirection();
        Vector3 gateTargetPosition = gateStartPosition + gateDirection * resolvedGateTravelDistance;
        float duration = Mathf.Max(animationDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            // Gate benar-benar bergeser; bagian yang melewati ujung mask (batas depan gate saat
            // tertutup) otomatis terpotong, jadi terlihat seperti menghilang ke balik batas itu.
            gate.position = Vector3.Lerp(gateStartPosition, gateTargetPosition, easedProgress);
            yield return null;
        }

        gate.position = gateTargetPosition;

        if (gateCollider != null)
        {
            gateCollider.enabled = false;
        }
    }

    IEnumerator AnimateLever()
    {
        if (!leverGraph.IsValid() || !leverClipPlayable.IsValid())
        {
            yield break;
        }

        float duration = leverAnimation.length;
        if (duration <= 0f)
        {
            Debug.LogError($"Lever animation clip on {name} has no duration.", this);
            yield break;
        }

        leverClipPlayable.SetTime(0d);
        leverClipPlayable.SetSpeed(1d);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, duration - elapsed);
            leverGraph.Evaluate(deltaTime);
            elapsed += deltaTime;
            yield return null;
        }

        leverClipPlayable.SetTime(duration);
        leverGraph.Evaluate(0f);
    }

    void OnDestroy()
    {
        if (leverGraph.IsValid())
        {
            leverGraph.Destroy();
        }

        if (gateMask != null)
        {
            Destroy(gateMask.gameObject);
        }

        if (gateMaskSprite != null)
        {
            Destroy(gateMaskSprite);
        }

        if (gateMaskTexture != null)
        {
            Destroy(gateMaskTexture);
        }
    }

    /// <summary>Arah buka pada ruang lokal gate (Up = sumbu Y gate, bukan selalu atas dunia).</summary>
    Vector2 GetLocalOpeningDirection()
    {
        switch (openDirection)
        {
            case GateOpenDirection.Up:
                return Vector2.up;
            case GateOpenDirection.Down:
                return Vector2.down;
            case GateOpenDirection.Right:
                return Vector2.right;
            case GateOpenDirection.Left:
                return Vector2.left;
            default:
                return Vector2.up;
        }
    }

    /// <summary>
    /// Arah buka di dunia. Diputar mengikuti rotasi gate supaya gate yang dimiringkan atau
    /// diputar 90 derajat tetap bergeser ke arah lokalnya sendiri, bukan selalu ke atas dunia.
    /// </summary>
    Vector2 GetOpeningDirection()
    {
        Vector2 localDirection = GetLocalOpeningDirection();
        return gate != null ? (Vector2)(gate.rotation * localDirection) : localDirection;
    }

    /// <summary>Ukuran kotak (lokal) pada satu sumbu, setelah dikalikan skala transform-nya.</summary>
    float GetSizeAlongAxis(Vector3 size, Vector3 scale, Vector2 axis)
    {
        return Mathf.Abs(axis.y) > 0f
            ? Mathf.Abs(size.y * scale.y)
            : Mathf.Abs(size.x * scale.x);
    }

    /// <summary>
    /// Panjang gate di dunia pada arah buka. Dihitung dari ukuran lokal sprite dikali skala,
    /// bukan dari bounds dunia, karena bounds dunia ikut membesar saat gate diputar.
    /// </summary>
    float GetGateSizeAlongOpeningAxis(Transform gateTransform)
    {
        SpriteRenderer renderer = gateTransform.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            return GetSizeAlongAxis(
                renderer.localBounds.size,
                renderer.transform.lossyScale,
                GetLocalOpeningDirection()
            );
        }

        return gateCollider != null
            ? GetSizeAlongAxis(gateCollider.size, gateCollider.transform.lossyScale, GetLocalOpeningDirection())
            : 0f;
    }

    /// <summary>Lebar gate di dunia pada arah tegak lurus arah buka.</summary>
    float GetGateCrossSize()
    {
        Vector2 localDirection = GetLocalOpeningDirection();
        Vector2 crossAxis = new Vector2(-localDirection.y, localDirection.x);
        SpriteRenderer renderer = gateRenderer != null
            ? gateRenderer
            : gate.GetComponentInChildren<SpriteRenderer>();

        if (renderer != null)
        {
            return GetSizeAlongAxis(renderer.localBounds.size, renderer.transform.lossyScale, crossAxis);
        }

        return gateCollider != null
            ? GetSizeAlongAxis(gateCollider.size, gateCollider.transform.lossyScale, crossAxis)
            : 0f;
    }
}
