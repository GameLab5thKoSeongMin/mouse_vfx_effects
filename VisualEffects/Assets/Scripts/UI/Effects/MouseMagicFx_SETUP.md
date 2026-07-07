# Mouse Magic FX Setup

`MouseMagicFxController` is a reusable UI mouse effect for a golden sparkle dust trail and a refined click ring burst. It uses pooled UI `Image` objects under a `RectTransform`, not a world-space `ParticleSystem`.

## Test Scene

The standalone VFX project includes editor tooling for a Play Mode test scene:

```text
Tools/Mouse Magic FX/Create Test Scene
```

This creates or regenerates:

```text
Assets/Scenes/MouseMagicFx_TestScene.unity
```

Open that scene and press Play. Move the mouse to see the trail, left click to see the ring burst, and click the two demo buttons to confirm the FX layer does not block UI input.

If the scene has not been generated yet, use the menu above. Batchmode execution can also call:

```text
MouseMagicFxTestSceneBuilder.CreateTestScene
```

## Prefab

The reusable prefab can be generated with:

```text
Tools/Mouse Magic FX/Create MouseFxRoot Prefab
```

Expected output:

```text
Assets/Prefabs/UI/Effects/MouseFxRoot.prefab
```

The prefab contains only:

```text
MouseFxRoot
  MouseMagicFxController
```

It does not include demo labels, buttons, or scene-only content.

## Manual Scene Setup

Create the effect root under the target UI Canvas:

```text
Canvas
  Existing UI
  MouseFxRoot
    MouseMagicFxController
```

Recommended `MouseFxRoot` settings:

- Add a `RectTransform` stretched to the full Canvas.
- Put it as the last child of the Canvas so it renders above normal UI.
- Add `MouseMagicFxController` to `MouseFxRoot`.
- Do not add `GraphicRaycaster` to `MouseFxRoot`.
- Keep `Keep Root On Top` enabled unless another dedicated cursor layer must render above it.

The generated particle `Image` objects set `raycastTarget = false`, so they do not block UI buttons, document dragging, or pointer checks.

## Canvas Modes

The controller supports:

- `Screen Space - Overlay`
- `Screen Space - Camera`

For `Screen Space - Camera`, assign the Canvas world camera normally. The controller resolves it from the parent Canvas and converts screen coordinates through `RectTransformUtility.ScreenPointToLocalPointInRectangle`.

## Optional Sprites

Custom art is not required.

- `Dust Sprite`: optional soft dot or small sparkle texture.
- `Ring Sprite`: optional thin white ring texture.

If either reference is empty, the controller creates a tiny fallback dot and ring texture once during `Awake`.

## Inspector Tuning

- Make trail more subtle: raise `Trail Spawn Distance`, lower `Trail Scale Range`, or reduce alpha in `Trail Colors`.
- Make trail stronger: lower `Trail Spawn Distance` toward `8`, raise `Max Trail Spawns Per Frame` to `2`, or increase `Trail Lifetime Range` slightly.
- Make click burst stronger: increase `Burst Count`, `Burst Speed Range`, or `Ring End Scale`.
- Reduce particle count: lower `Max Active Particles`, lower `Burst Count`, or raise `Trail Spawn Distance`.
- Fade faster: lower `Trail Lifetime Range`, `Burst Lifetime Range`, or `Ring Lifetime`.
- Disable effect: call `SetEnabled(false)`.

Manual click bursts can be triggered with:

```csharp
mouseMagicFxController.PlayClickBurst(screenPosition);
```

## Troubleshooting

InvalidOperationException: You are trying to read Input using UnityEngine.Input:

- Cause: the project is set to the new Input System only, but a runtime script or UI `EventSystem` is still using old `UnityEngine.Input` or `StandaloneInputModule`.
- Runtime fix: `MouseMagicFxController` uses guarded input backend handling. It reads `UnityEngine.InputSystem.Mouse.current` when `ENABLE_INPUT_SYSTEM` is available and calls legacy `UnityEngine.Input` only inside `ENABLE_LEGACY_INPUT_MANAGER` blocks.
- Scene fix: in new Input System projects, the scene `EventSystem` must use `InputSystemUIInputModule`, not `StandaloneInputModule`.
- Prefab rule: `MouseFxRoot.prefab` should not contain an `EventSystem`, `StandaloneInputModule`, or `InputSystemUIInputModule`. EventSystem ownership belongs to each scene.
- Existing generated scene: regenerate it with `Tools/Mouse Magic FX/Create Test Scene` if it still contains `StandaloneInputModule`.
- Existing generated prefab: regenerate it with `Tools/Mouse Magic FX/Create MouseFxRoot Prefab` if it contains anything other than `MouseFxRoot` plus `MouseMagicFxController`.
- Do not use `Active Input Handling = Both` as the primary fix. The package should adapt to the host project's input backend.

Effect is not visible:

- Confirm `MouseFxRoot` is under the active Canvas.
- Confirm `MouseFxRoot` is the last Canvas child.
- Confirm `SetEnabled(false)` was not called.
- Confirm the mouse is moving farther than `Trail Spawn Distance`.

Effect appears behind UI:

- Move `MouseFxRoot` to the bottom of the Canvas hierarchy so it renders last.
- Confirm `Keep Root On Top` is enabled on `MouseMagicFxController`.
- Check sibling order if the Canvas has multiple overlay roots.

Buttons are not clickable:

- Confirm `MouseFxRoot` has no `GraphicRaycaster`.
- Confirm generated particle images keep `raycastTarget = false`.
- Check for other full-screen UI panels blocking input.

Particles are too strong:

- Lower color alpha values.
- Raise `Trail Spawn Distance`.
- Lower `Burst Count`.
- Lower `Ring End Scale`.

No Canvas exists:

- Create a normal Unity UI Canvas first.
- Add `MouseFxRoot` as a child of that Canvas.

Screen Space Camera canvas position is wrong:

- Assign the Canvas world camera.
- Confirm the camera renders the UI layer.
- Keep `MouseFxRoot` stretched to the Canvas bounds.

## Main Game Import Notes

- Import required files from `MouseMagicFx_EXPORT_MANIFEST.md`.
- Import the prefab if it has been generated.
- Do not import `Assets/IMPLEMENTATION_LOG.md`.
- Do not overwrite the main project's `ProjectSettings`.
- Place `MouseFxRoot` under the target UI Canvas and tune values in context with real document UI.
