#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Screenshot cepat untuk mode debug. Saat Play Mode, tekan F12 di scene mana pun: gambar
/// disimpan ke folder "Screenshots" di akar proyek dengan nama scene + waktu.
///
/// Sebelum menangkap, Game view dipaksa ke resolusi tetap (lihat <see cref="CaptureWidth"/>), karena
/// ScreenCapture dengan parameter superSize hanya membesarkan render kamera: Canvas (UI) tetap
/// dirender di ukuran Game view, sehingga UI menumpuk kecil di pojok gambar. Ukuran Game view lama
/// dikembalikan setelah gambar diambil.
///
/// Seluruh isi file dibungkus #if UNITY_EDITOR, jadi build sama sekali tidak membawa kode ini.
/// Tidak perlu dimatikan manual sebelum build.
///
/// Tidak perlu komponen di scene: kelas ini memasang dirinya sendiri lewat
/// RuntimeInitializeOnLoadMethod, sama seperti SceneMusicAutoPlay, sehingga otomatis berlaku di
/// semua scene (Home Screen, Level Selection, level, dan scene intro).
/// </summary>
public static class DebugScreenshotTaker
{
    #region Constants

    // F12 dipilih karena tidak dipakai gameplay mana pun (gerak, ayun web, tembak, pause).
    private const Key ScreenshotKey = Key.F12;

    // Resolusi Game view saat menangkap. 1920x1080 sama dengan referensi Canvas Scaler, jadi UI
    // tampil persis seperti di build. Ganti ke 3840x2160 kalau butuh gambar 4K.
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;

    // Jeda beberapa frame supaya UI sempat menata ulang setelah resolusi Game view berubah.
    private const int FramesBeforeCapture = 3;
    private const int FramesBeforeRestore = 3;

    private const string OutputFolderName = "Screenshots";

    #endregion

    #region State

    // 0 = diam, 1 = menunggu resize, 2 = menunggu file selesai ditulis.
    private static int captureStage;

    private static int framesLeft;
    private static string pendingPath;
    private static int previousSizeIndex = -1;

    #endregion

    #region Unity Events

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // Input System baru tidak punya event tombol global, jadi pengecekan dilakukan setiap
        // input update. Dengan cara ini tidak perlu komponen di scene mana pun.
        InputSystem.onAfterUpdate -= HandleInputUpdate;
        InputSystem.onAfterUpdate += HandleInputUpdate;

