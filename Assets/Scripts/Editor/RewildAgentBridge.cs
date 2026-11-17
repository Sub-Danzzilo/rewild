using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Jembatan perintah untuk agen AI. Memantau `Tools/agent/queue` lalu menjalankan perintah yang
/// ditaruh di sana; hasilnya ditulis sebagai JSON di `Tools/agent/out`.
///
/// Tujuannya menggantikan pola "tulis skrip sementara lalu tunggu recompile": perintah bisa
/// dijalankan kapan saja, tanpa Unity kehilangan fokus dan tanpa menulis file C# baru.
///
/// Aktif hanya kalau `Tools/agent/enabled.flag` ada. Tanpa file itu jembatan diam.
/// Tidak ada jaringan, tidak ada port, dan tidak ada perintah yang menyimpan scene.
/// </summary>
[InitializeOnLoad]
public static class RewildAgentBridge
{
    #region Constants

    private const string AgentRootRelative = "../Tools/agent";
    private const string LogPrefix = "[RewildAgent]";
    private const double PollInterval = 0.3;
    private const int CellDumpLimit = 20000;

    #endregion

    #region Polling

    private static double nextPollTime;

    static RewildAgentBridge()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
        {
            return;
        }

        nextPollTime = EditorApplication.timeSinceStartup + PollInterval;

        string root = AgentRoot;
        if (!File.Exists(Path.Combine(root, "enabled.flag")))
        {
            return;
        }

        string queue = Path.Combine(root, "queue");
        if (!Directory.Exists(queue))
        {
            return;
        }

        string[] pending;
        try
        {
            pending = Directory.GetFiles(queue, "*.cmd");
        }
        catch (IOException)
        {
            return;
        }

        if (pending.Length == 0)
        {
            return;
        }

        Array.Sort(pending, StringComparer.Ordinal);

