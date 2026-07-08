# SoundManager Setup

## 설계 요약

`SoundManager`는 `DontDestroyOnLoad` 싱글톤으로 유지되는 오디오 관리자입니다. 씬별 루프 사운드는 `SceneLoopEntry` 매핑 테이블에서 현재 씬 이름을 찾아 하나의 `loopSource`로 재생합니다. SFX는 `SoundEntry.id` 문자열 기반 라이브러리로 찾고, 미리 만든 `AudioSource` 풀에서 재생합니다.

## 폴더 구조

```text
Assets/
  Audio/
    BGM/
    SFX/
    TestGenerated/
  Prefabs/
    Audio/
      SoundManager.prefab
  Scenes/
    AudioTest_Bootstrap.unity
    AudioTest_Title.unity
    AudioTest_Main.unity
  Scripts/
    Audio/
      Audio.Runtime.asmdef
      SceneLoopEntry.cs
      SoundManager.cs
      SoundEntry.cs
      UIButtonSound.cs
      ObjectClickSound.cs
      SoundManagerDebugPanel.cs
      SoundManager_SETUP.md
  Editor/
    Audio/
      SoundManagerTestSceneBuilder.cs
      SoundManagerPackageExporter.cs
      SoundManagerValidator.cs
  Tests/
    EditMode/
      Audio/
    PlayMode/
      Audio/
```

## 생성 메뉴

Unity Editor에서 다음 메뉴를 실행합니다.

```text
Tools/Audio/Create Test Audio Clips
Tools/Audio/Create SoundManager Prefab
Tools/Audio/Create SoundManager Test Scenes
Tools/Audio/Run SoundManager Validation
Tools/Audio/Export SoundManager Package
```

`Create SoundManager Test Scenes`는 테스트 씬을 만들고 Build Settings에도 등록합니다. `Export SoundManager Package`는 필요한 에셋을 `Exports/SoundManager.unitypackage`로 내보냅니다.

## Prefab 세팅

`Assets/Prefabs/Audio/SoundManager.prefab`에 다음 값을 연결합니다.

- `Loop Source`: 같은 GameObject의 `AudioSource`
- `Scene Loop Entries`: 씬 이름과 loop clip 매핑
  - `AudioTest_Title` -> 테스트 title 루프
  - `AudioTest_Main` -> 테스트 main 루프
  - `title` -> 실제 title 루프
  - `main` -> 실제 main 루프
- `Stop Loop When Scene Has No Entry`: 매핑 없는 씬에 들어가면 이전 loop를 정지
- `Sfx Library`: `button_click`, `object_click`, `paper_open`, `paper_close`, `page_flip`, `approve`, `reject`, `stamp`, `warning` 등
- `Initial Sfx Pool Size`: 기본 8
- `Max Sfx Pool Size`: 기본 24
- `Allow Pool Expansion`: 기본 true

## SFX 추가

새 효과음은 `SoundManager` 코드를 수정하지 않고 prefab의 `Sfx Library`에 `SoundEntry`를 추가합니다. `id`는 `page_flip` 같은 문자열 key이고, `clip`, `volume`, `pitchRange`를 Inspector에서 설정합니다. 중복 ID는 런타임과 Validator에서 warning으로 표시됩니다.

## Scene Loop 추가

새 씬 BGM/loop는 `SoundManager` 코드를 수정하지 않고 prefab의 `Scene Loop Entries`에 항목을 추가합니다. `sceneName`에는 Unity 씬 이름을 정확히 입력하고, `loopClip`과 scene별 `volume`을 연결합니다. 중복 sceneName은 첫 번째 항목만 사용되고 warning이 출력됩니다.

## ObjectClickSound 조건

`IPointerClickHandler` 경로는 `EventSystem`이 필요합니다. UI 오브젝트는 `GraphicRaycaster`, 3D 오브젝트는 카메라의 `PhysicsRaycaster`와 클릭 대상의 `Collider`가 필요합니다. 단순 테스트를 위해 `OnMouseDown` fallback 옵션도 포함되어 있습니다.

## PlayerPrefs

저장 key는 다음 세 개입니다.

```text
Sound.MasterVolume
Sound.LoopVolume
Sound.SfxVolume
```

`PlayerPrefs`는 사용자 설정 저장용입니다. 게임 진행 데이터 저장용으로 쓰지 않습니다.

## Export 대상

필수 포함:

```text
Assets/Scripts/Audio/
Assets/Prefabs/Audio/SoundManager.prefab
Assets/Audio/BGM/
Assets/Audio/SFX/
```

선택 포함:

```text
Assets/Editor/Audio/
Assets/Tests/EditMode/Audio/
Assets/Tests/PlayMode/Audio/
Assets/Scenes/AudioTest_*.unity
Assets/Audio/TestGenerated/
```

실제 프로젝트로 가져간 뒤에는 `SoundManager.prefab`을 시작 씬에 1개 배치하고, 실제 BGM/SFX AudioClip을 Inspector에 연결합니다.

## AudioMixer 확장

추후 AudioMixer를 붙일 경우 `SoundManager`의 `loopSource`와 `CreateSfxSource`에서 만든 SFX `AudioSource.outputAudioMixerGroup`을 각각 Loop/SFX 그룹에 연결하도록 필드를 추가하면 됩니다.

## 검증 체크리스트

1. `AudioTest_Bootstrap`에서 시작하면 `SoundManager` 인스턴스가 1개다.
2. `AudioTest_Title`로 이동하면 title 루프가 재생된다.
3. `AudioTest_Main`으로 이동하면 main 루프가 재생된다.
4. title/main 반복 이동 후에도 인스턴스가 1개다.
5. 같은 루프 clip은 다시 처음부터 재생되지 않는다.
6. SFX 버튼과 오브젝트 클릭 SFX가 재생된다.
7. 빠른 SFX 연속 재생 중에도 루프 사운드는 유지된다.
8. pool 부족 시 확장되거나 warning이 출력된다.
9. null clip 또는 없는 SFX ID는 warning만 출력한다.
10. Master/Loop/SFX 볼륨 저장과 불러오기가 동작한다.
