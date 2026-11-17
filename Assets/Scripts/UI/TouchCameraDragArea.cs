using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Area kosong di layar untuk menggeser kamera dengan jari: setiap bagian layar yang bukan tombol
/// sentuh (dan bukan HUD) bisa ditahan lalu digeser, sama seperti tahan-klik-kanan di Windows.
///
/// Area ini sengaja diletakkan paling belakang di canvas overlay, jadi tombol jalan, analog bidik,
/// dan HUD menerima sentuhan lebih dulu. Delta geserannya diteruskan ke
/// <see cref="CameraDragController"/> lewat <see cref="TouchControlsInput"/>.
/// </summary>
[DisallowMultipleComponent]
public class TouchCameraDragArea : MonoBehaviour, IDragHandler
{
    #region Inspector Fields

    [Tooltip("Pengali delta jari sebelum diteruskan ke kamera. 1 = kamera mengikuti jari apa adanya. " +
             "Turunkan kalau geser kamera terasa terlalu cepat di layar HP.")]
    [Min(0.1f)]
    [SerializeField] private float sensitivity = 1f;

    #endregion

    #region Event System Handlers

    public void OnDrag(PointerEventData eventData)
    {
        // Saat membidik, kamera memang tidak boleh bergeser (CameraDragController yang memutuskan);
        // delta tetap dikirim supaya tidak ada sisa gerakan setelah bidikan selesai.
        TouchControlsInput.AddCameraDrag(eventData.delta * sensitivity);
    }

    #endregion
}
