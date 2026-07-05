using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using BallSort;
using BallSort.Core;
using BallSort.Level;
using BallSort.UI;

/// <summary>
/// BallSort/Setup Scene menüsü ile tüm sahneyi, prefabları ve asset'leri
/// sıfırdan oluşturan Editor yardımcısı.
/// </summary>
public static class SceneSetupTool
{
    const string SpriteDir    = "Assets/Resources/Sprites";
    const string PaletteAsset = "Assets/Resources/BallColorPalette.asset";
    const string PrefabDir    = "Assets/Resources/Prefabs";
    const string PrefabPath   = "Assets/Resources/Prefabs/TubePrefab.prefab";

    [MenuItem("BallSort/Setup Scene")]
    public static void SetupScene()
    {
        EnsureDirectory("Assets/Resources");
        EnsureDirectory(SpriteDir);
        EnsureDirectory(PrefabDir);

        var ballSprite = GetOrCreateSprite(SpriteDir + "/ball.png",  64,  64,  true,  64);
        var tubeSprite = GetOrCreateSprite(SpriteDir + "/tube.png",  32,  128, false, 32);
        var palette    = GetOrCreatePalette(ballSprite);
        var tubePrefab = GetOrCreateTubePrefab(ballSprite, tubeSprite);

        BuildSceneObjects(palette, tubePrefab);

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Debug.Log("[BallSort] Sahne kurulumu tamamlandi! Play'e basarak oynayabilirsin.");
    }

    // ─── Sprite ───────────────────────────────────────────────────────────

