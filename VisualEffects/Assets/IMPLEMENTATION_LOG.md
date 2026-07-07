# Implementation Log

## v0.17.0 - UI Mouse Magic FX: Golden Sparkle Trail & Click Ring Burst

- Added a reusable UI-based `MouseMagicFxController` for subtle golden cursor dust and click burst effects.
- Implemented object-pooled UI `Image` particles with `raycastTarget = false`.
- Added distance-threshold trail spawning, unscaled-time animation, max active particle protection, and left-click burst triggering.
- Added fallback dot and ring sprites generated once at startup when custom art is not assigned.
- Added setup documentation because the current `SampleScene.unity` does not contain a production UI Canvas to safely wire.

## v0.17.1 - MouseMagicFx Test Scene, Prefab, Export Stability Check

- Added editor tooling to generate a reusable `MouseFxRoot` prefab and a Play Mode UI test scene from the Unity menu.
- Added `MouseMagicFxDemoControls` for demo button click counting and FX enable/disable toggling.
- Added a package exporter menu item scoped to approved MouseMagicFx package assets.
- Expanded setup documentation with test scene, prefab, tuning, troubleshooting, and main-project import notes.
- Added an export manifest that separates required, recommended, optional test/demo, optional exporter, and excluded files.
- Unity batchmode did not execute the menu generation in this environment, so the scene, prefab, and package should be generated from the Unity Editor menu before visual QA or distribution.

## v0.17.2 - Input System Compatibility Fix for MouseMagicFx Scene, Prefab, and Runtime

- Updated `MouseMagicFxController` pointer helpers to prefer `UnityEngine.InputSystem.Mouse.current` when the new Input System is enabled and to call legacy `UnityEngine.Input` only inside `ENABLE_LEGACY_INPUT_MANAGER` blocks.
- Updated the test scene builder so generated `EventSystem` objects use `InputSystemUIInputModule` in new Input System projects and `StandaloneInputModule` only in legacy input projects.
- Repaired the existing generated test scene directly; it now uses `InputSystemUIInputModule` and no longer contains `StandaloneInputModule`.
- Inspected the existing `MouseFxRoot` prefab; it contains no `EventSystem`, `StandaloneInputModule`, or `InputSystemUIInputModule`.
- Updated setup and export documentation with the `UnityEngine.Input` InvalidOperationException troubleshooting path.
- Fixed FX render ordering so `MouseFxRoot` remains the last Canvas child and renders above the demo document panel.
