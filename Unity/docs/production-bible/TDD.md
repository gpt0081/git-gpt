# TDD

- 엔진: Unity 6.3 LTS 6000.3.19f1.
- 렌더링: 내장 렌더 파이프라인 호환 Standard Shader 우선.
- 씬: 최초 임포트 시 Editor 스크립트가 `Assets/Scenes/Fjordfall.unity` 생성.
- 런타임: `FjordfallGame`이 게임 상태를 소유하고 `FjordfallExternalRuntime`이 시각 자산을 교체한다.
- 입력: 레거시 Mouse/Touch 입력, 단일 터치.
- 외부 자산: Resources 기반 OBJ/MTL/PNG. 누락 시 절차형 fallback.
- 텍스처 상한: 256px, mipmap 및 압축 사용.
- Git 체크아웃: 모델과 텍스처 생성기가 누락된 소스 자산을 자동 생성한다.
- 빌드: `Fjordfall > 2. Android APK 빌드`, 결과 `Builds/Fjordfall-Unity3D.apk`.
- 계측: 전투 시간, 생존 병력, 승패를 결과 화면에 표시.
