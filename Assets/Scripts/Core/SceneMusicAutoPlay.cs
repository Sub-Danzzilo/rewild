using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Memulai musik setiap scene secara otomatis tanpa komponen tambahan di scene.
///
/// Scene level punya intro kamera, jadi musiknya dimulai oleh <see cref="LevelStartCameraIntro"/>
/// supaya tepat saat zoom ke tumpukan tanah dimulai. Scene lain (Home Screen, Level Selection)
/// langsung memutar musik begitu scene selesai dimuat.
///
/// Playlist-nya diambil dari Assets/Resources/MusicLibrary.asset berdasarkan nama scene, jadi
/// menambah level baru cukup menambah entri di asset itu. Scene tanpa entri (mis. Help)
/// tidak memutar apa pun dan membiarkan musik scene sebelumnya berjalan; saat kembali ke scene
/// pemilik playlist itu, AudioManager melanjutkan track yang sama dari posisi terakhir.
/// </summary>
public static class SceneMusicAutoPlay
{
    #region Unity Events

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayForScene(SceneManager.GetActiveScene());
    }

    #endregion

    #region Internal Helpers

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene);
    }

    private static void PlayForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        // Level dengan intro kamera mengurus musiknya sendiri (lihat LevelStartCameraIntro).
        if (Object.FindFirstObjectByType<LevelStartCameraIntro>() != null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySceneMusic(scene.name);
        }
    }

    #endregion
}
