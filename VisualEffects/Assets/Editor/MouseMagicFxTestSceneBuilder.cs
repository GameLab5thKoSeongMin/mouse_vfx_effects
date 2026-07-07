// 코드 책임: MouseMagicFx 검증용 UI 테스트 장면과 재사용 프리팹을 에디터에서 생성한다.
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

public static class MouseMagicFxTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MouseMagicFx_TestScene.unity";
    private const string PrefabPath = "Assets/Prefabs/UI/Effects/MouseFxRoot.prefab";

    [MenuItem("Tools/Mouse Magic FX/Create Test Scene")]
    public static void CreateTestScene()
    {
        EnsureAssetFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MouseMagicFx_TestScene";

        CreateEventSystem();

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateBackground(canvasObject.transform);

        GameObject mouseFxRoot = CreateMouseFxRoot(canvasObject.transform);
        MouseMagicFxController fxController = mouseFxRoot.GetComponent<MouseMagicFxController>();

        CreateDocumentPanel(canvasObject.transform, fxController);
        mouseFxRoot.transform.SetAsLastSibling();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("Mouse Magic FX test scene created: " + ScenePath);
    }

    [MenuItem("Tools/Mouse Magic FX/Create MouseFxRoot Prefab")]
    public static void CreateMouseFxRootPrefab()
    {
        EnsureAssetFolders();
        GameObject mouseFxRoot = CreateMouseFxRoot(null);
        PrefabUtility.SaveAsPrefabAsset(mouseFxRoot, PrefabPath);
        Object.DestroyImmediate(mouseFxRoot);
        AssetDatabase.Refresh();
        Debug.Log("Mouse Magic FX prefab created: " + PrefabPath);
    }

    public static void CreateAll()
    {
        CreateMouseFxRootPrefab();
        CreateTestScene();
    }

    private static void EnsureAssetFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "UI");
        EnsureFolder("Assets/Prefabs/UI", "Effects");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.GetComponent<EventSystem>().sendNavigationEvents = true;
        ConfigureInputModule(eventSystem);
    }

    private static void ConfigureInputModule(GameObject eventSystem)
    {
        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            Object.DestroyImmediate(standaloneInputModule);
        }

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        eventSystem.AddComponent<StandaloneInputModule>();
#else
        Debug.LogWarning("Mouse Magic FX test scene EventSystem has no UI input module because no supported input backend is enabled.");
#endif
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject background = CreateImage("DemoBackground", parent, new Color(0.11f, 0.16f, 0.21f, 1f));
        RectTransform rect = background.GetComponent<RectTransform>();
        Stretch(rect);
        background.GetComponent<Image>().raycastTarget = false;
    }

    private static void CreateDocumentPanel(Transform parent, MouseMagicFxController fxController)
    {
        GameObject panel = CreateImage("DemoDocumentPanel", parent, new Color(0.93f, 0.89f, 0.78f, 1f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 470f);
        panelRect.anchoredPosition = Vector2.zero;

        MouseMagicFxDemoControls controls = panel.AddComponent<MouseMagicFxDemoControls>();

        Text titleText = CreateText(
            "Title text",
            panel.transform,
            "Mouse Magic FX Test Scene",
            30,
            FontStyle.Bold,
            new Color(0.14f, 0.13f, 0.10f, 1f));
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(42f, -80f);
        titleRect.offsetMax = new Vector2(-42f, -28f);

        Text bodyText = CreateText(
            "Body text",
            panel.transform,
            "Move the mouse to see the golden dust trail.\nLeft click to see the golden ring burst.\nClick the buttons to confirm the FX layer does not block UI.",
            20,
            FontStyle.Normal,
            new Color(0.22f, 0.20f, 0.16f, 1f));
        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(48f, -190f);
        bodyRect.offsetMax = new Vector2(-48f, -98f);

        Text statusText = CreateText(
            "Status text",
            panel.transform,
            "Ready. Move the mouse or left click over this document.",
            18,
            FontStyle.Italic,
            new Color(0.28f, 0.22f, 0.10f, 1f));
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.offsetMin = new Vector2(48f, 135f);
        statusRect.offsetMax = new Vector2(-48f, 175f);

        Text countText = CreateText(
            "Click count text",
            panel.transform,
            "Button clicks: 0",
            18,
            FontStyle.Normal,
            new Color(0.22f, 0.20f, 0.16f, 1f));
        RectTransform countRect = countText.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(0.5f, 0f);
        countRect.offsetMin = new Vector2(48f, 94f);
        countRect.offsetMax = new Vector2(-48f, 124f);

        Button clickButton = CreateButton("Click Test Button", panel.transform, "Click Test Button", new Vector2(-145f, 44f));
        Button toggleButton = CreateButton("Toggle FX Button", panel.transform, "Disable FX", new Vector2(145f, 44f));

        Text toggleButtonText = toggleButton.GetComponentInChildren<Text>();

        SerializedObject serializedControls = new SerializedObject(controls);
        serializedControls.FindProperty("fxController").objectReferenceValue = fxController;
        serializedControls.FindProperty("statusText").objectReferenceValue = statusText;
        serializedControls.FindProperty("clickCountText").objectReferenceValue = countText;
        serializedControls.FindProperty("toggleButtonText").objectReferenceValue = toggleButtonText;
        serializedControls.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(clickButton.onClick, controls.RegisterClick);
        UnityEventTools.AddPersistentListener(toggleButton.onClick, controls.ToggleFx);
    }

    private static GameObject CreateMouseFxRoot(Transform parent)
    {
        GameObject root = new GameObject("MouseFxRoot", typeof(RectTransform), typeof(MouseMagicFxController));
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect);
        root.transform.SetAsLastSibling();
        return root;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = ResolveBuiltInFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = CreateImage(name, parent, new Color(0.55f, 0.42f, 0.18f, 1f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(230f, 52f);
        buttonRect.anchoredPosition = position;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.55f, 0.42f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.72f, 0.57f, 0.25f, 1f);
        colors.pressedColor = new Color(0.42f, 0.31f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.35f, 0.32f, 0.25f, 0.45f);
        button.colors = colors;

        Text text = CreateText("Label", buttonObject.transform, label, 18, FontStyle.Bold, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        Stretch(textRect);

        return button;
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
}
