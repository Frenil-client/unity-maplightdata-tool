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
    public class SceneInvalidCheckTool : EditorWindow
    {
        private readonly string _battleSceneDirectory = "Assets/Game/RemoteResources/Scene";
        private readonly string _mapLightDataDirectory =
            "Assets/Game/RemoteResources/Scene/ScriptableObject/MapLightData";


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
                {
                    continue;
                }

                RegisterPrefabs(scenePath, Path.GetFileNameWithoutExtension(scenePath), "BG", sceneName);

                var scriptableObjectPath = Path.Combine(_mapLightDataDirectory, sceneName + ".asset");
                EditorSceneManager.OpenScene(scenePath);

                var asset = CreateInstance<MapLightData>();
                asset.SetRenderSetting();
                AssetDatabase.CreateAsset(asset, scriptableObjectPath);
                AssetDatabase.SaveAssets();
                RegisterPrefabs(scriptableObjectPath, scriptableObjectPath, "MapLightData");

                Debug.Log($"Created MapLightData: {scriptableObjectPath}");
            }

            EditorSceneManager.OpenScene(startScene);
            Debug.Log("Check And Create MapLightData Complete");
        }

        public static void RegisterPrefabs(string assetPath, string address, string groupName, string label = "")
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings를 찾을 수 없습니다.");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null);
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"GUID를 찾을 수 없습니다: {assetPath}");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            if (!string.IsNullOrEmpty(label))
            {
                entry.SetLabel(label, true, true);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif
