using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Scene Names")]
    [Tooltip("Nama scene menu utama (Home Screen). Dipakai tombol Back pada menu lain. Harus terdaftar di Scenes In Build.")]
    [SerializeField] private string homeScene = "Home Screen";

    [Tooltip("Nama scene pemilihan level (Level Selection). Dipakai tombol Play di Home Screen.")]
    [SerializeField] private string levelSelectScene = "Level Selection";

    [Tooltip("Nama scene level 1. Dipakai tombol level di scene Level Selection.")]
    [SerializeField] private string level1Scene = "Level 1";

    [Tooltip("Nama scene panduan bermain (How To Play). Dipakai tombol How to Play di Home Screen.")]
    [SerializeField] private string howToPlayScene = "How To Play";

    #endregion

    #region Public Methods

    /// <summary>Kembali ke menu utama.</summary>
    public void LoadHome()
    {
        LoadScene(homeScene);
    }

    /// <summary>Membuka scene pemilihan level.</summary>
    public void LoadLevelSelect()
    {
        LoadScene(levelSelectScene);
    }

    /// <summary>Memuat scene level 1.</summary>
    public void LoadLevel1()
    {
        LoadScene(level1Scene);
    }

    /// <summary>Membuka scene panduan bermain.</summary>
    public void LoadHowToPlay()
    {
        LoadScene(howToPlayScene);
    }

    /// <summary>Menutup game. Di Editor, Play Mode dihentikan supaya tetap bisa dites tanpa build.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Internal Helpers

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty on GameSceneController.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    #endregion
}
