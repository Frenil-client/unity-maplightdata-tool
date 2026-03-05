using UnityEngine;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬에 배치하는 조명 등록 컴포넌트.
    ///
    /// 핵심 설계:
    /// - Awake 시 sceneType 기준으로 MapLightManager에 조명을 등록
    /// - OnDestroy 시 Additive 씬이면 Manager에서 Pop -> 이전 조명 자동 복원
    /// - Primary / Additive 구분은 Inspector에서 명시적으로 지정 (SceneType enum)
    /// - 런타임 교체는 ApplyOverride()를 통해 Manager에 위임
    /// </summary>
    public class SceneLightController : MonoBehaviour
    {
        /// <summary>씬 로드 방식. Primary: 메인 씬 / Additive: 추가 로드 씬</summary>
        public enum SceneType { Primary, Additive }

        // -----------------------------------------------------------------------

        [Tooltip("Primary: 메인 씬. Additive: 추가 로드되는 씬.")]
        public SceneType sceneType = SceneType.Primary;

        [Tooltip("이 씬에서 사용할 MapLightData 에셋")]
        public MapLightData lightData;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (lightData == null)
            {
                Debug.LogWarning($"[SceneLightController] lightData가 비어 있습니다. ({gameObject.scene.name})");
                return;
            }

            if (sceneType == SceneType.Additive)
                MapLightManager.Instance.PushAdditive(lightData);
            else
                MapLightManager.Instance.RegisterPrimary(lightData);
        }

        private void OnDestroy()
        {
            // Additive 씬 언로드 시 Manager에서 Pop -> 이전 조명 자동 복원
            if (sceneType == SceneType.Additive)
                MapLightManager.Instance.PopAdditive(lightData);
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// 런타임 중 이 씬의 조명을 교체합니다. (낮/밤 전환 등)
        /// MapLightManager의 현재 슬롯(Primary or Additive 최상단)을 함께 갱신합니다.
        /// </summary>
        public void ApplyOverride(MapLightData data)
        {
            lightData = data;
            MapLightManager.Instance.ApplyOverride(data);
        }
    }
}
