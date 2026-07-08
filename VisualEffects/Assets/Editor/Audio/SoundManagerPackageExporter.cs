// Code responsibility: export the reusable SoundManager runtime, prefab, audio folders, and optional test assets.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SoundManagerPackageExporter
{
    private const string PackagePath = "Exports/SoundManager.unitypackage";

    [MenuItem("Tools/Audio/Export SoundManager Package")]
    public static void ExportPackage()
    {
        SoundManagerTestSceneBuilder.CreateAll();
        Directory.CreateDirectory("Exports");

        List<string> assets = new List<string>
        {
            "Assets/Scripts/Audio",
            "Assets/Prefabs/Audio/SoundManager.prefab",
            "Assets/Audio/BGM",
            "Assets/Audio/SFX",
            "Assets/Scenes/AudioTest_Bootstrap.unity",
            "Assets/Scenes/AudioTest_Title.unity",
            "Assets/Scenes/AudioTest_Main.unity",
            "Assets/Editor/Audio",
            "Assets/Tests/EditMode/Audio",
            "Assets/Tests/PlayMode/Audio",
            "Assets/Audio/TestGenerated"
        };

        for (int i = assets.Count - 1; i >= 0; i--)
        {
            if (!File.Exists(assets[i]) && !Directory.Exists(assets[i]))
            {
                Debug.LogWarning("SoundManager export skipped missing asset: " + assets[i]);
                assets.RemoveAt(i);
            }
        }

        AssetDatabase.ExportPackage(assets.ToArray(), PackagePath, ExportPackageOptions.Recurse);
        Debug.Log("SoundManager package exported: " + PackagePath);
    }
}
