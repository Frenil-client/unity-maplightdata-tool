# Unity MapLightData Tool

씬별 조명 환경을 ScriptableObject로 캡처하고, 씬 전환 및 Additive 씬 로드/언로드에 따라
조명을 자동으로 적용하고 복원하는 시스템입니다.

## 이 저장소에 대하여

실무에서 다룬 씬 조명 저장·복원 문제를 바탕으로 개인이 새로 구현한 공개 예시입니다.
회사 원본 코드가 아닙니다.

## 개요

Unity의 `RenderSettings`(Skybox, Ambient, Fog, Flare 등)를 `MapLightData`(ScriptableObject)로
저장하고, `MapLightManager`가 Primary / Additive 씬 스택을 중앙에서 관리합니다.

각 씬의 `SceneLightController`가 Awake에서 조명을 등록하고, Additive 씬은 OnDestroy에서 해제합니다.
씬 타입은 `ReserveNextSceneType()`으로 예약한 값이 우선이며, 예약이 없으면 Inspector의 `sceneType`을 사용합니다.
활성 씬을 비교해 자동 판별하지는 않습니다.

## 구조

```
Runtime/
├─ MapLightData.cs           씬 조명 데이터 ScriptableObject
├─ MapLightManager.cs        Primary / Additive 스택 중앙 관리 싱글톤
└─ SceneLightController.cs   씬 진입점 컴포넌트 - Manager에 등록/해제
Editor/
└─ SceneInvalidCheckTool.cs  EditorWindow - 에셋 생성 및 Addressables 자동 등록
```

## 핵심 설계

### 1. MapLightManager - 스택 기반 중앙 제어

`DontDestroyOnLoad` 싱글톤으로 씬 전환과 무관하게 상태를 유지합니다.

```
Primary 씬 로드     -> RegisterPrimary(data)  : Primary 슬롯 교체, 스택 없으면 즉시 적용
Additive 씬 로드    -> PushAdditive(data)     : 스택 Push, 최상단 즉시 적용
Additive 씬 언로드  -> PopAdditive(data)      : 스택 Pop, 이전 조명 자동 복원
런타임 교체         -> ApplyOverride(data)    : 현재 슬롯 교체 (낮/밤 전환 등)
```

적용 우선순위: **Additive 스택 최상단 > Primary**

### 2. SceneLightController - SceneType 결정 우선순위

씬에 하나만 배치하면 되며, SceneType은 아래 우선순위로 결정됩니다.

```
1순위: MapLightManager.ReserveNextSceneType()으로 씬 로드 전 예약된 타입
2순위: Inspector에서 직접 설정한 sceneType (기본값: Primary)
```

```csharp
// Awake
var reserved = MapLightManager.Instance.ConsumeReservedType(gameObject.scene.name);
_resolvedType = reserved ?? sceneType;  // 예약 있으면 우선 사용

if (_resolvedType == SceneType.Additive)
    MapLightManager.Instance.PushAdditive(lightData);
else
    MapLightManager.Instance.RegisterPrimary(lightData);

// OnDestroy - Awake에서 결정된 타입 기준으로 해제
if (_resolvedType == SceneType.Additive)
    MapLightManager.Instance.PopAdditive(lightData);
```

### 3. MapLightData - 지원하는 RenderSettings 항목 캡처

캡처 항목:

- Environment: Skybox / Sun Source / Shadow Color / Ambient Mode
- Ambient Lighting: Sky Color / Equator Color / Ground Color (Trilight)
- Reflections: Reflection Bounces / Intensity Multiplier
- Other: Fog / Fog Color / Fog Mode / Density / Start / End / Flare

변경된 값만 갱신해 불필요한 RenderSettings 갱신을 방지합니다.

```csharp
if (RenderSettings.skybox != skybox)
    RenderSettings.skybox = skybox;
```

중복 적용도 `CurrentMapLightData` 비교로 차단합니다.

```csharp
public void ApplyMapLightData()
{
    if (CurrentMapLightData == this) return;
    CurrentMapLightData = this;
    // ...
}
```

### 4. SceneInvalidCheckTool - 에디터 자동화

`Tools > MapLightData > Scene Variable Editor` 메뉴로 EditorWindow를 열고,
버튼으로 지정 경로의 `BG_` 접두사 씬을 순회하며 아래 작업을 수행합니다.
현재 입력 경로는 `Assets/Game/RemoteResources/Scene`, 출력 경로는 그 아래
`ScriptableObject/MapLightData`로 코드에 고정되어 있습니다. 다른 프로젝트에서는 경로와 필터를 수정해야 합니다.

```
1. 지정 디렉토리에서 씬 목록 수집
2. 씬을 열어 RenderSettings 읽기 (SetRenderSetting)
3. MapLightData 에셋 생성 및 저장
4. Addressables 그룹에 씬 + 에셋 등록
5. 작업 완료 후 원래 씬으로 복귀
```

## 사용 방법

### 1. 씬 설정

각 씬의 루트 오브젝트에 `SceneLightController`를 붙이고 `lightData`를 지정합니다.
`sceneType`은 기본값(Primary)으로 두거나, 씬 로드 전 `ReserveNextSceneType()`으로 외부에서 지정합니다.

```
[SceneLightController]
  sceneType : Primary   // 예약이 없을 때의 기본값
  lightData : ForestDay
```

### 2. 씬 전환 - ReserveNextSceneType

Inspector 설정과 다른 타입으로 로드할 때는 `ReserveNextSceneType()`을 먼저 호출합니다.
예약을 생략하면 컨트롤러에 저장된 `sceneType`을 사용합니다.
씬이 로드되면 `SceneLightController.Awake`에서 예약 타입을 자동으로 소비합니다.

