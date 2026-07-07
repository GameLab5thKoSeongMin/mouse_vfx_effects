MouseMagicFx 사용법
===========================


Import 방법
-----------

1. 대상 Unity 프로젝트를 엽니다.
2. Unity 메뉴에서 Assets > Import Package > Custom Package... 를 선택합니다.
3. MouseMagicFx.unitypackage 파일을 선택합니다.
4. Import 창에서 기본 선택 그대로 Import 합니다.


씬에 적용하는 방법
-----------------

1. 대상 씬의 UI Canvas를 찾습니다.
2. import된 prefab을 찾습니다.

   Assets/Prefabs/UI/Effects/MouseFxRoot.prefab

3. MouseFxRoot.prefab을 Canvas의 child로 넣습니다.
4. MouseFxRoot를 Canvas hierarchy의 맨 아래, 즉 마지막 child로 둡니다.
5. MouseFxRoot에 붙은 MouseMagicFxController에서 Keep Root On Top이 켜져 있는지 확인합니다.

권장 구조:

Canvas
  Existing UI
  MouseFxRoot


Input System 주의
-----------------

대상 프로젝트가 New Input System을 쓰면 씬의 EventSystem은 InputSystemUIInputModule을 사용해야 합니다.

권장 구조:

EventSystem
  InputSystemUIInputModule

StandaloneInputModule이 있으면 New Input System only 프로젝트에서 아래 오류가 날 수 있습니다.

InvalidOperationException: You are trying to read Input using UnityEngine.Input...

이 경우 EventSystem에서 StandaloneInputModule을 제거하고 InputSystemUIInputModule로 바꾸면 됩니다.

MouseFxRoot.prefab 안에는 EventSystem이나 InputModule이 들어가면 안 됩니다.
EventSystem은 각 씬이 소유해야 합니다.


동작 확인
---------

Play Mode에서 다음을 확인합니다.

1. 마우스를 움직이면 금색 sparkle trail이 나옵니다.
2. 좌클릭하면 금색 ring burst가 나옵니다.
3. UI 버튼 클릭이 막히지 않습니다.
4. FX가 UI 뒤에 가리면 MouseFxRoot를 Canvas의 마지막 child로 옮깁니다.
5. MouseMagicFxController의 Keep Root On Top이 켜져 있는지 확인합니다.


튜닝 방법
---------

MouseMagicFxController Inspector에서 조절합니다.

더 은은하게:
- Trail Spawn Distance 증가
- Trail Scale Range 감소
- Trail Colors의 alpha 감소

더 강하게:
- Burst Count 증가
- Ring End Scale 증가
- Burst Speed Range 증가

파티클이 너무 많으면:
- Max Active Particles 감소
- Burst Count 감소
- Trail Spawn Distance 증가

더 빨리 사라지게:
- Trail Lifetime Range 감소
- Burst Lifetime Range 감소
- Ring Lifetime 감소

효과 끄기:

mouseMagicFxController.SetEnabled(false);

수동 클릭 이펙트 재생:

mouseMagicFxController.PlayClickBurst(screenPosition);


가져가지 않아도 되는 것
----------------------

다음 항목은 메인 프로젝트에 넣지 않아도 됩니다.

- ProjectSettings
- Library
- Logs
- Temp
- Assets/IMPLEMENTATION_LOG.md

UnityPackage에는 필요한 runtime script, prefab, setup document, manifest, test scene, editor helper만 들어가 있습니다.


포함된 주요 파일
----------------

- Assets/Scripts/UI/Effects/MouseMagicFxController.cs
- Assets/Scripts/UI/Effects/MouseMagicFxDemoControls.cs
- Assets/Scripts/UI/Effects/MouseMagicFx_SETUP.md
- Assets/Scripts/UI/Effects/MouseMagicFx_EXPORT_MANIFEST.md
- Assets/Prefabs/UI/Effects/MouseFxRoot.prefab
- Assets/Scenes/MouseMagicFx_TestScene.unity
- Assets/Editor/MouseMagicFxTestSceneBuilder.cs
- Assets/Editor/MouseMagicFxPackageExporter.cs
