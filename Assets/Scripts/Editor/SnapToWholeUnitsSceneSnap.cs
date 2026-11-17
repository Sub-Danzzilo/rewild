using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Membulatkan posisi/rotasi/skala objek ber-<see cref="SnapToWholeUnits"/> selama objek itu
/// digeser, diputar, atau diskalakan dengan gizmo di Scene view.
///
/// Aturannya:
/// - Hanya transform yang <b>baru berubah</b> saat drag di Scene view yang dibulatkan.
/// - Nilai yang diketik manual di Inspector (termasuk desimal) tidak disentuh.
/// - Memasang komponen tidak mengubah nilai apa pun; perubahan pertama hanya dicatat.
/// </summary>
[InitializeOnLoad]
static class SnapToWholeUnitsSceneSnap
{
    // Drag dianggap masih berjalan selama masih ada event mouse di Scene view. Jendela waktu ini
    // juga menangkap perubahan terakhir tepat setelah tombol mouse dilepas.
    const double DragWindow = 0.4;

    static readonly Dictionary<Transform, TransformState> KnownStates =
        new Dictionary<Transform, TransformState>();

    static double lastSceneDragTime;

    static SnapToWholeUnitsSceneSnap()
    {
        SceneView.duringSceneGui += OnSceneGui;
        EditorApplication.update += OnEditorUpdate;
        AssemblyReloadEvents.afterAssemblyReload += ResetCache;
    }

    static void ResetCache()
    {
        KnownStates.Clear();
        lastSceneDragTime = 0.0;
    }

    static void OnSceneGui(SceneView sceneView)
    {
        Event current = Event.current;
        if (current == null || current.button != 0)
        {
            return;
        }

        if (current.type == EventType.MouseDown ||
            current.type == EventType.MouseDrag ||
            current.type == EventType.MouseUp)
        {
            lastSceneDragTime = EditorApplication.timeSinceStartup;
        }
    }

    static bool IsDraggingInSceneView()
    {
        return lastSceneDragTime > 0.0 &&
               EditorApplication.timeSinceStartup - lastSceneDragTime <= DragWindow;
    }

    static void OnEditorUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SnapToWholeUnits[] targets = Object.FindObjectsByType<SnapToWholeUnits>(FindObjectsSortMode.None);
        if (targets.Length == 0)
        {
            return;
        }

        bool dragging = IsDraggingInSceneView();

        for (int i = 0; i < targets.Length; i++)
        {
            SnapToWholeUnits target = targets[i];
            if (target == null)
            {
                continue;
            }

            Transform targetTransform = target.transform;
            TransformState currentState = TransformState.From(targetTransform);

            // Transform yang belum pernah terlihat (mis. komponen baru dipasang) hanya dicatat,
            // jadi nilai yang sudah ada tidak ikut berubah.
            if (!KnownStates.TryGetValue(targetTransform, out TransformState knownState) ||
                !knownState.Matches(currentState))
            {
                if (dragging && target.isActiveAndEnabled && target.enabled)
                {
                    SnapTransform(target);
                }

                KnownStates[targetTransform] = TransformState.From(targetTransform);
            }
        }
    }

    static void SnapTransform(SnapToWholeUnits target)
    {
        Transform targetTransform = target.transform;

        if (target.snapPosition && target.positionStep > 0f)
        {
            Vector3 position = targetTransform.localPosition;
            Vector3 snappedPosition = SnapVector(position, target.positionStep);
            if ((snappedPosition - position).sqrMagnitude > 0.0000001f)
            {
                targetTransform.localPosition = snappedPosition;
            }
        }

        if (target.snapRotation && target.rotationStep > 0f)
        {
            Vector3 euler = targetTransform.localEulerAngles;
            Vector3 snappedEuler = SnapVector(euler, target.rotationStep);
            // Sudut dibandingkan dengan DeltaAngle supaya 360 dan 0 tidak memicu penulisan terus.
            if (AngleDistance(euler.x, snappedEuler.x) > 0.0001f ||
                AngleDistance(euler.y, snappedEuler.y) > 0.0001f ||
                AngleDistance(euler.z, snappedEuler.z) > 0.0001f)
            {
                targetTransform.localEulerAngles = snappedEuler;
            }
        }

        if (target.snapScale && target.scaleStep > 0f)
        {
            Vector3 scale = targetTransform.localScale;
            Vector3 snappedScale = SnapScale(scale, target.scaleStep);
            if ((snappedScale - scale).sqrMagnitude > 0.0000001f)
            {
                targetTransform.localScale = snappedScale;
            }
        }
    }

    static Vector3 SnapVector(Vector3 value, float step)
    {
        return new Vector3(
            Mathf.Round(value.x / step) * step,
            Mathf.Round(value.y / step) * step,
            Mathf.Round(value.z / step) * step
        );
    }

    static Vector3 SnapScale(Vector3 scale, float step)
    {
        Vector3 snapped = SnapVector(scale, step);

        // Skala tidak boleh jadi nol hanya karena pembulatan; kembalikan ke satu langkah terkecil.
        if (Mathf.Approximately(snapped.x, 0f) && !Mathf.Approximately(scale.x, 0f))
        {
            snapped.x = Mathf.Sign(scale.x) * step;
        }

        if (Mathf.Approximately(snapped.y, 0f) && !Mathf.Approximately(scale.y, 0f))
        {
            snapped.y = Mathf.Sign(scale.y) * step;
        }

        if (Mathf.Approximately(snapped.z, 0f) && !Mathf.Approximately(scale.z, 0f))
        {
            snapped.z = Mathf.Sign(scale.z) * step;
        }

        return snapped;
    }

    static float AngleDistance(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b));
    }

    /// <summary>Salinan nilai transform untuk mendeteksi perubahan tanpa memicu perubahan itu sendiri.</summary>
    struct TransformState
    {
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        public static TransformState From(Transform source)
        {
            return new TransformState
            {
                position = source.localPosition,
                rotation = source.localRotation,
                scale = source.localScale
            };
        }

        public bool Matches(TransformState other)
        {
            return (position - other.position).sqrMagnitude <= 0.0000001f &&
                   Quaternion.Angle(rotation, other.rotation) <= 0.001f &&
                   (scale - other.scale).sqrMagnitude <= 0.0000001f;
        }
    }
}
