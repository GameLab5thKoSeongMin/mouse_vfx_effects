// 코드 책임: MouseMagicFx 패키지에 허용된 에셋만 포함해 UnityPackage로 내보낸다.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MouseMagicFxPackageExporter
{
    private const string PackagePath = "Exports/MouseMagicFx_v0.17.2.unitypackage";

    [MenuItem("Tools/Mouse Magic FX/Export Package")]
    public static void ExportPackage()
    {
        MouseMagicFxTestSceneBuilder.CreateAll();

        Directory.CreateDirectory("Exports");

        List<string> assets = new List<string>
        {
            "Assets/Scripts/UI/Effects/MouseMagicFxController.cs",
            "Assets/Scripts/UI/Effects/MouseMagicFxDemoControls.cs",
            "Assets/Scripts/UI/Effects/MouseMagicFx_SETUP.md",
            "Assets/Scripts/UI/Effects/MouseMagicFx_EXPORT_MANIFEST.md",
            "Assets/Prefabs/UI/Effects/MouseFxRoot.prefab",
            "Assets/Scenes/MouseMagicFx_TestScene.unity",
            "Assets/Editor/MouseMagicFxTestSceneBuilder.cs",
            "Assets/Editor/MouseMagicFxPackageExporter.cs"
        };

        for (int i = assets.Count - 1; i >= 0; i--)
        {
            if (!File.Exists(assets[i]) && !Directory.Exists(assets[i]))
            {
                Debug.LogWarning("Mouse Magic FX export skipped missing asset: " + assets[i]);
                assets.RemoveAt(i);
            }
        }

        AssetDatabase.ExportPackage(assets.ToArray(), PackagePath, ExportPackageOptions.Default);
        Debug.Log("Mouse Magic FX package exported: " + PackagePath);
    }
}
