# Unity MapLightData Tool

씬별 조명 환경을 ScriptableObject로 캡처하고, 씬 전환 및 Additive 씬 로드/언로드에 따라
조명을 자동으로 적용하고 복원하는 시스템입니다.

## 개요

Unity의 `RenderSettings`(Skybox, Ambient, Fog, Flare 등)를 `MapLightData`(ScriptableObject)로
저장하고, `MapLightManager`가 Primary / Additive 씬 스택을 중앙에서 관리합니다.

각 씬에 배치된 `SceneLightController`가 Awake / OnDestroy 시점에 Manager에
자동으로 등록/해제하며, Primary인지 Additive인지는 `SceneManager.GetActiveScene()`과
비교해 자동으로 판단합니다.

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

### 2. SceneLightController - Primary / Additive 자동 판단

씬에 하나만 배치하면 되며, `lightData`만 지정하면 됩니다.
Primary / Additive 구분은 `SceneManager.GetActiveScene()` 비교로 자동 판단합니다.

```csharp
// Awake
_isAdditive = gameObject.scene != SceneManager.GetActiveScene();

if (_isAdditive)
    MapLightManager.Instance.PushAdditive(lightData);
else
    MapLightManager.Instance.RegisterPrimary(lightData);

// OnDestroy (Additive 씬 언로드 시)
if (_isAdditive)
    MapLightManager.Instance.PopAdditive(lightData);
// -> Manager가 스택 이전 조명으로 자동 복원
```

### 3. MapLightData - RenderSettings 전체 캡처

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
버튼 하나로 씬 디렉토리 전체를 순회하며 아래 작업을 자동 수행합니다.

```
1. 지정 디렉토리에서 씬 목록 수집
2. 씬을 열어 RenderSettings 읽기 (SetRenderSetting)
3. MapLightData 에셋 생성 및 저장
4. Addressables 그룹에 씬 + 에셋 등록
5. 작업 완료 후 원래 씬으로 복귀
```

## 사용 방법

### 1. 씬 설정

각 씬(Primary / Additive 모두)의 루트 오브젝트에 `SceneLightController`를 붙이고
`lightData`를 지정합니다. Primary / Additive 판단은 자동입니다.

```
[SceneLightController]
  lightData : ForestDay (MapLightData 에셋)
```

### 2. Additive 씬 흐름 예시

```
[로드 순서]
1. LobbyScene (Primary)  -> RegisterPrimary(LobbyLight)   적용: LobbyLight
2. DungeonScene (Additive) -> PushAdditive(DungeonLight)  적용: DungeonLight
3. DungeonScene 언로드   -> PopAdditive(DungeonLight)     적용: LobbyLight  (자동 복원)
```

### 3. 런타임 중 교체 (낮/밤 전환 등)

```csharp
// SceneLightController를 통한 교체
sceneLightController.ApplyOverride(nightLightData);

// 또는 Manager 직접 호출
MapLightManager.Instance.ApplyOverride(nightLightData);
```

### 4. 에디터 - 에셋 일괄 생성

1. `Tools > MapLightData > Scene Variable Editor` 메뉴 클릭
2. `Update BG Addressable` 버튼 클릭
3. 지정 디렉토리의 씬을 순회하며 MapLightData 에셋 자동 생성 및 Addressables 등록

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
│   └─ Current                   현재 적용 중인 조명 (읽기 전용)
└─ SceneLightController.cs
    ├─ Awake()                   씬 타입 자동 판단 후 Manager에 등록
    ├─ OnDestroy()               Additive 씬 언로드 시 Manager에서 Pop
    └─ ApplyOverride()           런타임 중 조명 교체 요청
Editor/
└─ SceneInvalidCheckTool.cs
    ├─ ShowWindow()              EditorWindow 열기
    ├─ UpdateBGAddressable()     씬 순회 -> 에셋 생성 -> Addressables 등록
    └─ RegisterPrefabs()         Addressables 그룹 생성 및 에셋 등록
```

## 요구 사항

- Unity 2021.2+ (C# 9.0)
- Addressables 패키지
