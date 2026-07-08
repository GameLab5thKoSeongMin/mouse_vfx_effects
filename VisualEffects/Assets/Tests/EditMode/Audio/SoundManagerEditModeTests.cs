using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SoundManagerEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        SoundManager[] managers = Object.FindObjectsByType<SoundManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            Object.DestroyImmediate(managers[i].gameObject);
        }

        PlayerPrefs.DeleteKey(SoundManager.MasterVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.LoopVolumeKey);
        PlayerPrefs.DeleteKey(SoundManager.SfxVolumeKey);
    }

    [Test]
    public void SceneLoopEntry_EmptySceneName_IsInvalid()
    {
        SceneLoopEntry entry = new SceneLoopEntry();

        Assert.IsFalse(entry.HasValidSceneName);
    }

    [Test]
    public void SceneLoopEntry_NullLoopClip_IsWarningTarget()
    {
        SceneLoopEntry entry = CreateSceneLoopEntry("AudioTest_Title", null, 1f);

        Assert.IsTrue(entry.HasValidSceneName);
        Assert.IsFalse(entry.HasValidLoopClip);
    }

    [Test]
    public void SoundEntry_EmptyId_IsInvalid()
    {
        SoundEntry entry = new SoundEntry();

        Assert.IsFalse(entry.HasValidId);
    }

    [Test]
    public void SfxLibrary_DuplicateId_LogsWarning()
    {
        SoundManager manager = CreateManager();
        AudioClip clip = CreateClip("button");
        SetPrivateField(manager, "sfxLibrary", new List<SoundEntry>
        {
            CreateSoundEntry("button_click", clip),
            CreateSoundEntry("button_click", clip)
        });

        LogAssert.Expect(LogType.Warning, new Regex("Duplicate SFX ID ignored: button_click"));
        manager.PlaySfx("button_click");
    }

    [Test]
    public void SceneLoopTable_DuplicateSceneName_ReturnsWarning()
    {
        SoundManager manager = CreateManager();
        AudioClip clip = CreateClip("loop");
        SetPrivateField(manager, "sceneLoopEntries", new List<SceneLoopEntry>
        {
            CreateSceneLoopEntry("AudioTest_Title", clip, 1f),
            CreateSceneLoopEntry("AudioTest_Title", clip, 1f)
        });

        LogAssert.Expect(LogType.Warning, new Regex("Duplicate SceneLoopEntry sceneName ignored: AudioTest_Title"));
        Assert.IsTrue(manager.TryPlayLoopForScene("AudioTest_Title"));
    }

    [Test]
    public void PlayerPrefsKeyNames_AreExpected()
    {
        Assert.AreEqual("Sound.MasterVolume", SoundManager.MasterVolumeKey);
        Assert.AreEqual("Sound.LoopVolume", SoundManager.LoopVolumeKey);
        Assert.AreEqual("Sound.SfxVolume", SoundManager.SfxVolumeKey);
    }

    [Test]
    public void Volume_SetValueOutsideRange_ClampsToZeroOne()
    {
        SoundManager manager = CreateManager();

        manager.SetMasterVolume(-1f);
        manager.SetLoopVolume(2f);
        manager.SetSfxVolume(1.5f);

        Assert.AreEqual(0f, manager.GetMasterVolume());
        Assert.AreEqual(1f, manager.GetLoopVolume());
        Assert.AreEqual(1f, manager.GetSfxVolume());
    }

    private static SoundManager CreateManager()
    {
        GameObject gameObject = new GameObject("SoundManager_EditModeTest");
        SoundManager manager = gameObject.AddComponent<SoundManager>();
        SetPrivateField(manager, "useLoopFade", false);
        return manager;
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

    private static AudioClip CreateClip(string name)
    {
        AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
        float[] samples = new float[4410];
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