        Debug.Log("[Rewild] Screenshot debug siap: tekan F12 untuk menyimpan gambar "
                  + CaptureWidth + "x" + CaptureHeight + " ke folder " + OutputFolderName + ".");
    }

    #endregion

    #region Capture Flow

    private static void HandleInputUpdate()
    {
        // RuntimeInitializeOnLoadMethod juga berjalan di editor, jadi pastikan hanya saat Play.
        if (!Application.isPlaying || captureStage != 0)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[ScreenshotKey].wasPressedThisFrame)
        {
            return;
        }

        BeginCapture();
    }

    private static void BeginCapture()
    {
        pendingPath = BuildFilePath();

        bool gameViewTooSmall = Screen.width < CaptureWidth || Screen.height < CaptureHeight;
        if (gameViewTooSmall && TryApplyGameViewSize(CaptureWidth, CaptureHeight))
        {
            captureStage = 1;
            framesLeft = FramesBeforeCapture;
            EditorApplication.update += StepCapture;
            return;
        }

        CaptureNow();
    }

    private static void StepCapture()
    {
        if (framesLeft > 0)
        {
            framesLeft--;
            return;
        }

        if (captureStage == 1)
        {
            CaptureNow();
            captureStage = 2;
            framesLeft = FramesBeforeRestore;
            return;
        }

        if (captureStage == 2)
        {
            RestoreGameViewSize();
            EditorApplication.update -= StepCapture;
            captureStage = 0;
        }
    }

    private static void CaptureNow()
    {
        // superSize sengaja 1: resolusi sudah diatur lewat Game view, dan superSize > 1 justru
        // membuat UI mengecil di pojok gambar.
        ScreenCapture.CaptureScreenshot(pendingPath, 1);

        if (Screen.width < CaptureWidth || Screen.height < CaptureHeight)
        {
            Debug.LogWarning(string.Format(
                CultureInfo.InvariantCulture,
                "[Rewild] Screenshot diambil di {0}x{1}, lebih kecil dari {2}x{3}. "
                + "Set Game view ke resolusi tetap (bukan Free Aspect) atau naikkan Scale-nya.",
                Screen.width, Screen.height, CaptureWidth, CaptureHeight));
        }

        Debug.Log(string.Format(
            CultureInfo.InvariantCulture,
            "[Rewild] Screenshot {0}x{1} -> {2}",
            Screen.width, Screen.height, pendingPath));
    }

    private static string BuildFilePath()
    {
        string folder = Path.Combine(ProjectRoot, OutputFolderName);

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception exception)
        {
            Debug.LogError("[Rewild] Gagal membuat folder screenshot: " + exception.Message);
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "Untitled";
        }

        string fileName = Sanitize(sceneName) + "_"
                          + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff", CultureInfo.InvariantCulture)
                          + ".png";

        return Path.Combine(folder, fileName);
    }

    #endregion

    #region Game View Size

    // Mengubah resolusi Game view lewat API internal editor. Semua nama diperiksa dulu, dan kalau
    // ada yang berubah di versi Unity lain, fungsi ini gagal dengan sopan (screenshot tetap jalan
    // memakai ukuran Game view saat itu).
    private static bool TryApplyGameViewSize(int width, int height)
    {
        try
        {
            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroup");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Type groupKindType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");

            if (gameViewType == null || sizesType == null || groupType == null
                || sizeType == null || sizeKindType == null || groupKindType == null)
            {
                Debug.LogWarning("[Rewild] API Game view tidak dikenali di Unity ini; "
                                 + "screenshot memakai ukuran Game view sekarang.");
                return false;
            }

            EditorWindow gameView = FindGameViewWindow(gameViewType);
            if (gameView == null)
            {
                return false;
            }

            object sizes = GetGameViewSizesInstance(sizesType);
            if (sizes == null)
            {
                return false;
            }

            object group = sizesType.GetMethod("GetGroup")
                ?.Invoke(sizes, new[] { Enum.Parse(groupKindType, "Standalone") });
            if (group == null)
            {
                return false;
            }

            int index = FindOrAddSize(groupType, sizeType, sizeKindType, group, width, height);
            if (index < 0)
            {
                return false;
            }

            int currentIndex = ReadSizeIndex(gameView, gameViewType);
            if (!WriteSizeIndex(gameView, gameViewType, index))
            {
                return false;
            }

            previousSizeIndex = currentIndex;
            gameView.Repaint();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Rewild] Gagal mengubah resolusi Game view: " + exception.Message);
            return false;
        }
    }

    private static void RestoreGameViewSize()
    {
        if (previousSizeIndex < 0)
        {
            return;
        }

        try
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            EditorWindow gameView = gameViewType == null ? null : FindGameViewWindow(gameViewType);
            if (gameView != null && WriteSizeIndex(gameView, gameViewType, previousSizeIndex))
            {
                gameView.Repaint();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Rewild] Gagal mengembalikan resolusi Game view: " + exception.Message);
        }

        previousSizeIndex = -1;
    }

    private static EditorWindow FindGameViewWindow(Type gameViewType)
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        for (int i = 0; i < windows.Length; i++)
        {
            if (windows[i] != null && windows[i].GetType() == gameViewType)
            {
                return windows[i];
            }
        }

        return EditorWindow.GetWindow(gameViewType, false, null, false);
    }

    private static object GetGameViewSizesInstance(Type sizesType)
    {
        PropertyInfo direct = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
        if (direct != null)
        {
            return direct.GetValue(null);
        }

        // Cadangan: instance berasal dari ScriptableSingleton<GameViewSizes>.
        Type singletonType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ScriptableSingleton`1");
        if (singletonType == null)
        {
            return null;
        }

        PropertyInfo property = singletonType.MakeGenericType(sizesType)
            .GetProperty("instance", BindingFlags.Public | BindingFlags.Static);

        return property == null ? null : property.GetValue(null);
    }

    private static int FindOrAddSize(Type groupType, Type sizeType, Type sizeKindType, object group,
                                     int width, int height)
    {
        MethodInfo getTotalCount = groupType.GetMethod("GetTotalCount");
        MethodInfo getGameViewSize = groupType.GetMethod("GetGameViewSize");
        if (getTotalCount == null || getGameViewSize == null)
        {
            return -1;
        }

        int index = FindSize(groupType, group, getTotalCount, getGameViewSize, width, height);
        if (index >= 0)
        {
            return index;
        }

        ConstructorInfo constructor = sizeType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { sizeKindType, typeof(int), typeof(int), typeof(string) },
            null);

        MethodInfo addCustomSize = groupType.GetMethod("AddCustomSize");
        if (constructor == null || addCustomSize == null)
        {
            return -1;
        }

        object entry = constructor.Invoke(new[]
        {
            Enum.Parse(sizeKindType, "FixedResolution"), (object)width, height, width + "x" + height
        });

        addCustomSize.Invoke(group, new[] { entry });

        // AddCustomSize tidak selalu mengembalikan index, jadi daftar dicari ulang.
        return FindSize(groupType, group, getTotalCount, getGameViewSize, width, height);
    }

    private static int FindSize(Type groupType, object group, MethodInfo getTotalCount,
                               MethodInfo getGameViewSize, int width, int height)
    {
        int count = (int)getTotalCount.Invoke(group, null);
        for (int i = 0; i < count; i++)
        {
            object entry = getGameViewSize.Invoke(group, new object[] { i });
            if (entry == null)
            {
                continue;
            }

            if (ReadIntMember(entry, "width", "m_Width") == width
                && ReadIntMember(entry, "height", "m_Height") == height)
            {
                return i;
            }
        }

        return -1;
    }

    private static int ReadSizeIndex(EditorWindow gameView, Type gameViewType)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        PropertyInfo property = gameViewType.GetProperty("selectedSizeIndex", flags);
        if (property != null)
        {
            return (int)property.GetValue(gameView);
        }

        FieldInfo field = gameViewType.GetField("selectedSizeIndex", flags);
        return field == null ? -1 : (int)field.GetValue(gameView);
    }

    private static bool WriteSizeIndex(EditorWindow gameView, Type gameViewType, int index)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        PropertyInfo property = gameViewType.GetProperty("selectedSizeIndex", flags);
        if (property != null && property.CanWrite)
        {
            property.SetValue(gameView, index);
            return true;
        }

        FieldInfo field = gameViewType.GetField("selectedSizeIndex", flags);
        if (field != null)
        {
            field.SetValue(gameView, index);
            return true;
        }

        return false;
    }

    private static int ReadIntMember(object target, string propertyName, string fieldName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = target.GetType();

        PropertyInfo property = type.GetProperty(propertyName, flags);
        if (property != null && property.PropertyType == typeof(int))
        {
            return (int)property.GetValue(target);
        }

        FieldInfo field = type.GetField(fieldName, flags);
        return field != null && field.FieldType == typeof(int) ? (int)field.GetValue(target) : -1;
    }

    #endregion

    #region Internal Helpers

    // Nama scene dipakai sebagai nama file, jadi buang karakter yang tidak boleh ada di path.
    private static string Sanitize(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
        {
            name = name.Replace(invalid[i], '_');
        }

        return name.Trim();
    }

    private static string ProjectRoot
    {
        get { return Path.GetDirectoryName(Application.dataPath); }
    }

    #endregion
}
#endif
