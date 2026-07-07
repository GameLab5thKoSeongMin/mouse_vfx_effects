# Mouse Magic FX Export Manifest

This manifest describes the reusable UI VFX package for `MouseMagicFxController`.

## Required export files

Export these files into the main project:

```text
Assets/Scripts/UI/Effects/MouseMagicFxController.cs
Assets/Scripts/UI/Effects/MouseMagicFx_SETUP.md
Assets/Scripts/UI/Effects/MouseMagicFx_EXPORT_MANIFEST.md
```

When exporting through Unity, include the corresponding `.meta` files automatically by using Unity's package exporter.

## Recommended export files

Export these if the prefab has been generated:

```text
Assets/Prefabs/UI/Effects/MouseFxRoot.prefab
Assets/Prefabs/UI/Effects/MouseFxRoot.prefab.meta
```

The prefab contains only the reusable full-screen `MouseFxRoot` with `MouseMagicFxController`.

## Optional test/demo files

These are useful for validating the package in this standalone project, but they are not required in the main game project:

```text
Assets/Scenes/MouseMagicFx_TestScene.unity
Assets/Scenes/MouseMagicFx_TestScene.unity.meta
Assets/Scripts/UI/Effects/MouseMagicFxDemoControls.cs
Assets/Scripts/UI/Effects/MouseMagicFxDemoControls.cs.meta
Assets/Editor/MouseMagicFxTestSceneBuilder.cs
Assets/Editor/MouseMagicFxTestSceneBuilder.cs.meta
```

## Optional export utility files

These are optional editor-only helpers:

```text
Assets/Editor/MouseMagicFxPackageExporter.cs
Assets/Editor/MouseMagicFxPackageExporter.cs.meta
```

## Do not export into the main project

Exclude these paths:

```text
Assets/IMPLEMENTATION_LOG.md
ProjectSettings/
Library/
Temp/
Logs/
UserSettings/
obj/
.vs/
```

## Main project import checklist

1. Import required files.
2. Import `MouseFxRoot.prefab` if available.
3. Place `MouseFxRoot` under the target Canvas.
4. Confirm Canvas render mode.
5. Confirm the FX layer is above normal UI.
6. Confirm UI buttons still receive clicks.
7. Tune default values.
8. Run Play Mode test.
9. Do not overwrite the main project's `IMPLEMENTATION_LOG.md`.

## Input System compatibility checklist

- In new Input System projects, use `InputSystemUIInputModule` on scene `EventSystem` objects.
- In old Input Manager projects, use `StandaloneInputModule`.
- If both input backends are enabled, prefer `InputSystemUIInputModule`.
- Do not put any input module or `EventSystem` inside `MouseFxRoot.prefab`.
- If the test scene throws `InvalidOperationException: You are trying to read Input using UnityEngine.Input...`, regenerate it through `Tools/Mouse Magic FX/Create Test Scene` or replace `StandaloneInputModule` with `InputSystemUIInputModule`.
- If the prefab contains scene-only input objects, regenerate it through `Tools/Mouse Magic FX/Create MouseFxRoot Prefab`.
- Do not force the main project to switch `Active Input Handling` to `Both`; the package should work with the project's existing input backend.

## Manual UnityPackage export

If the automated exporter cannot run, use Unity's package export UI and select only the required, recommended, and optional test/export utility files listed above. Do not select `ProjectSettings`, `Library`, `Logs`, `Temp`, or `Assets/IMPLEMENTATION_LOG.md`.
