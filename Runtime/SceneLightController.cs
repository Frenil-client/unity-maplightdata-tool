using UnityEngine;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬에 배치하는 조명 등록 컴포넌트.
    ///
    /// 핵심 설계:
    /// - Awake 시 MapLightManager에 조명을 등록
    /// - SceneType 결정 우선순위:
    ///   1. MapLightManager에 예약된 타입 (ReserveNextSceneType) - 씬 로드 전 외부에서 지정
    ///   2. Inspector에서 직접 설정한 sceneType
    /// - OnDestroy 시 Additive이면 Manager에서 Pop -> 이전 조명 자동 복원
    /// - 런타임 교체는 ApplyOverride()를 통해 Manager에 위임
    /// </summary>
    public class SceneLightController : MonoBehaviour
    {
        /// <summary>씬 로드 방식. Primary: 메인 씬 / Additive: 추가 로드 씬</summary>
        public enum SceneType { Primary, Additive }

        // -----------------------------------------------------------------------

        [Tooltip("씬 로드 방식. 씬 로드 전 MapLightManager.ReserveNextSceneType()으로 예약하면 이 값보다 우선 적용됩니다.")]
        public SceneType sceneType = SceneType.Primary;

        [Tooltip("이 씬에서 사용할 MapLightData 에셋")]
        public MapLightData lightData;

        /// <summary>Awake에서 최종 결정된 SceneType. OnDestroy에서 등록 해제에 사용됩니다.</summary>
        private SceneType _resolvedType;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (lightData == null)
            {
                Debug.LogWarning($"[SceneLightController] lightData가 비어 있습니다. ({gameObject.scene.name})");
                return;
            }

            // 예약된 SceneType이 있으면 우선 적용, 없으면 Inspector 설정 사용
            var reserved = MapLightManager.Instance.ConsumeReservedType(gameObject.scene.name);
            _resolvedType = reserved ?? sceneType;

            if (_resolvedType == SceneType.Additive)
                MapLightManager.Instance.PushAdditive(lightData);
            else
                MapLightManager.Instance.RegisterPrimary(lightData);
        }

        private void OnDestroy()
        {
            // Awake에서 결정된 타입 기준으로 해제
            if (_resolvedType == SceneType.Additive)
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