```csharp
// Primary 씬 전환
MapLightManager.Instance.ReserveNextSceneType("LobbyScene", SceneType.Primary);
SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);

// Additive 씬 로드
MapLightManager.Instance.ReserveNextSceneType("DungeonScene", SceneType.Additive);
SceneManager.LoadScene("DungeonScene", LoadSceneMode.Additive);

// Additive 씬 언로드 - 별도 예약 불필요
// SceneLightController.OnDestroy -> PopAdditive -> 이전 조명 자동 복원
SceneManager.UnloadSceneAsync("DungeonScene");
```

### 3. Additive 씬 흐름 예시

```
[1] LobbyScene (Primary) 로드
    ReserveNextSceneType("LobbyScene", Primary)
    -> RegisterPrimary(LobbyLight)      적용: LobbyLight

[2] DungeonScene (Additive) 로드
    ReserveNextSceneType("DungeonScene", Additive)
    -> PushAdditive(DungeonLight)       적용: DungeonLight

[3] DungeonScene 언로드
    -> PopAdditive(DungeonLight)        적용: LobbyLight  (자동 복원)
```

### 4. 런타임 중 교체 (낮/밤 전환 등)

```csharp
// 현재 적용 중인 최상단 씬의 컨트롤러를 통해 교체합니다.
// 컨트롤러의 lightData와 매니저 슬롯을 함께 갱신합니다.
sceneLightController.ApplyOverride(nightLightData);
```

## 파일 구성

```
Runtime/
├─ MapLightData.cs
│   ├─ EnvironmentLightData      Ambient 색상 3종 묶음 (내부 클래스)
│   ├─ ApplyMapLightData()       전체 RenderSettings 적용 (중복 방지 포함)
│   ├─ ApplyEnvSky()             Skybox / Sun / Shadow / AmbientMode
│   ├─ ApplyEnvLighting()        Ambient 색상 3종
│   ├─ ApplyEnvReflections()     Reflection Bounces
│   ├─ ApplyEnvOtherSettings()   Fog / Flare
│   └─ SetRenderSetting()        현재 RenderSettings -> 에셋 저장 (Editor only)
├─ MapLightManager.cs
│   ├─ RegisterPrimary()         Primary 씬 조명 등록
│   ├─ PushAdditive()            Additive 씬 조명 스택 Push
│   ├─ PopAdditive()             Additive 씬 언로드 시 스택 Pop + 자동 복원
│   ├─ ApplyOverride()           런타임 중 현재 슬롯 교체
│   ├─ ReserveNextSceneType()    씬 로드 전 SceneType 예약
│   ├─ ConsumeReservedType()     SceneLightController.Awake에서 예약 타입 소비
│   └─ Current                   현재 적용 중인 조명 (읽기 전용)
└─ SceneLightController.cs
    ├─ SceneType (enum)          Primary / Additive
    ├─ Awake()                   예약 타입 우선 소비 후 Manager에 등록
    ├─ OnDestroy()               Additive 씬 언로드 시 Manager에서 Pop
    └─ ApplyOverride()           런타임 중 조명 교체 요청
Samples~/MapLightExample/
└─ MapLightExample.cs            씬 전환 / Additive 로드 / 낮밤 전환 사용 예시
Editor/
└─ SceneInvalidCheckTool.cs
    ├─ ShowWindow()              EditorWindow 열기
    ├─ UpdateBGAddressable()     씬 순회 -> 에셋 생성 -> Addressables 등록
    └─ RegisterPrefabs()         Addressables 그룹 생성 및 에셋 등록
```

## 현재 제약

### 조명 교체와 씬 해제

스택은 씬 식별자 대신 `MapLightData` 참조로 항목을 구분합니다.
Additive 씬이 Day로 등록된 뒤 `MapLightManager.ApplyOverride(Night)`를 직접 부르면
컨트롤러에는 Day가 남습니다. 이후 씬이 종료되어 `PopAdditive(Day)`를 호출해도
스택의 Night를 찾지 못해 이전 조명이 복원되지 않을 수 있습니다.

현재 적용 중인 씬은 컨트롤러의 `ApplyOverride`를 사용해야 합니다.
다만 아래쪽 씬을 지정해 교체하거나 여러 씬이 같은 조명 에셋을 공유하는 경우까지 안전하게 처리하는 구조는 아닙니다.
등록 핸들 또는 씬 식별자로 소유권을 구분하고, 교체·해제를 같은 항목에 수행하는 개선이 필요합니다.

### 에디터 일괄 처리

성공 경로에서는 작업 전 활성 씬을 다시 엽니다. 예외 발생 시의 복원과
여러 Additive 씬을 열어 둔 편집 상태 전체의 복원은 구현되어 있지 않습니다.
실행 전 작업을 저장하고, 입력·출력 경로와 기존 에셋을 확인합니다.

## 다음 검증 항목

- 씬 등록 핸들을 도입한 뒤 최상단·중간 씬 해제, 동일 에셋 공유, 낮밤 교체 후 해제를 테스트한다.
- 에디터 경로·접두사를 설정 가능하게 하고, 기존 에셋 갱신과 예외 시 씬 구성 복원을 보강한다.
- 현재 자동 테스트·CI 구성은 없으며, 씬 전환과 RenderSettings 적용은 Unity에서 확인해야 한다.

## 개발 환경

- Unity 2021.2+ (C# 9.0)
- Addressables 패키지 (에디터 자동화)
