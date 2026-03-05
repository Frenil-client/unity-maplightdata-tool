#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬별 MapLightData 에셋 생성 및 Addressables 자동 등록 EditorWindow.
    ///
    /// 핵심 설계:
    /// - 지정 디렉토리의 씬을 순회하며 RenderSettings를 MapLightData 에셋으로 일괄 캡처
    /// - 생성된 씬 에셋과 MapLightData 에셋을 Addressables 그룹에 자동 등록
    /// - 작업 완료 후 원래 씬으로 복귀
    ///
    /// 메뉴: Tools > MapLightData > Scene Variable Editor
    /// </summary>
    public class SceneInvalidCheckTool : EditorWindow
    {
        /// <summary>씬 에셋을 탐색할 루트 디렉토리</summary>
        private readonly string _battleSceneDirectory = "Assets/Game/RemoteResources/Scene";

        /// <summary>생성된 MapLightData 에셋을 저장할 디렉토리</summary>
        private readonly string _mapLightDataDirectory =
            "Assets/Game/RemoteResources/Scene/ScriptableObject/MapLightData";

        // -----------------------------------------------------------------------

        [MenuItem("Tools/MapLightData/Scene Variable Editor")]
        public static void ShowWindow()
        {
            GetWindow<SceneInvalidCheckTool>("Scene Variable Editor");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Update BG Addressable"))
            {
                UpdateBGAddressable();
            }
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// 씬 디렉토리를 순회하며 MapLightData 에셋을 생성하고 Addressables에 등록합니다.
        ///
        /// 처리 흐름:
        /// 1. _battleSceneDirectory에서 씬 목록 수집
        /// 2. "BG_" 접두사 씬만 필터링
        /// 3. 씬을 열어 RenderSettings -> MapLightData 에셋 생성 및 저장
        /// 4. 씬과 MapLightData 에셋을 각각 Addressables 그룹에 등록
        /// 5. 작업 완료 후 원래 씬으로 복귀
        /// </summary>
        private void UpdateBGAddressable()
        {
            var startScene = SceneManager.GetActiveScene().path;

            var sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { _battleSceneDirectory });
            AssetDatabase.FindAssets("t:asset", new[] { _mapLightDataDirectory });

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);

                if (!sceneName.StartsWith("BG_"))
                    continue;

                // 씬을 Addressables BG 그룹에 등록
                RegisterPrefabs(scenePath, Path.GetFileNameWithoutExtension(scenePath), "BG", sceneName);

                var scriptableObjectPath = Path.Combine(_mapLightDataDirectory, sceneName + ".asset");
                EditorSceneManager.OpenScene(scenePath);

                // 현재 씬 RenderSettings -> MapLightData 에셋 생성
                var asset = CreateInstance<MapLightData>();
                asset.SetRenderSetting();
                AssetDatabase.CreateAsset(asset, scriptableObjectPath);
                AssetDatabase.SaveAssets();

                // MapLightData 에셋을 Addressables MapLightData 그룹에 등록
                RegisterPrefabs(scriptableObjectPath, scriptableObjectPath, "MapLightData");

                Debug.Log($"Created MapLightData: {scriptableObjectPath}");
            }

            EditorSceneManager.OpenScene(startScene);
            Debug.Log("Check And Create MapLightData Complete");
        }

        /// <summary>
        /// 에셋을 Addressables 그룹에 등록합니다.
        /// 그룹이 없으면 자동으로 생성합니다.
        /// </summary>
        /// <param name="assetPath">등록할 에셋의 프로젝트 경로</param>
        /// <param name="address">Addressables 주소 문자열</param>
        /// <param name="groupName">등록할 Addressables 그룹 이름</param>
        /// <param name="label">에셋에 추가할 레이블 (없으면 생략)</param>
        public static void RegisterPrefabs(string assetPath, string address, string groupName, string label = "")
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings를 찾을 수 없습니다.");
                return;
            }

            // 그룹이 없으면 자동 생성
            AddressableAssetGroup group = settings.FindGroup(groupName)
                ?? settings.CreateGroup(groupName, false, false, true, null);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"GUID를 찾을 수 없습니다: {assetPath}");
                return;
            }

            // 에셋 등록 및 주소/레이블 설정
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            if (!string.IsNullOrEmpty(label))
                entry.SetLabel(label, true, true);

            AssetDatabase.SaveAssets();
        }
    }
}

#endif
