using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SoundManagerPlayModeTests
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SoundManager[] managers = Object.FindObjectsByType<SoundManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            Object.Destroy(managers[i].gameObject);
        }

        GameObject listenerObject = GameObject.Find("SoundManager_TestAudioListener");
        if (listenerObject != null)
        {
            Object.Destroy(listenerObject);
        }

        PlayerPrefs.DeleteKey(SoundManager.MasterVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.LoopVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.SfxVolumeKey);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Instance_IsSetWhenCreated()
    {
        SoundManager manager = CreateConfiguredManager();
        yield return null;

        Assert.AreSame(manager, SoundManager.Instance);
    }

    [UnityTest]
    public IEnumerator DuplicateSoundManager_DestroysNewInstance()
    {
        SoundManager first = CreateConfiguredManager("SoundManager_First");
        GameObject secondObject = new GameObject("SoundManager_Second");
        SoundManager second = secondObject.AddComponent<SoundManager>();
        yield return null;

        Assert.AreSame(first, SoundManager.Instance);
        Assert.AreEqual(1, SoundManager.CountLiveInstances());
        Assert.IsTrue(second == null || second.gameObject == null);
    }

    [UnityTest]
    public IEnumerator TryPlayLoopForScene_Title_AssignsTitleClip()
    {
        AudioClip titleClip = CreateClip("title_loop", 1f);
        SoundManager manager = CreateConfiguredManager(titleClip: titleClip);
        yield return null;

        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Title"));
        yield return null;

        Assert.AreSame(titleClip, manager.LoopSource.clip);
        Assert.IsTrue(manager.LoopSource.loop);
        Assert.AreSame(titleClip, manager.GetCurrentLoopClip());
        Assert.AreEqual("AudioTest_Title", manager.GetCurrentLoopSceneName());
    }

    [UnityTest]
    public IEnumerator TryPlayLoopForScene_Main_AssignsMainClip()
    {
        AudioClip mainClip = CreateClip("main_loop", 1f);
        SoundManager manager = CreateConfiguredManager(mainClip: mainClip);
        yield return null;

        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Main"));
        yield return null;

        Assert.AreSame(mainClip, manager.LoopSource.clip);
        Assert.IsTrue(manager.LoopSource.loop);
        Assert.AreSame(mainClip, manager.GetCurrentLoopClip());
        Assert.AreEqual("AudioTest_Main", manager.GetCurrentLoopSceneName());
    }

    [UnityTest]
    public IEnumerator TryPlayLoopForScene_SameScene_DoesNotRestartClip()
    {
        SoundManager manager = CreateConfiguredManager();
        yield return null;

        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Title"));
        yield return null;

        manager.LoopSource.timeSamples = 1000;
        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Title"));
        yield return null;

        Assert.GreaterOrEqual(manager.LoopSource.timeSamples, 1000);
    }

    [UnityTest]
    public IEnumerator TryPlayLoopForScene_MissingScene_StopsLoopWhenConfigured()
    {
        SoundManager manager = CreateConfiguredManager();
        SetPrivateField(manager, "stopLoopWhenSceneHasNoEntry", true);
        yield return null;

        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Title"));
        yield return null;
        Assert.IsFalse(manager.TryPlayLoopForScene("MissingScene"));
        yield return null;

        Assert.IsFalse(manager.LoopSource.isPlaying);
        Assert.IsNull(manager.LoopSource.clip);
        Assert.IsNull(manager.GetCurrentLoopClip());
    }

    [UnityTest]
    public IEnumerator PlaySfx_ButtonClick_UsesPooledSource()
    {
        SoundManager manager = CreateConfiguredManager();
        yield return null;

        manager.PlaySfx("button_click");
        yield return null;

        Assert.GreaterOrEqual(manager.ActiveSfxSourceCount, 1);
    }

    [UnityTest]
    public IEnumerator PlaySfx_MissingId_LogsWarningWithoutException()
    {
        SoundManager manager = CreateConfiguredManager();
        yield return null;

        LogAssert.Expect(LogType.Warning, new Regex("SFX ID not found: missing_sfx"));
        manager.PlaySfx("missing_sfx");
        yield return null;

        Assert.AreEqual(0, manager.ActiveSfxSourceCount);
    }

    [UnityTest]
    public IEnumerator PlaySfx_ExceedInitialPool_ExpandsWhenAllowed()
    {
        SoundManager manager = CreateConfiguredManager();
        int initialPoolSize = manager.SfxPoolCount;
        yield return null;

        for (int i = 0; i < initialPoolSize + 1; i++)
        {
            manager.PlaySfx("button_click");
        }

        yield return null;

        Assert.Greater(manager.SfxPoolCount, initialPoolSize);
    }

    [UnityTest]
    public IEnumerator Volume_SaveAndLoad_RestoresPlayerPrefsValues()
    {
        SoundManager manager = CreateConfiguredManager();
        yield return null;

        manager.SetMasterVolume(0.8f);
        manager.SetLoopVolume(0.7f);
        manager.SetSfxVolume(0.5f);
        manager.SaveVolumeSettings();

        manager.SetMasterVolume(0.1f);
        manager.SetLoopVolume(0.1f);
        manager.SetSfxVolume(0.1f);
        manager.LoadVolumeSettings();

        Assert.AreEqual(0.8f, manager.GetMasterVolume());
        Assert.AreEqual(0.7f, manager.GetLoopVolume());
        Assert.AreEqual(0.5f, manager.GetSfxVolume());
    }

    private static SoundManager CreateConfiguredManager(string name = "SoundManager_PlayModeTest", AudioClip titleClip = null, AudioClip mainClip = null)
    {
        EnsureAudioListener();
        PlayerPrefs.DeleteKey(SoundManager.MasterVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.LoopVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.SfxVolumeKey);

        GameObject gameObject = new GameObject(name);
        SoundManager manager = gameObject.AddComponent<SoundManager>();
        SetPrivateField(manager, "useLoopFade", false);

        titleClip = titleClip != null ? titleClip : CreateClip("title_loop", 1f);
        mainClip = mainClip != null ? mainClip : CreateClip("main_loop", 1f);
        AudioClip buttonClip = CreateClip("button_click", 1f);

        SetPrivateField(manager, "sceneLoopEntries", new List<SceneLoopEntry>
        {
            CreateSceneLoopEntry("AudioTest_Title", titleClip, 1f),
            CreateSceneLoopEntry("AudioTest_Main", mainClip, 1f),
            CreateSceneLoopEntry("title", titleClip, 1f),
            CreateSceneLoopEntry("main", mainClip, 1f)
        });

        SetPrivateField(manager, "sfxLibrary", new List<SoundEntry>
        {
            CreateSoundEntry("button_click", buttonClip)
        });

        return manager;
    }

    private static void EnsureAudioListener()
    {
        if (Object.FindFirstObjectByType<AudioListener>() != null)
        {
            return;
        }

        GameObject listenerObject = new GameObject("SoundManager_TestAudioListener");
        listenerObject.AddComponent<AudioListener>();
    }

    private static SceneLoopEntry CreateSceneLoopEntry(string sceneName, AudioClip clip, float volume)
    {
        SceneLoopEntry entry = new SceneLoopEntry();
        SetPrivateField(entry, "sceneName", sceneName);
        SetPrivateField(entry, "loopClip", clip);
        SetPrivateField(entry, "volume", volume);
        return entry;
    }

    private static SoundEntry CreateSoundEntry(string id, AudioClip clip)
    {
        SoundEntry entry = new SoundEntry();
        SetPrivateField(entry, "id", id);
        SetPrivateField(entry, "clip", clip);
        SetPrivateField(entry, "volume", 1f);
        SetPrivateField(entry, "pitchRange", new Vector2(1f, 1f));
        return entry;
    }

    private static AudioClip CreateClip(string name, float durationSeconds)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Sin(i * 0.04f) * 0.1f;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, "Missing field: " + fieldName);
        field.SetValue(target, value);
    }
}
