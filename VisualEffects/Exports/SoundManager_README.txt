SoundManager Unity Package README
=================================

패키지 파일
-----------
- SoundManager.unitypackage

권장 import 대상
----------------
- Unity 6.x 이상
- uGUI 패키지 활성화
- Audio 모듈 활성화

포함된 런타임 파일
------------------
- Assets/Scripts/Audio/
- Assets/Prefabs/Audio/SoundManager.prefab
- Assets/Audio/BGM/
- Assets/Audio/SFX/

포함된 개발/테스트 파일
-----------------------
- Assets/Editor/Audio/
- Assets/Tests/EditMode/Audio/
- Assets/Tests/PlayMode/Audio/
- Assets/Scenes/AudioTest_Bootstrap.unity
- Assets/Scenes/AudioTest_Title.unity
- Assets/Scenes/AudioTest_Main.unity
- Assets/Audio/TestGenerated/

다른 Unity 프로젝트에서 기본 세팅 방법
--------------------------------------
1. SoundManager.unitypackage를 import한다.
2. Assets/Prefabs/Audio/SoundManager.prefab을 게임의 첫 씬에 1개 배치한다.
3. SoundManager prefab 또는 씬에 배치된 SoundManager 오브젝트를 선택한다.
4. Inspector의 Scene Loop Entries에서 씬 이름과 loop clip을 연결한다.
   - sceneName: title
   - loopClip: title 씬에서 재생할 파도/루프 사운드
   - sceneName: main
   - loopClip: main 씬에서 재생할 BGM
5. Inspector의 Sfx Library에 SFX ID와 AudioClip을 추가한다.
   - button_click
   - object_click
   - paper_open
   - paper_close
   - page_flip
   - approve
   - reject
   - stamp
   - warning
6. SoundManager 인스턴스는 시작 씬에 1개만 둔다.

씬별 loop audio 동작 방식
-------------------------
- SoundManager는 코드에 씬 이름을 하드코딩하지 않는다.
- SceneLoopEntry 목록을 사용해 씬 이름과 loop AudioClip을 매핑한다.
- Unity에서 씬이 로드되면 현재 씬 이름으로 SceneLoopEntry를 찾는다.
- 매칭되는 entry가 있으면 해당 AudioClip을 하나의 loopSource에서 loop 재생한다.
- 매칭되는 entry가 없고 Stop Loop When Scene Has No Entry가 켜져 있으면 loopSource를 정지한다.
- 새 씬 BGM이 필요하면 코드를 수정하지 말고 Inspector에서 Scene Loop Entries 항목만 추가한다.

SFX 동작 방식
-------------
- SFX는 SoundManager.PlaySfx("id") 형태로 문자열 ID 기반 재생을 한다.
- SFX AudioSource는 pool로 관리한다.
- Initial Sfx Pool Size만큼 미리 생성하고, Allow Pool Expansion이 켜져 있으면 Max Sfx Pool Size까지 확장한다.
- 존재하지 않는 SFX ID 또는 null AudioClip은 게임을 멈추지 않고 warning만 출력한다.

유용한 컴포넌트
---------------
- UIButtonSound
  Unity UI Button에 붙여 사용한다.
  기본 SFX ID는 button_click이다.

- ObjectClickSound
  클릭 가능한 일반 오브젝트에 붙여 사용한다.
  기본 SFX ID는 object_click이다.
  IPointerClickHandler 방식은 EventSystem과 GraphicRaycaster 또는 PhysicsRaycaster가 필요하다.
  3D 오브젝트는 Collider도 필요하다.
  간단한 테스트용으로 OnMouseDown fallback 옵션도 포함되어 있다.

- SoundManagerDebugPanel
  테스트 씬에서 SoundManager 상태를 확인하기 위한 디버그 패널이다.
  인스턴스 개수, 현재 loop 상태, SFX pool 상태, 볼륨, 테스트 버튼을 표시한다.

테스트 씬 사용 방법
-------------------
1. Assets/Scenes/AudioTest_Bootstrap.unity를 연다.
2. Play를 누른다.
3. Sound Manager Debug 패널을 사용한다.
4. 다음 버튼을 눌러 확인한다.
   - Play Loop: AudioTest_Title
   - Play Loop: AudioTest_Main
   - Play button_click
   - Play object_click
   - Go To Title Test Scene
   - Go To Main Test Scene

소리가 들리지 않을 때 확인할 것
-------------------------------
- 씬에 활성 AudioListener가 정확히 1개 있는지 확인한다.
- 자동 생성된 AudioTest 씬은 Main Camera에 AudioListener가 붙어 있다.
- Unity Game 뷰의 Mute Audio가 꺼져 있는지 확인한다.
- Master, Loop, SFX volume이 0보다 큰지 확인한다.
- Scene Loop Entries와 Sfx Library에 AudioClip이 연결되어 있는지 확인한다.

Editor 메뉴
-----------
- Tools/Audio/Create Test Audio Clips
- Tools/Audio/Create SoundManager Prefab
- Tools/Audio/Create SoundManager Test Scenes
- Tools/Audio/Run SoundManager Validation
- Tools/Audio/Export SoundManager Package

자동 테스트
-----------
- EditMode 테스트: Assets/Tests/EditMode/Audio/
- PlayMode 테스트: Assets/Tests/PlayMode/Audio/

Export 기준
-----------
- 다른 프로젝트에서 실제로 필요한 필수 파일은 Runtime scripts와 SoundManager.prefab이다.
- Editor, Tests, 테스트 씬, 테스트용 자동 생성 오디오는 개발/검증용 선택 파일이다.
- 실제 게임 프로젝트에서는 BGM/SFX AudioClip을 해당 프로젝트 기준으로 다시 연결하는 것을 권장한다.
