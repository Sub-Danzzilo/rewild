using UnityEngine;

/// <summary>
/// Penanda dan setelan pembulatan untuk objek yang digeser lewat gizmo di Scene view.
/// Pembulatannya dikerjakan script editor <c>SnapToWholeUnitsSceneSnap</c> dan hanya terjadi
/// saat objek sedang di-drag dengan gizmo. Mengetik angka desimal di Inspector tetap bebas,
/// dan memasang script ini tidak mengubah nilai posisi/rotasi/skala yang sudah ada.
/// </summary>
[DisallowMultipleComponent]
public class SnapToWholeUnits : MonoBehaviour
{
    #region Inspector Fields

    [Header("Position")]

    [Tooltip("Bulatkan posisi lokal saat objek digeser dengan gizmo di Scene view.")]
    public bool snapPosition = true;

    [Tooltip("Kelipatan pembulatan posisi lokal. 1 = angka bulat; 2 = kelipatan satu sel grid.")]
    public float positionStep = 1f;

    [Header("Rotation")]

    [Tooltip("Bulatkan rotasi lokal saat objek diputar dengan gizmo di Scene view.")]
    public bool snapRotation = true;

    [Tooltip("Kelipatan pembulatan sudut lokal (derajat).")]
    public float rotationStep = 90f;

    [Header("Scale")]

    [Tooltip("Bulatkan skala lokal saat objek diskalakan dengan gizmo di Scene view.")]
    public bool snapScale = true;

    [Tooltip("Kelipatan pembulatan skala lokal.")]
    public float scaleStep = 1f;

    #endregion
}
