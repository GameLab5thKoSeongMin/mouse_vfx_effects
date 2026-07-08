// Code responsibility: render a lightweight IMGUI debug panel for SoundManager state, SFX tests,
// scene navigation, and singleton duplicate warnings in audio test scenes.
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SoundManagerDebugPanel : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "AudioTest_Title";
    [SerializeField] private string mainSceneName = "AudioTest_Main";
    [SerializeField] private string[] testLoopSceneNames =
    {
        "title",
        "main",
        "AudioTest_Title",
        "AudioTest_Main"
    };
    [SerializeField] private string[] testSfxIds =
    {
        "button_click",
        "object_click",
        "paper_open",
        "approve",
        "reject",
        "stamp",
        "warning"
    };

    [SerializeField] private Rect panelRect = new Rect(16f, 16f, 360f, 520f);
    [SerializeField] private bool showPanel = true;

    private Vector2 scrollPosition;

    public string TitleSceneName
    {
        get { return titleSceneName; }
    }

    public string MainSceneName
    {
        get { return mainSceneName; }
    }

    public void LoadTitleScene()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            return;
        }

        panelRect = GUILayout.Window(GetInstanceID(), panelRect, DrawPanel, "Sound Manager Debug");
    }

    private void DrawPanel(int windowId)
    {
        SoundManager manager = SoundManager.Instance;
        int instanceCount = SoundManager.CountLiveInstances();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(340f), GUILayout.Height(460f));
        GUILayout.Label("Current scene: " + SceneManager.GetActiveScene().name);
        GUILayout.Label("SoundManager instances: " + instanceCount);

        if (instanceCount > 1)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("Warning: duplicate SoundManager instances detected.");
            GUI.color = Color.white;
        }

        if (manager == null)
        {
            GUI.color = Color.red;
            GUILayout.Label("SoundManager.Instance is null.");
            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUI.DragWindow();
            return;
        }

        GUILayout.Label("Loop scene key: " + manager.GetCurrentLoopSceneName());
        GUILayout.Label("Loop clip: " + manager.GetCurrentLoopClipName());
        GUILayout.Label("Scene loop entries: " + manager.SceneLoopEntryCount);
        GUILayout.Label("SFX entries: " + manager.SfxEntryCount);
        GUILayout.Label("SFX pool: " + manager.SfxPoolCount + " / " + manager.MaxSfxPoolSize);
        GUILayout.Label("Active SFX sources: " + manager.ActiveSfxSourceCount);
        GUILayout.Space(8f);

        DrawVolumeSlider("Master", manager.GetMasterVolume(), manager.SetMasterVolume);
        DrawVolumeSlider("Loop", manager.GetLoopVolume(), manager.SetLoopVolume);
        DrawVolumeSlider("SFX", manager.GetSfxVolume(), manager.SetSfxVolume);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Volumes"))
        {
            manager.SaveVolumeSettings();
        }

        if (GUILayout.Button("Load Volumes"))
        {
            manager.LoadVolumeSettings();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear PlayerPrefs Sound Keys"))
        {
            manager.ClearVolumeSettings();
        }

        GUILayout.Space(8f);
        GUILayout.Label("Loop Tests");
        for (int i = 0; i < testLoopSceneNames.Length; i++)
        {
            string sceneName = testLoopSceneNames[i];
            if (GUILayout.Button("Play Loop: " + sceneName))
            {
                manager.TryPlayLoopForScene(sceneName);
            }
        }

        GUILayout.Space(8f);
        GUILayout.Label("SFX Tests");
        for (int i = 0; i < testSfxIds.Length; i++)
        {
            string sfxId = testSfxIds[i];
            if (GUILayout.Button("Play " + sfxId))
            {
                manager.PlaySfx(sfxId);
            }
        }

        GUILayout.Space(8f);
        GUILayout.Label("Scene Load Tests");
        if (GUILayout.Button("Go To Title Test Scene"))
        {
            LoadTitleScene();
        }

        if (GUILayout.Button("Go To Main Test Scene"))
        {
            LoadMainScene();
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private static void DrawVolumeSlider(string label, float currentValue, System.Action<float> applyValue)
    {
        GUILayout.Label(label + " Volume: " + currentValue.ToString("0.00"));
        float nextValue = GUILayout.HorizontalSlider(currentValue, 0f, 1f);
        if (!Mathf.Approximately(nextValue, currentValue))
        {
            applyValue(nextValue);
        }
    }
}
