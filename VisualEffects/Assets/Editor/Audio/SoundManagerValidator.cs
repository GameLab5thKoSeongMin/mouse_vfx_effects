// Code responsibility: validate generated SoundManager assets, scene setup, SFX IDs, and build settings.
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SoundManagerValidator
{
    private static readonly string[] RequiredLoopSceneNames =
    {
        "AudioTest_Title",
        "AudioTest_Main",
        "title",
        "main"
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

    [MenuItem("Tools/Audio/Run SoundManager Validation")]
    public static void RunValidation()
    {
        bool hasError = false;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoundManagerTestSceneBuilder.SoundManagerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("SoundManager prefab is missing: " + SoundManagerTestSceneBuilder.SoundManagerPrefabPath);
            hasError = true;
        }
        else
        {
            ValidatePrefab(prefab, ref hasError);
        }

        ValidateSceneExists(SoundManagerTestSceneBuilder.BootstrapScenePath, ref hasError);
        ValidateSceneExists(SoundManagerTestSceneBuilder.TitleScenePath, ref hasError);
        ValidateSceneExists(SoundManagerTestSceneBuilder.MainScenePath, ref hasError);
        ValidateBuildSettings(SoundManagerTestSceneBuilder.BootstrapScenePath, ref hasError);
        ValidateBuildSettings(SoundManagerTestSceneBuilder.TitleScenePath, ref hasError);
        ValidateBuildSettings(SoundManagerTestSceneBuilder.MainScenePath, ref hasError);

        if (hasError)
        {
            Debug.LogError("SoundManager validation finished with errors. See previous messages.");
        }
        else
        {
            Debug.Log("SoundManager validation finished successfully.");
        }
    }

    private static void ValidatePrefab(GameObject prefab, ref bool hasError)
    {
        SoundManager manager = prefab.GetComponent<SoundManager>();
        if (manager == null)
        {
            Debug.LogError("SoundManager prefab has no SoundManager component.", prefab);
            hasError = true;
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        if (serializedManager.FindProperty("loopSource").objectReferenceValue == null)
        {
            Debug.LogWarning("SoundManager prefab loopSource is not assigned. Runtime can auto-create it, but prefab assignment is recommended.", prefab);
        }

        int initialPoolSize = serializedManager.FindProperty("initialSfxPoolSize").intValue;
        int maxPoolSize = serializedManager.FindProperty("maxSfxPoolSize").intValue;
        if (initialPoolSize < 1 || maxPoolSize < initialPoolSize)
        {
            Debug.LogError("SoundManager SFX pool settings are invalid. initial must be >= 1 and max must be >= initial.", prefab);
            hasError = true;
        }

        ValidateSceneLoopEntries(serializedManager.FindProperty("sceneLoopEntries"), prefab, ref hasError);
        ValidateSfxLibrary(serializedManager.FindProperty("sfxLibrary"), prefab, ref hasError);
        ValidatePlayerPrefsKeys(ref hasError);
    }

    private static void ValidateSceneLoopEntries(SerializedProperty entriesProperty, Object context, ref bool hasError)
    {
        if (entriesProperty == null)
        {
            Debug.LogError("SoundManager prefab is missing sceneLoopEntries field.", context);
            hasError = true;
            return;
        }

        if (entriesProperty.arraySize == 0)
        {
            Debug.LogWarning("SceneLoopEntry list is empty. No scene loop audio will play.", context);
        }

        HashSet<string> sceneNames = new HashSet<string>();
        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
            string sceneName = entry.FindPropertyRelative("sceneName").stringValue;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("SceneLoopEntry has an empty sceneName at index " + i + ".", context);
                continue;
            }

            if (!sceneNames.Add(sceneName))
            {
                Debug.LogWarning("Duplicate SceneLoopEntry sceneName found: " + sceneName, context);
            }

            if (entry.FindPropertyRelative("loopClip").objectReferenceValue == null)
            {
                Debug.LogWarning("SceneLoopEntry has no loopClip assigned: " + sceneName, context);
            }
        }

        for (int i = 0; i < RequiredLoopSceneNames.Length; i++)
        {
            if (!sceneNames.Contains(RequiredLoopSceneNames[i]))
            {
                Debug.LogWarning("Required SceneLoopEntry sceneName is missing: " + RequiredLoopSceneNames[i], context);
            }
        }
    }

    private static void ValidateSfxLibrary(SerializedProperty libraryProperty, Object context, ref bool hasError)
    {
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < libraryProperty.arraySize; i++)
        {
            SerializedProperty entry = libraryProperty.GetArrayElementAtIndex(i);
            string id = entry.FindPropertyRelative("id").stringValue;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("SFX entry has an empty ID at index " + i + ".", context);
                continue;
            }

            if (!ids.Add(id))
            {
                Debug.LogWarning("Duplicate SFX ID found: " + id, context);
            }

            if (entry.FindPropertyRelative("clip").objectReferenceValue == null)
            {
                Debug.LogWarning("SFX ID has no AudioClip assigned: " + id, context);
            }
        }

        for (int i = 0; i < RequiredSfxIds.Length; i++)
        {
            if (!ids.Contains(RequiredSfxIds[i]))
            {
                Debug.LogWarning("Required SFX ID is missing: " + RequiredSfxIds[i], context);
            }
        }
    }

    private static void ValidatePlayerPrefsKeys(ref bool hasError)
    {
        if (ReadPublicStringConstant("MasterVolumeKey") != "Sound.MasterVolume"
            || ReadPublicStringConstant("LoopVolumeKey") != "Sound.LoopVolume"
            || ReadPublicStringConstant("SfxVolumeKey") != "Sound.SfxVolume")
        {
            Debug.LogError("SoundManager PlayerPrefs key constants do not match the expected values.");
            hasError = true;
        }
    }

    private static string ReadPublicStringConstant(string fieldName)
    {
        FieldInfo field = typeof(SoundManager).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        return field != null ? field.GetRawConstantValue() as string : null;
    }

    private static void ValidateSceneExists(string scenePath, ref bool hasError)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogError("SoundManager test scene is missing: " + scenePath);
            hasError = true;
        }
    }

    private static void ValidateBuildSettings(string scenePath, ref bool hasError)
    {
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
            if (scene.path == scenePath && scene.enabled)
            {
                return;
            }
        }

        Debug.LogWarning("Scene is not enabled in Build Settings: " + scenePath);
    }
}