        // Satu perintah per putaran supaya tidak menahan editor terlalu lama.
        Execute(pending[0], root);
    }

    private static string AgentRoot
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, AgentRootRelative)); }
    }

    #endregion

    #region Command execution

    private static void Execute(string commandPath, string root)
    {
        string name = Path.GetFileNameWithoutExtension(commandPath);
        string command;

        try
        {
            command = File.ReadAllText(commandPath).Trim();
        }
        catch (IOException)
        {
            // File masih ditulis; biarkan, coba lagi di putaran berikutnya.
            return;
        }

        try
        {
            File.Delete(commandPath);
        }
        catch (IOException)
        {
            return;
        }

        bool failed = false;
        string result;

        try
        {
            result = Dispatch(command);
        }
        catch (Exception exception)
        {
            failed = true;
            result = "{\"ok\":false,\"error\":" + Quote(exception.GetType().Name + ": " + exception.Message) + "}";
        }

        string outputDirectory = Path.Combine(root, "out");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            Path.Combine(outputDirectory, name + ".result"),
            result,
            new UTF8Encoding(false)
        );

        Debug.Log(LogPrefix + " " + name + " (" + command + ") " + (failed ? "GAGAL" : "ok"));
    }

    private static string Dispatch(string command)
    {
        string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return Error("perintah kosong");
        }

        switch (parts[0].ToLowerInvariant())
        {
            case "ping":
                return Ping();
            case "state":
                return State(parts.Length > 1 ? parts[1] : null);
            case "view":
                return ViewReport();
            case "run":
                return RunAction(parts);
            default:
                return Error("perintah tidak dikenal: " + parts[0]);
        }
    }

    private static string Ping()
    {
        Json json = new Json();
        json.OpenObject();
        json.Field("ok", true);
        json.Field("unity", Application.unityVersion);
        json.Field("project", Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
        json.Field("scene", SceneManager.GetActiveScene().name);
        json.Field("isPlaying", EditorApplication.isPlaying);
        json.Field("isCompiling", EditorApplication.isCompiling);
        json.CloseObject();
        return json.ToString();
    }

    #endregion

    #region Actions

    private static string RunAction(string[] parts)
    {
        if (parts.Length < 2)
        {
            return Error("run butuh nama aksi (rebuild-collider / reimport)");
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return Error("ditolak: sedang play mode");
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return Error("ditolak: Unity sedang sibuk (kompilasi/import)");
        }

        string action = parts[1].ToLowerInvariant();
        string argument = parts.Length > 2 ? parts[2] : null;

        switch (action)
        {
            case "rebuild-collider":
                return RebuildColliders(argument);
            case "refresh-tilemap":
                return RefreshTilemaps(argument);

            case "reimport":
                return Reimport(argument);
            default:
                return Error("aksi tidak dikenal: " + action);
        }
    }

    /// <summary>
    /// Muat ulang data tile dari aset Tile-nya (mis. setelah warna tile diubah) tanpa menyentuh
    /// isi sel. Dipakai karena Tilemap menyimpan cache data tile per sel; tanpa ini perubahan warna
    /// di aset tile tidak terlihat di scene.
    /// </summary>
    private static string RefreshTilemaps(string tilemapFilter)
    {
        List<Tilemap> targets = CollectTilemaps(tilemapFilter);

        Json json = new Json();
        json.OpenObject();
        json.Field("ok", true);
        json.Field("count", targets.Count);
        json.Key("items");
        json.OpenArray();

        foreach (Tilemap tilemap in targets)
        {
            tilemap.RefreshAllTiles();

            json.OpenObject();
            json.Field("tilemap", tilemap.name);
            json.Field("tiles", CountTiles(tilemap));
            json.CloseObject();
        }

        json.CloseArray();
        json.CloseObject();
        return json.ToString();
    }

    /// <summary>Kumpulkan Tilemap di scene yang sedang terbuka, opsional disaring berdasarkan nama.</summary>
    private static List<Tilemap> CollectTilemaps(string tilemapFilter)
    {
        List<Tilemap> targets = new List<Tilemap>();

        Tilemap[] tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !tilemap.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tilemapFilter) && tilemap.name != tilemapFilter)
            {
                continue;
            }

            targets.Add(tilemap);
        }

        return targets;
    }

    /// <summary>
    /// Buang cache bentuk collider per tile dengan membuat ulang komponen TilemapCollider2D,
    /// lalu minta composite menyusun geometri baru. Tile, warna, flag, dan matrix tidak disentuh.
    /// Ini satu-satunya cara yang terbukti memperbarui collider setelah bentuk sprite berubah.
    /// </summary>
    private static string RebuildColliders(string tilemapFilter)
    {
        Json json = new Json();
        json.OpenObject();

        List<Tilemap> targets = new List<Tilemap>();

        Tilemap[] tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !tilemap.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tilemapFilter) && tilemap.name != tilemapFilter)
            {
                continue;
            }

            if (tilemap.GetComponent<TilemapCollider2D>() == null ||
                tilemap.GetComponent<CompositeCollider2D>() == null)
            {
                continue;
            }

            targets.Add(tilemap);
        }

        json.Field("ok", true);
        json.Field("count", targets.Count);
        json.Key("items");
        json.OpenArray();

        int changed = 0;

        foreach (Tilemap tilemap in targets)
        {
            CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
            TilemapCollider2D original = tilemap.GetComponent<TilemapCollider2D>();

            int beforePaths = composite.pathCount;
            Bounds beforeBounds = composite.bounds;

            TilemapCollider2D fresh = Recreate(original);

            composite.GenerateGeometry();
            Physics2D.SyncTransforms();

            if (tilemap.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
            }

            changed++;

            json.OpenObject();
            json.Field("tilemap", tilemap.name);
            json.Field("tiles", CountTiles(tilemap));
            json.Field("freshShapeCount", fresh != null ? fresh.shapeCount : -1);
            json.Field("pathsBefore", beforePaths);
            json.Field("pathsAfter", composite.pathCount);
            json.Field("boundsBefore", beforeBounds);
            json.Field("boundsAfter", composite.bounds);
            json.CloseObject();
        }

        json.CloseArray();
        json.Field("changed", changed);
        json.CloseObject();
        return json.ToString();
    }

    private static string Reimport(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
        {
            return Error("reimport butuh path yang diawali Assets/");
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Json json = new Json();
        json.OpenObject();
        json.Field("ok", true);
        json.Field("imported", assetPath);
        json.CloseObject();
        return json.ToString();
    }

    /// <summary>Ganti komponen dengan salinan baru yang pengaturannya sama, supaya cache bentuk hilang.</summary>
    private static TilemapCollider2D Recreate(TilemapCollider2D old)
    {
        GameObject target = old.gameObject;

        var operation = old.compositeOperation;
        bool isTrigger = old.isTrigger;
        PhysicsMaterial2D material = old.sharedMaterial;
        float density = old.density;
        Vector2 offset = old.offset;
        float extrusionFactor = old.extrusionFactor;
        uint maximumTileChangeCount = old.maximumTileChangeCount;
        bool useDelaunayMesh = old.useDelaunayMesh;
        int layerOverridePriority = old.layerOverridePriority;

        Undo.DestroyObjectImmediate(old);

        TilemapCollider2D fresh = Undo.AddComponent<TilemapCollider2D>(target);
        fresh.compositeOperation = operation;
        fresh.isTrigger = isTrigger;
        fresh.sharedMaterial = material;
        fresh.density = density;
        fresh.offset = offset;
        fresh.extrusionFactor = extrusionFactor;
        fresh.maximumTileChangeCount = maximumTileChangeCount;
        fresh.useDelaunayMesh = useDelaunayMesh;
        fresh.layerOverridePriority = layerOverridePriority;

        fresh.ProcessTilemapChanges();
        return fresh;
    }

    private static int CountTiles(Tilemap tilemap)
    {
        int count = 0;

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cell) != null)
            {
                count++;
            }
        }

        return count;
    }

    #endregion

    #region State dump

    /// <summary>Keadaan scene apa adanya: tile per sel, matrix, komponen collider, path composite.</summary>
    private static string State(string tilemapFilter)
    {
        Scene scene = SceneManager.GetActiveScene();

        Json json = new Json();
        json.OpenObject();
        json.Field("ok", true);
        json.Field("unity", Application.unityVersion);
        json.Field("scene", scene.name);
        json.Field("scenePath", scene.path);
        json.Field("sceneDirty", scene.isDirty);
        json.Field("isPlaying", EditorApplication.isPlaying);
        json.Field("isCompiling", EditorApplication.isCompiling);

        json.Key("tilemaps");
        json.OpenArray();

        Tilemap[] tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !tilemap.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tilemapFilter) && tilemap.name != tilemapFilter)
            {
                continue;
            }

            WriteTilemap(json, tilemap);
        }

        json.CloseArray();
        json.CloseObject();
        return json.ToString();
    }

    private static void WriteTilemap(Json json, Tilemap tilemap)
    {
        json.OpenObject();
        json.Field("name", tilemap.name);
        json.Field("layer", tilemap.gameObject.layer);
        json.Field("active", tilemap.gameObject.activeInHierarchy);

        var grid = tilemap.layoutGrid;
        if (grid != null)
        {
            json.Field("cellSizeX", grid.cellSize.x);
            json.Field("cellSizeY", grid.cellSize.y);
        }

        Vector3 anchor = tilemap.tileAnchor;
        json.Field("tileAnchorX", anchor.x);
        json.Field("tileAnchorY", anchor.y);

        BoundsInt bounds = tilemap.cellBounds;
        json.Field("originX", bounds.xMin);
        json.Field("originY", bounds.yMin);
        json.Field("sizeX", bounds.size.x);
        json.Field("sizeY", bounds.size.y);

        json.Key("cells");
        WriteCells(json, tilemap, bounds);

        TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            json.Key("tilemapCollider");
            json.OpenObject();
            json.Field("enabled", tilemapCollider.enabled);
            json.Field("compositeOperation", (int)tilemapCollider.compositeOperation);
            json.Field("isTrigger", tilemapCollider.isTrigger);
            json.Field("shapeCount", tilemapCollider.shapeCount);
            json.Field("extrusionFactor", tilemapCollider.extrusionFactor);
            json.Field("maximumTileChangeCount", tilemapCollider.maximumTileChangeCount);
            json.Field("useDelaunayMesh", tilemapCollider.useDelaunayMesh);
            json.Field("layerOverridePriority", tilemapCollider.layerOverridePriority);
            json.Field("bounds", tilemapCollider.bounds);
            json.CloseObject();
        }

        CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            json.Key("composite");
            json.OpenObject();
            json.Field("enabled", composite.enabled);
            json.Field("geometryType", (int)composite.geometryType);
            json.Field("generationType", (int)composite.generationType);
            json.Field("isTrigger", composite.isTrigger);
            json.Field("pathCount", composite.pathCount);
            json.Field("bounds", composite.bounds);
            json.Key("paths");
            WriteCompositePaths(json, composite);
            json.CloseObject();
        }

        Rigidbody2D body = tilemap.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            json.Key("rigidbody");
            json.OpenObject();
            json.Field("bodyType", (int)body.bodyType);
            json.Field("gravityScale", body.gravityScale);
            json.CloseObject();
        }

        json.CloseObject();
    }

    private static void WriteCells(Json json, Tilemap tilemap, BoundsInt bounds)
    {
        long volume = (long)bounds.size.x * bounds.size.y * Mathf.Max(1, bounds.size.z);

        if (volume > CellDumpLimit)
        {
            json.OpenObject();
            json.Field("skipped", true);
            json.Field("reason", "area terlalu besar");
            json.Field("volume", volume);
            json.CloseObject();
            return;
        }

        json.OpenArray();

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            if (tile == null)
            {
                continue;
            }

            Matrix4x4 matrix = tilemap.GetTransformMatrix(cell);
            Sprite sprite = tilemap.GetSprite(cell);
            Color color = tilemap.GetColor(cell);

            json.OpenObject();
            json.Field("x", cell.x);
            json.Field("y", cell.y);
            json.Field("z", cell.z);
            json.Field("tile", tile.name);
            json.Field("sprite", sprite != null ? sprite.name : null);
            json.Field("e00", matrix.m00);
            json.Field("e01", matrix.m01);
            json.Field("e10", matrix.m10);
            json.Field("e11", matrix.m11);
            json.Field("flags", (int)tilemap.GetTileFlags(cell));
            json.Field("color", color);
            json.CloseObject();
        }

        json.CloseArray();
    }

    private static void WriteCompositePaths(Json json, CompositeCollider2D composite)
    {
        MethodInfo getPath = typeof(CompositeCollider2D).GetMethod(
            "GetPath",
            new[] { typeof(int), typeof(List<Vector2>) }
        );

        if (getPath == null)
        {
            json.OpenObject();
            json.Field("unsupported", true);
            json.CloseObject();
            return;
        }

        List<Vector2> points = new List<Vector2>();

        json.OpenArray();

        for (int i = 0; i < composite.pathCount; i++)
        {
            points.Clear();
            getPath.Invoke(composite, new object[] { i, points });

            json.OpenArray();

            foreach (Vector2 point in points)
            {
                json.OpenArray();
                json.Value(point.x);
                json.Value(point.y);
                json.CloseArray();
            }

            json.CloseArray();
        }

        json.CloseArray();
    }

    #endregion

    #region View report

    /// <summary>
    /// Ringkasan geometri latar dan kamera: posisi/ukuran kamera, batas background, batas isi level,
    /// dan objek bantuan "(Auto)" buatan LevelBackgroundExtension. Hanya membaca, tidak mengubah
    /// apa pun, dan boleh dipanggil saat play mode.
    /// </summary>
    private static string ViewReport()
    {
        Json json = new Json();
        json.OpenObject();
        json.Field("ok", true);
        json.Field("scene", SceneManager.GetActiveScene().name);
        json.Field("isPlaying", EditorApplication.isPlaying);

        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        }

        if (camera != null)
        {
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            Vector3 cameraPosition = camera.transform.position;

            json.Key("camera");
            json.OpenObject();
            json.Field("name", camera.name);
            json.Field("position", cameraPosition);
            json.Field("orthographicSize", camera.orthographicSize);
            json.Field("aspect", camera.aspect);
            json.Field("backgroundColor", camera.backgroundColor);
            json.Field("viewMinX", cameraPosition.x - halfWidth);
            json.Field("viewMaxX", cameraPosition.x + halfWidth);
            json.Field("viewMinY", cameraPosition.y - halfHeight);
            json.Field("viewMaxY", cameraPosition.y + halfHeight);
            json.CloseObject();

            CameraDragController controller = camera.GetComponent<CameraDragController>();
            if (controller != null)
            {
                SerializedObject serialized = new SerializedObject(controller);

                json.Key("controller");
                json.OpenObject();
                WriteRectProperty(json, serialized, "levelBounds");
                WriteBoolProperty(json, serialized, "useBackgroundAsBounds");
                WriteBoolProperty(json, serialized, "useLevelContentAsBounds");
                WriteFloatProperty(json, serialized, "contentPadding");
                WriteBoolProperty(json, serialized, "extendBackground");
                WriteObjectProperty(json, serialized, "backgroundRenderer");
                json.CloseObject();
            }
        }

        if (camera != null)
        {
            LevelBackgroundExtension extension = camera.GetComponent<LevelBackgroundExtension>();

            json.Key("extension");
            json.OpenObject();
            json.Field("exists", extension != null);
            if (extension != null)
            {
                json.Field("enabled", extension.enabled);
                SerializedObject serializedExtension = new SerializedObject(extension);
                WriteBoolProperty(json, serializedExtension, "repeatHorizontally");
                WriteBoolProperty(json, serializedExtension, "mirrorTiles");
                WriteBoolProperty(json, serializedExtension, "fillGapsWithGradient");
                WriteBoolProperty(json, serializedExtension, "autoDetectThemeColors");
                WriteObjectProperty(json, serializedExtension, "backgroundRenderer");
            }
            json.CloseObject();
        }

        GameObject extensionRoot = GameObject.Find("Background Extension (Auto)");
        json.Key("extensionRoot");
        json.OpenObject();
        json.Field("exists", extensionRoot != null);
        if (extensionRoot != null)
        {
            json.Field("childCount", extensionRoot.transform.childCount);
            json.Key("children");
            json.OpenArray();
            foreach (Transform child in extensionRoot.transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                json.OpenObject();
                json.Field("name", child.name);
                if (renderer != null)
                {
                    json.Field("enabled", renderer.enabled);
                    json.Field("bounds", renderer.bounds);
                    json.Field("sortingOrder", renderer.sortingOrder);
                }
                json.CloseObject();
            }
            json.CloseArray();
        }
        json.CloseObject();

        GameObject background = GameObject.Find("Background (World)");
        SpriteRenderer backgroundRenderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
        if (backgroundRenderer != null)
        {
            Sprite sprite = backgroundRenderer.sprite;

            json.Key("background");
            json.OpenObject();
            json.Field("position", background.transform.position);
            json.Field("scale", background.transform.lossyScale);
            json.Field("sprite", sprite != null ? sprite.name : null);
            json.Field("texture", sprite != null && sprite.texture != null ? sprite.texture.name : null);
            json.Field("bounds", backgroundRenderer.bounds);
            json.Field("sortingOrder", backgroundRenderer.sortingOrder);
            json.Field("color", backgroundRenderer.color);
            json.CloseObject();
        }

        json.Key("autoObjects");
        json.OpenArray();
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None
                 ))
        {
            if (renderer == null || !renderer.name.Contains("(Auto)"))
            {
                continue;
            }

            json.OpenObject();
            json.Field("name", renderer.name);
            json.Field("enabled", renderer.enabled);
            json.Field("bounds", renderer.bounds);
            json.Field("sortingOrder", renderer.sortingOrder);
            json.Field("color", renderer.color);
            json.CloseObject();
        }
        json.CloseArray();

        json.Key("tilemaps");
        json.OpenArray();
        foreach (TilemapRenderer renderer in UnityEngine.Object.FindObjectsByType<TilemapRenderer>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None
                 ))
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid())
            {
                continue;
            }

            json.OpenObject();
            json.Field("name", renderer.name);
            json.Field("active", renderer.gameObject.activeInHierarchy);
            json.Field("bounds", renderer.bounds);
            json.CloseObject();
        }
        json.CloseArray();

        json.CloseObject();
        return json.ToString();
    }

    private static void WriteFloatProperty(Json json, SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            json.Field(name, property.floatValue);
        }
    }

    private static void WriteBoolProperty(Json json, SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            json.Field(name, property.boolValue);
        }
    }

    private static void WriteRectProperty(Json json, SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            return;
        }

        Rect rect = property.rectValue;
        json.Key(name);
        json.OpenObject();
        json.Field("xMin", rect.xMin);
        json.Field("xMax", rect.xMax);
        json.Field("yMin", rect.yMin);
        json.Field("yMax", rect.yMax);
        json.CloseObject();
    }

    private static void WriteObjectProperty(Json json, SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            return;
        }

        UnityEngine.Object value = property.objectReferenceValue;
        json.Field(name, value != null ? value.name : null);
    }

    #endregion

    #region JSON helper

    private static string Error(string message)
    {
        Json json = new Json();
        json.OpenObject();
        json.Field("ok", false);
        json.Field("error", message);
        json.CloseObject();
        return json.ToString();
    }

    /// <summary>Penulis JSON kecil: objek, array, string, angka, dan bool.</summary>
    private sealed class Json
    {
        private readonly StringBuilder text = new StringBuilder();
        private readonly bool[] comma = new bool[64];
        private int depth;
        private bool afterKey;

        public void OpenObject()
        {
            Place();
            text.Append('{');
            Push();
        }

        public void CloseObject()
        {
            Pop();
            text.Append('}');
        }

        public void OpenArray()
        {
            Place();
            text.Append('[');
            Push();
        }

        public void CloseArray()
        {
            Pop();
            text.Append(']');
        }

        public void Key(string name)
        {
            Place();
            text.Append(Quote(name)).Append(':');
            afterKey = true;
        }

        public void Field(string name, bool value)
        {
            Key(name);
            Raw(value ? "true" : "false");
        }

        public void Field(string name, int value)
        {
            Key(name);
            Raw(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Field(string name, uint value)
        {
            Key(name);
            Raw(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Field(string name, long value)
        {
            Key(name);
            Raw(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Field(string name, float value)
        {
            Key(name);
            Value(value);
        }

        public void Field(string name, string value)
        {
            Key(name);
            Raw(value == null ? "null" : Quote(value));
        }

        public void Field(string name, Bounds value)
        {
            Key(name);
            OpenObject();
            Field("cx", value.center.x);
            Field("cy", value.center.y);
            Field("ex", value.extents.x);
            Field("ey", value.extents.y);
            CloseObject();
        }

        public void Field(string name, Vector3 value)
        {
            Key(name);
            OpenObject();
            Field("x", value.x);
            Field("y", value.y);
            Field("z", value.z);
            CloseObject();
        }

        public void Field(string name, Color value)
        {
            Key(name);
            OpenObject();
            Field("r", value.r);
            Field("g", value.g);
            Field("b", value.b);
            Field("a", value.a);
            CloseObject();
        }

        public void Value(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                Place();
                text.Append("null");
                return;
            }

            Place();
            text.Append(value.ToString("0.######", CultureInfo.InvariantCulture));
        }

        public override string ToString()
        {
            return text.ToString();
        }

        private void Push()
        {
            depth++;
            comma[depth] = false;
        }

        private void Pop()
        {
            depth--;
        }

        /// <summary>
        /// Tulis nilai mentah (bool, angka bulat, null, string yang sudah di-quote). Dipanggil tepat
        /// setelah <see cref="Key(string)"/>; penanda "habis key" harus dibersihkan di sini, kalau
        /// tidak koma pemisah tidak pernah ditulis dan JSON-nya tidak sah.
        /// </summary>
        private void Raw(string value)
        {
            afterKey = false;
            text.Append(value);
        }

        private void Place()
        {
            if (afterKey)
            {
                afterKey = false;
                return;
            }

            if (depth > 0 && comma[depth])
            {
                text.Append(',');
            }

            if (depth > 0)
            {
                comma[depth] = true;
            }
        }
    }

    private static string Quote(string value)
    {
        StringBuilder text = new StringBuilder(value.Length + 2);
        text.Append('"');

        foreach (char character in value)
        {
            switch (character)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (character < ' ')
                    {
                        text.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        text.Append(character);
                    }

                    break;
            }
        }

        text.Append('"');
        return text.ToString();
    }

    #endregion
}
