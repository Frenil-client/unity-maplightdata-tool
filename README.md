# Unity MapLightData Tool

씬별 조명 환경을 ScriptableObject로 캡처하고, Addressables 그룹에 자동 등록하는 에디터 자동화 툴입니다.

## 개요

Unity의 `RenderSettings`(Skybox, Ambient, Fog, Flare 등)를 ScriptableObject 에셋으로 저장해두고,
런타임에 씬 전환 시 `ApplyMapLightData()`를 호출하는 것만으로 조명 환경을 교체할 수 있습니다.

에디터 툴(`SceneInvalidCheckTool`)은 지정 디렉토리의 씬들을 순회하며
`MapLightData` 에셋 생성과 Addressables 등록을 한 번에 자동화합니다.

## 구조

```
Runtime/
├─ MapLightData.cs           씬 조명 데이터 ScriptableObject
└─ SceneLightController.cs   씬 진입점 조명 적용 및 Additive 씬 처리 컴포넌트
Editor/
└─ SceneInvalidCheckTool.cs  EditorWindow - 에셋 생성 및 Addressables 자동 등록
```

## 핵심 설계

### 1. MapLightData - RenderSettings 전체 캡처

Inspector에서 직접 편집하거나, 에디터 메서드 `SetRenderSetting()`으로
현재 씬의 RenderSettings 값을 그대로 읽어와 저장합니다.

```csharp
// 현재 씬 RenderSettings -> ScriptableObject 에셋으로 저장
asset.SetRenderSetting();
AssetDatabase.CreateAsset(asset, path);
```

캡처 항목:

- Environment: Skybox / Sun Source / Shadow Color / Ambient Mode
- Ambient Lighting: Sky Color / Equator Color / Ground Color (Trilight)
- Reflections: Reflection Bounces / Intensity Multiplier
- Other: Fog / Fog Color / Fog Mode / Density / Start / End / Flare

### 2. 중복 적용 방지 - CurrentMapLightData

같은 에셋을 반복 적용할 때 RenderSettings 갱신을 건너뜁니다.

```csharp
public void ApplyMapLightData()
{
    if (CurrentMapLightData == this)
        return;

    CurrentMapLightData = this;
    ApplyEnvSky();
    ApplyEnvLighting();
    ApplyEnvReflections();
    ApplyEnvOtherSettings();
}
```

### 3. 변경된 값만 갱신

각 Apply 메서드는 현재 RenderSettings 값과 비교 후 달라진 항목만 설정합니다.

```csharp
if (RenderSettings.skybox != skybox)
    RenderSettings.skybox = skybox;

if (RenderSettings.fog != fog)
    RenderSettings.fog = fog;
```

### 4. SceneInvalidCheckTool - 에디터 자동화

`Tools > MapLightData > Scene Variable Editor` 메뉴로 EditorWindow를 열고,
버튼 하나로 씬 디렉토리 전체를 순회하며 아래 작업을 자동 수행합니다.

```
1. 지정 디렉토리에서 씬 목록 수집
2. 씬을 열어 RenderSettings 읽기
3. MapLightData 에셋 생성 및 저장
4. Addressables 그룹에 씬 + 에셋 등록
5. 작업 완료 후 원래 씬으로 복귀
```

### 5. RegisterPrefabs - Addressables 등록

```csharp
// 그룹이 없으면 자동 생성
AddressableAssetGroup group = settings.FindGroup(groupName)
    ?? settings.CreateGroup(groupName, false, false, true, null);

// 에셋 등록 및 주소 설정
AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
entry.address = address;
entry.SetLabel(label, true, true);
```

## 사용 방법

### 1. Primary 씬 - 씬 진입 시 자동 적용

씬 루트 오브젝트에 `SceneLightController`를 붙이고 `lightData`를 지정합니다.
씬이 로드되면 `Awake`에서 자동으로 `ApplyMapLightData()`를 호출합니다.

```
[SceneLightController]
  lightData       : ForestDay (MapLightData 에셋)
  isAdditiveScene : false
```

### 2. Additive 씬 - 로드/언로드 시 조명 교체 및 복원

Additive 씬의 `SceneLightController`에서 `isAdditiveScene = true`로 설정하고,
언로드 후 복원할 조명을 `previousLightData`에 지정합니다.

```
[SceneLightController] (Additive 씬)
  lightData        : DungeonInterior (이 씬의 조명)
  isAdditiveScene  : true
  previousLightData: ForestDay      (언로드 후 복원할 조명)
```

- 씬 로드: `Awake`에서 `DungeonInterior` 적용
- 씬 언로드: `OnDestroy`에서 `ForestDay` 자동 복원

### 3. 런타임 중 교체 - 낮/밤 전환 등

```csharp
// 컴포넌트를 직접 참조해 교체
sceneLightController.ApplyLightData(nightLightData);

// 씬 참조로 교체 (컴포넌트를 직접 들고 있지 않을 때)
SceneLightController.ApplyToScene(SceneManager.GetActiveScene(), nightLightData);
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
└─ SceneLightController.cs
    ├─ Awake()                   씬 진입 시 lightData 자동 적용
    ├─ OnDestroy()               Additive 씬 언로드 시 previousLightData 복원
    ├─ ApplyLightData()          런타임 중 조명 교체 (낮/밤 전환 등)
    └─ ApplyToScene() (static)   씬 참조로 SceneLightController 탐색 후 교체
Editor/
└─ SceneInvalidCheckTool.cs
    ├─ ShowWindow()              EditorWindow 열기
    ├─ UpdateBGAddressable()     씬 순회 -> 에셋 생성 -> Addressables 등록
    └─ RegisterPrefabs()         Addressables 그룹 생성 및 에셋 등록
```

## 요구 사항

- Unity 2021.2+ (C# 9.0)
- Addressables 패키지
