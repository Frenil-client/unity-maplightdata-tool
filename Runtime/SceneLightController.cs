using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬에 배치하는 조명 적용 컴포넌트.
    ///
    /// 사용 방식:
    /// - Primary 씬: Awake 시 해당 씬의 MapLightData를 자동 적용
    /// - Additive 씬: 로드/언로드 시점에 맞춰 조명을 교체하거나 복원
    /// - 런타임 교체: ApplyLightData()를 직접 호출 (낮/밤 전환 등)
    /// </summary>
    public class SceneLightController : MonoBehaviour
    {
        [Header("Light Data")]
        [Tooltip("이 씬에서 사용할 MapLightData 에셋")]
        public MapLightData lightData;

        [Header("Additive Scene")]
        [Tooltip("Additive 씬 여부. true이면 언로드 시 previousLightData로 복원합니다.")]
        public bool isAdditiveScene = false;

        [Tooltip("Additive 씬 언로드 후 복원할 조명 데이터 (비워두면 복원하지 않음)")]
        public MapLightData previousLightData;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (lightData == null)
            {
                Debug.LogWarning($"[SceneLightController] lightData가 비어 있습니다. ({gameObject.scene.name})");
                return;
            }

            lightData.ApplyMapLightData();
        }

        private void OnDestroy()
        {
            // Additive 씬이 언로드될 때 이전 조명으로 복원
            if (isAdditiveScene && previousLightData != null)
            {
                // 씬이 언로드된 이후에도 RenderSettings 복원은 유효
                previousLightData.ApplyMapLightData();
            }
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// 런타임 중 조명을 교체합니다. (낮/밤 전환 등)
        /// </summary>
        public void ApplyLightData(MapLightData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SceneLightController] ApplyLightData: data가 null입니다.");
                return;
            }

            lightData = data;
            data.ApplyMapLightData();
        }

        /// <summary>
        /// 현재 씬에 배치된 SceneLightController를 찾아 조명을 교체합니다.
        /// </summary>
        public static void ApplyToScene(Scene scene, MapLightData data)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<SceneLightController>();
                if (controller != null)
                {
                    controller.ApplyLightData(data);
                    return;
                }
            }

            Debug.LogWarning($"[SceneLightController] '{scene.name}'에서 SceneLightController를 찾을 수 없습니다.");
        }
    }
}
