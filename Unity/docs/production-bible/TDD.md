# TDD

- 엔진: Unity 6.3 LTS 6000.3.19f1.
- 렌더링: 내장 렌더 파이프라인 호환 Standard Shader 우선.
- 씬: 최초 임포트 시 Editor 스크립트가 `Assets/Scenes/Fjordfall.unity` 생성.
- 런타임: 외부 프리팹 없이 `FjordfallGame`이 절차적으로 월드와 UI 생성.
- 입력: 레거시 Mouse/Touch 입력. 단일 터치.
- 목표: 가로형 Android 및 데스크톱 에디터.
- 빌드: `Fjordfall > 2. Android APK 빌드`, 결과 `Builds/Fjordfall-Unity3D.apk`.
- 계측: 전투 시간, 생존 병력, 승패를 결과 화면에 표시.
