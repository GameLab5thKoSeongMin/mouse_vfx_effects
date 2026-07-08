// Code responsibility: generate SoundManager test audio clips, prefab, and audio validation scenes.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class SoundManagerTestSceneBuilder
{
    public const string SoundManagerPrefabPath = "Assets/Prefabs/Audio/SoundManager.prefab";
    public const string BootstrapScenePath = "Assets/Scenes/AudioTest_Bootstrap.unity";
    public const string TitleScenePath = "Assets/Scenes/AudioTest_Title.unity";
    public const string MainScenePath = "Assets/Scenes/AudioTest_Main.unity";

    private const string TestAudioFolder = "Assets/Audio/TestGenerated";

    private static readonly SoundClipSpec[] TestClipSpecs =
    {
        new SoundClipSpec("Test_Title_Wave_Loop", 0.35f, 2.4f, true),
        new SoundClipSpec("Test_Main_BGM_Loop", 0.22f, 2.4f, true),
        new SoundClipSpec("Test_Button_Click", 0.55f, 0.12f, false),
        new SoundClipSpec("Test_Object_Click", 0.42f, 0.12f, false),
        new SoundClipSpec("Test_Paper_Open", 0.30f, 0.18f, false),
        new SoundClipSpec("Test_Approve", 0.70f, 0.18f, false),
        new SoundClipSpec("Test_Reject", 0.25f, 0.20f, false),
        new SoundClipSpec("Test_Stamp", 0.18f, 0.16f, false),
        new SoundClipSpec("Test_Warning", 0.12f, 0.24f, false)
    };

    private static readonly string[] RequiredSfxIds =
    {
        "button_click",
        "object_click",
        "paper_open",
        "paper_close",
        "page_flip",
        "approve",
        "reject",
        "stamp",
        "warning"
    };

    [MenuItem("Tools/Audio/Create Test Audio Clips")]
    public static void CreateTestAudioClips()
    {
        EnsureAssetFolders();

        for (int i = 0; i < TestClipSpecs.Length; i++)
        {
            SoundClipSpec spec = TestClipSpecs[i];
            string path = TestAudioFolder + "/" + spec.Name + ".wav";
            WriteSineWaveWav(path, spec.FrequencyNormalized, spec.DurationSeconds, spec.LoopFriendly);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                importer.loadInBackground = false;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    compressionFormat = AudioCompressionFormat.PCM,
                    loadType = AudioClipLoadType.DecompressOnLoad,
                    preloadAudioData = true,
                    quality = 1f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
                };
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("SoundManager test audio clips created in " + TestAudioFolder + ".");
    }

    [MenuItem("Tools/Audio/Create SoundManager Prefab")]
    public static void CreateSoundManagerPrefab()
    {
        EnsureAssetFolders();
        CreateTestAudioClips();

        GameObject root = new GameObject("SoundManager", typeof(AudioSource), typeof(SoundManager));
        AudioSource loopSource = root.GetComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;

        SoundManager manager = root.GetComponent<SoundManager>();
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("loopSource").objectReferenceValue = loopSource;
        serializedManager.FindProperty("stopLoopWhenSceneHasNoEntry").boolValue = true;
        FillSceneLoopEntries(serializedManager.FindProperty("sceneLoopEntries"));
        serializedManager.FindProperty("initialSfxPoolSize").intValue = 8;
        serializedManager.FindProperty("maxSfxPoolSize").intValue = 24;
        serializedManager.FindProperty("allowPoolExpansion").boolValue = true;
        FillSfxLibrary(serializedManager.FindProperty("sfxLibrary"));
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SoundManagerPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("SoundManager prefab created: " + SoundManagerPrefabPath);
    }

    [MenuItem("Tools/Audio/Create SoundManager Test Scenes")]
    public static void CreateSoundManagerTestScenes()
    {
        EnsureAssetFolders();

        if (!File.Exists(SoundManagerPrefabPath))
        {
            CreateSoundManagerPrefab();
        }

        CreateBootstrapScene();
        CreateAudioScene(TitleScenePath, "AudioTest_Title", "Title loop test scene", "Expected loop: Test_Title_Wave_Loop");
        CreateAudioScene(MainScenePath, "AudioTest_Main", "Main BGM test scene", "Expected loop: Test_Main_BGM_Loop");
        AddScenesToBuildSettings();
        AssetDatabase.Refresh();
        Debug.Log("SoundManager test scenes created.");
    }

    public static void CreateAll()
    {
        CreateTestAudioClips();
        CreateSoundManagerPrefab();
        CreateSoundManagerTestScenes();
    }

    private static void CreateBootstrapScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "AudioTest_Bootstrap";

        CreateEventSystem();
        CreateCamera();
        SoundManagerDebugPanel debugPanel = CreateDebugPanel();
        CreateCanvasWithButtons("Sound Manager Bootstrap", "Creates the persistent SoundManager and provides SFX/scene tests.", debugPanel);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoundManagerPrefabPath);
        if (prefab != null)
        {
            PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            Debug.LogWarning("SoundManager prefab missing while creating bootstrap scene.");
        }

        EditorSceneManager.SaveScene(scene, BootstrapScenePath);
    }

    private static void CreateAudioScene(string path, string sceneName, string title, string description)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;

        CreateEventSystem();
        CreateCamera();
        SoundManagerDebugPanel debugPanel = CreateDebugPanel();
        CreateCanvasWithButtons(title, description, debugPanel);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateCanvasWithButtons(string title, string description, SoundManagerDebugPanel debugPanel)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateImage("Background", canvasObject.transform, new Color(0.10f, 0.12f, 0.14f, 1f), true);
        CreateText("Title", canvasObject.transform, title, 30, FontStyle.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -86f), new Vector2(-40f, -30f));
        CreateText("Description", canvasObject.transform, description, 18, FontStyle.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -134f), new Vector2(-40f, -94f));

        Button titleButton = CreateButton("Go To Title Test Scene", canvasObject.transform, "Go To Title Test Scene", new Vector2(-190f, 210f));
        UnityEventTools.AddPersistentListener(titleButton.onClick, debugPanel.LoadTitleScene);
        titleButton.gameObject.AddComponent<UIButtonSound>();

        Button mainButton = CreateButton("Go To Main Test Scene", canvasObject.transform, "Go To Main Test Scene", new Vector2(190f, 210f));
        UnityEventTools.AddPersistentListener(mainButton.onClick, debugPanel.LoadMainScene);
        mainButton.gameObject.AddComponent<UIButtonSound>();

        string[] sfxIds = { "button_click", "object_click", "paper_open", "approve", "reject", "stamp", "warning" };
        for (int i = 0; i < sfxIds.Length; i++)
        {
            int row = i / 4;
            int col = i % 4;
            Vector2 position = new Vector2(-345f + col * 230f, 110f - row * 70f);
            Button button = CreateButton("Play " + sfxIds[i], canvasObject.transform, sfxIds[i], position);
            UIButtonSound buttonSound = button.gameObject.AddComponent<UIButtonSound>();
            SerializedObject serializedButtonSound = new SerializedObject(buttonSound);
            serializedButtonSound.FindProperty("sfxId").stringValue = sfxIds[i];
            serializedButtonSound.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ObjectClickSound_TestCube";
        cube.transform.position = new Vector3(0f, 0f, 4f);
        cube.transform.localScale = Vector3.one * 1.2f;
        cube.AddComponent<ObjectClickSound>();
    }

    private static SoundManagerDebugPanel CreateDebugPanel()
    {
        GameObject panel = new GameObject("SoundManagerDebugPanel", typeof(SoundManagerDebugPanel));
        panel.transform.position = Vector3.zero;
        return panel.GetComponent<SoundManagerDebugPanel>();
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.GetComponent<EventSystem>().sendNavigationEvents = true;
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
        eventSystem.AddComponent<StandaloneInputModule>();
#else
        Debug.LogWarning("SoundManager test scene EventSystem has no input module because no supported input backend is enabled.");
#endif
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PhysicsRaycaster));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.14f, 1f);
    }

    private static GameObject CreateImage(string name, Transform parent, Color color, bool stretch)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (stretch)
        {
            Stretch((RectTransform)imageObject.transform);
        }

        return imageObject;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = ResolveBuiltInFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = CreateImage(name, parent, new Color(0.24f, 0.44f, 0.56f, 1f), false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(210f, 48f);
        rect.anchoredPosition = position;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.24f, 0.44f, 0.56f, 1f);
        colors.highlightedColor = new Color(0.34f, 0.58f, 0.72f, 1f);
        colors.pressedColor = new Color(0.17f, 0.31f, 0.41f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText("Label", buttonObject.transform, label, 16, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void FillSfxLibrary(SerializedProperty libraryProperty)
    {
        libraryProperty.ClearArray();

        Dictionary<string, string> clipById = new Dictionary<string, string>
        {
            { "button_click", "Test_Button_Click" },
            { "object_click", "Test_Object_Click" },
            { "paper_open", "Test_Paper_Open" },
            { "paper_close", "Test_Paper_Open" },
            { "page_flip", "Test_Paper_Open" },
            { "approve", "Test_Approve" },
            { "reject", "Test_Reject" },
            { "stamp", "Test_Stamp" },
            { "warning", "Test_Warning" }
        };

        for (int i = 0; i < RequiredSfxIds.Length; i++)
        {
            libraryProperty.InsertArrayElementAtIndex(i);
            SerializedProperty entryProperty = libraryProperty.GetArrayElementAtIndex(i);
            string id = RequiredSfxIds[i];
            entryProperty.FindPropertyRelative("id").stringValue = id;
            entryProperty.FindPropertyRelative("clip").objectReferenceValue = LoadTestClip(clipById[id]);
            entryProperty.FindPropertyRelative("volume").floatValue = 1f;
            entryProperty.FindPropertyRelative("pitchRange").vector2Value = new Vector2(1f, 1f);
        }
    }

    private static void FillSceneLoopEntries(SerializedProperty entriesProperty)
    {
        entriesProperty.ClearArray();

        AddSceneLoopEntry(entriesProperty, 0, "AudioTest_Title", LoadTestClip("Test_Title_Wave_Loop"), 1f);
        AddSceneLoopEntry(entriesProperty, 1, "AudioTest_Main", LoadTestClip("Test_Main_BGM_Loop"), 1f);
        AddSceneLoopEntry(entriesProperty, 2, "title", LoadTestClip("Test_Title_Wave_Loop"), 1f);
        AddSceneLoopEntry(entriesProperty, 3, "main", LoadTestClip("Test_Main_BGM_Loop"), 1f);
    }

    private static void AddSceneLoopEntry(SerializedProperty entriesProperty, int index, string sceneName, AudioClip loopClip, float volume)
    {
        entriesProperty.InsertArrayElementAtIndex(index);
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
        entryProperty.FindPropertyRelative("sceneName").stringValue = sceneName;
        entryProperty.FindPropertyRelative("loopClip").objectReferenceValue = loopClip;
        entryProperty.FindPropertyRelative("volume").floatValue = Mathf.Clamp01(volume);
    }

    private static AudioClip LoadTestClip(string clipName)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(TestAudioFolder + "/" + clipName + ".wav");
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddBuildScene(scenes, BootstrapScenePath);
        AddBuildScene(scenes, TitleScenePath);
        AddBuildScene(scenes, MainScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path)
            {
                scenes[i].enabled = true;
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static void EnsureAssetFolders()
    {
        EnsureFolder("Assets", "Audio");
        EnsureFolder("Assets/Audio", "BGM");
        EnsureFolder("Assets/Audio", "SFX");
        EnsureFolder("Assets/Audio", "TestGenerated");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Audio");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets/Scripts", "Audio");
        EnsureFolder("Assets", "Editor");
        EnsureFolder("Assets/Editor", "Audio");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Font ResolveBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void WriteSineWaveWav(string assetPath, float frequencyNormalized, float durationSeconds, bool loopFriendly)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        const int sampleRate = 44100;
        const short channels = 1;
        const short bitsPerSample = 16;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int dataSize = sampleCount * blockAlign;

        using (BinaryWriter writer = new BinaryWriter(File.Open(fullPath, FileMode.Create)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            float frequency = Mathf.Lerp(160f, 880f, frequencyNormalized);
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float envelope = loopFriendly ? 0.35f : Mathf.Clamp01(1f - (i / (float)sampleCount));
                short sample = (short)Mathf.Clamp(wave * envelope * short.MaxValue, short.MinValue, short.MaxValue);
                writer.Write(sample);
            }
        }
    }

    private struct SoundClipSpec
    {
        public string Name;
        public float FrequencyNormalized;
        public float DurationSeconds;
        public bool LoopFriendly;

        public SoundClipSpec(string name, float frequencyNormalized, float durationSeconds, bool loopFriendly)
        {
            Name = name;
            FrequencyNormalized = frequencyNormalized;
            DurationSeconds = durationSeconds;
            LoopFriendly = loopFriendly;
        }
    }

}