    static Sprite GetOrCreateSprite(string path, int w, int h, bool circle, int ppu)
    {
        if (!File.Exists(path))
        {
            var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            float cx   = w * 0.5f - 0.5f;
            float cy   = h * 0.5f - 0.5f;
            float r    = Mathf.Min(w, h) * 0.5f - 1f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool inside = !circle || ((x - cx) * (x - cx) + (y - cy) * (y - cy)) <= r * r;
                    pixels[y * w + x] = inside ? Color.white : Color.clear;
                }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType          = TextureImporterType.Sprite;
            imp.spriteImportMode     = SpriteImportMode.Single;
            imp.spritePixelsPerUnit  = ppu;
            imp.filterMode           = FilterMode.Bilinear;
            imp.alphaIsTransparency  = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ─── BallColorPalette ─────────────────────────────────────────────────

    static BallColorPalette GetOrCreatePalette(Sprite ballSprite)
    {
        var pal = AssetDatabase.LoadAssetAtPath<BallColorPalette>(PaletteAsset);
        if (pal != null) return pal;

        pal = ScriptableObject.CreateInstance<BallColorPalette>();
        var so      = new SerializedObject(pal);
        var entries = so.FindProperty("_entries");

        (BallColor id, Color col)[] data =
        {
            (BallColor.Red,    new Color(0.90f, 0.20f, 0.20f)),
            (BallColor.Blue,   new Color(0.20f, 0.45f, 0.90f)),
            (BallColor.Green,  new Color(0.20f, 0.78f, 0.30f)),
            (BallColor.Yellow, new Color(0.95f, 0.85f, 0.10f)),
            (BallColor.Purple, new Color(0.60f, 0.20f, 0.80f)),
            (BallColor.Orange, new Color(0.95f, 0.55f, 0.10f)),
            (BallColor.Teal,   new Color(0.10f, 0.75f, 0.75f)),
            (BallColor.Pink,   new Color(0.95f, 0.40f, 0.65f)),
        };

        entries.arraySize = data.Length;
        for (int i = 0; i < data.Length; i++)
        {
            var e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("colorId").intValue             = (int)data[i].id;
            e.FindPropertyRelative("unityColor").colorValue        = data[i].col;
            e.FindPropertyRelative("sprite").objectReferenceValue  = ballSprite;
        }
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(pal, PaletteAsset);
        AssetDatabase.SaveAssets();
        return pal;
    }

    // ─── Tube Prefab ──────────────────────────────────────────────────────

    static GameObject GetOrCreateTubePrefab(Sprite ballSprite, Sprite tubeSprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        // Root: Tube component + BoxCollider2D (required)
        var root = new GameObject("TubePrefab");
        root.AddComponent<BoxCollider2D>().size = new Vector2(1f, 4f);

        // TubeBackground child (tube_rect sprite = 1 x 4 world units)
        var bgGO = new GameObject("TubeBackground");
        bgGO.transform.SetParent(root.transform, false);
        var tubeRenderer       = bgGO.AddComponent<SpriteRenderer>();
        tubeRenderer.sprite        = tubeSprite;
        tubeRenderer.color         = new Color(0.78f, 0.82f, 0.90f, 0.75f);
        tubeRenderer.sortingOrder  = 0;

        // 4 ball children; centers at -1.5, -0.5, +0.5, +1.5 (1 unit apart in 4-unit tube)
        var ballRenderers = new SpriteRenderer[4];
        float[] ys = { -1.5f, -0.5f, 0.5f, 1.5f };
        for (int i = 0; i < 4; i++)
        {
            var ballGO = new GameObject($"Ball_{i}");
            ballGO.transform.SetParent(root.transform, false);
            ballGO.transform.localPosition = new Vector3(0f, ys[i], -0.05f);
            ballGO.transform.localScale    = new Vector3(0.85f, 0.85f, 1f);

            var sr            = ballGO.AddComponent<SpriteRenderer>();
            sr.sprite         = ballSprite;
            sr.color          = Color.white;
            sr.sortingOrder   = 1;
            ballRenderers[i]  = sr;
        }

        // Wire Tube component
        var tube   = root.AddComponent<Tube>();
        var tubeSO = new SerializedObject(tube);
        var ballArr = tubeSO.FindProperty("_ballRenderers");
        ballArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            ballArr.GetArrayElementAtIndex(i).objectReferenceValue = ballRenderers[i];
        tubeSO.FindProperty("_tubeRenderer").objectReferenceValue = tubeRenderer;
        tubeSO.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ─── Scene ────────────────────────────────────────────────────────────

    static void BuildSceneObjects(BallColorPalette palette, GameObject tubePrefab)
    {
        // Camera
        var camGO = GameObject.FindWithTag("MainCamera");
        Camera cam;
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        else
        {
            cam = camGO.GetComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.backgroundColor    = new Color(0.12f, 0.13f, 0.18f);
        cam.orthographic       = true;
        cam.orthographicSize   = 5f;
        if (!camGO.TryGetComponent<Physics2DRaycaster>(out _))
            camGO.AddComponent<Physics2DRaycaster>();

        // EventSystem (tubes ve UI için gerekli)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // TubeContainer — tüplerin spawn noktası
        var containerGO = new GameObject("TubeContainer");
        containerGO.transform.position = new Vector3(0f, -1f, 0f);

        // GameManager
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("_tubePrefab").objectReferenceValue    = tubePrefab;
        gmSO.FindProperty("_tubeContainer").objectReferenceValue = containerGO.transform;
        gmSO.FindProperty("_colorPalette").objectReferenceValue  = palette;
        gmSO.ApplyModifiedProperties();

        // LevelManager — Resources/Levels/*.json otomatik yükler
        new GameObject("LevelManager").AddComponent<LevelManager>();

        // Canvas + UI
        BuildCanvas();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static void BuildCanvas()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var gameUI = canvasGO.AddComponent<GameUI>();

        // HUD — üst bar
        var hudGO = MakeRect(canvasGO.transform, "HUD");
        SetAnchors(hudGO, new Vector2(0,1), new Vector2(1,1),
                   new Vector2(20,-155), new Vector2(-20,-10));
        var vhud = hudGO.AddComponent<VerticalLayoutGroup>();
        vhud.spacing              = 6;
        vhud.childForceExpandWidth  = true;
        vhud.childForceExpandHeight = false;
        vhud.childAlignment         = TextAnchor.UpperCenter;
        vhud.padding                = new RectOffset(10, 10, 10, 10);

        var levelNameText = MakeTMP(hudGO.transform, "LevelNameText", "Level 1", 36, FontStyle.Bold);
        AddLayoutElement(levelNameText.gameObject, preferredHeight: 50);
        var movesText     = MakeTMP(hudGO.transform, "MovesText",     "Hamleler: 0", 26, FontStyle.Normal);
        AddLayoutElement(movesText.gameObject, preferredHeight: 40);

        // Alt buton satırı
        var btnRowGO = MakeRect(canvasGO.transform, "ButtonRow");
        SetAnchors(btnRowGO, new Vector2(0,0), new Vector2(1,0),
                   new Vector2(20,20), new Vector2(-20,120));
        var hlg = btnRowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 20;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding              = new RectOffset(10, 10, 10, 10);

        var undoButton    = MakeButton(btnRowGO.transform, "UndoButton",    "Geri Al");
        var restartButton = MakeButton(btnRowGO.transform, "RestartButton", "Yeniden");

        // Kazanma paneli
        var winGO = MakeRect(canvasGO.transform, "WinPanel");
        SetAnchors(winGO, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f),
                   Vector2.zero, Vector2.zero);
        var winImg   = winGO.AddComponent<Image>();
        winImg.color = new Color(0.08f, 0.09f, 0.15f, 0.97f);
        var vlg      = winGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 24;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.padding              = new RectOffset(40, 40, 50, 50);
        winGO.SetActive(false);

        var winMovesText     = MakeTMP(winGO.transform, "WinMovesText",     "Tebrikler!", 38, FontStyle.Bold);
        AddLayoutElement(winMovesText.gameObject, preferredHeight: 60);
        var winBestText      = MakeTMP(winGO.transform, "WinBestText",      "",           24, FontStyle.Normal);
        AddLayoutElement(winBestText.gameObject, preferredHeight: 40);
        var nextLevelButton  = MakeButton(winGO.transform, "NextLevelButton",  "Sonraki Seviye");
        AddLayoutElement(nextLevelButton.gameObject, preferredHeight: 80);
        var winRestartButton = MakeButton(winGO.transform, "WinRestartButton", "Yeniden Oyna");
        AddLayoutElement(winRestartButton.gameObject, preferredHeight: 80);

        // GameUI field bağlantıları
        var guiSO = new SerializedObject(gameUI);
        guiSO.FindProperty("_levelNameText").objectReferenceValue    = levelNameText;
        guiSO.FindProperty("_movesText").objectReferenceValue        = movesText;
        guiSO.FindProperty("_undoButton").objectReferenceValue       = undoButton;
        guiSO.FindProperty("_restartButton").objectReferenceValue    = restartButton;
        guiSO.FindProperty("_winPanel").objectReferenceValue         = winGO;
        guiSO.FindProperty("_winMovesText").objectReferenceValue     = winMovesText;
        guiSO.FindProperty("_winBestText").objectReferenceValue      = winBestText;
        guiSO.FindProperty("_nextLevelButton").objectReferenceValue  = nextLevelButton;
        guiSO.FindProperty("_winRestartButton").objectReferenceValue = winRestartButton;
        guiSO.ApplyModifiedProperties();
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────

    static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetAnchors(GameObject go,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
                                   float fontSize, FontStyle style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.color      = Color.white;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img   = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.42f, 0.72f);
        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lblRT       = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;
        var tmp         = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = label;
        tmp.fontSize    = 22;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.color       = Color.white;

        return btn;
    }

    static void AddLayoutElement(GameObject go, float preferredHeight)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
    }

    static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        var folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
