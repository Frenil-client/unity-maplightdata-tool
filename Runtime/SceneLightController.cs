using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬에 배치하는 조명 등록 컴포넌트.
    /// MapLightManager에 자신의 조명을 등록하고, 언로드 시 해제합니다.
    ///
    /// Primary / Additive 구분은 컴포넌트가 속한 씬과
    /// SceneManager.GetActiveScene()을 비교해 자동으로 판단합니다.
    /// - Active 씬 == 이 컴포넌트의 씬  ->  Primary 등록
    /// - Active 씬 != 이 컴포넌트의 씬  ->  Additive Push
    /// </summary>
    public class SceneLightController : MonoBehaviour
    {
        [Tooltip("이 씬에서 사용할 MapLightData 에셋")]
        public MapLightData lightData;

        private bool _isAdditive;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (lightData == null)
            {
                Debug.LogWarning($"[SceneLightController] lightData가 비어 있습니다. ({gameObject.scene.name})");
                return;
            }

            // Active 씬과 비교해 Primary / Additive 자동 판단
            _isAdditive = gameObject.scene != SceneManager.GetActiveScene();

            if (_isAdditive)
                MapLightManager.Instance.PushAdditive(lightData);
            else
                MapLightManager.Instance.RegisterPrimary(lightData);
        }

        private void OnDestroy()
        {
            // Additive 씬 언로드 시 Manager에서 Pop -> 이전 조명 자동 복원
            if (_isAdditive)
                MapLightManager.Instance.PopAdditive(lightData);
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// 런타임 중 이 씬의 조명을 교체합니다. (낮/밤 전환 등)
        /// Manager의 현재 슬롯(Primary or Additive 최상단)을 함께 갱신합니다.
        /// </summary>
        public void ApplyOverride(MapLightData data)
        {
            lightData = data;
            MapLightManager.Instance.ApplyOverride(data);
        }
    }
}
