using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapLightDataTool.Example
{
    /// <summary>
    /// MapLightManager를 활용한 씬 전환 예시.
    ///
    /// 핵심 패턴:
    /// - 씬 로드 전 ReserveNextSceneType()으로 Primary / Additive를 미리 예약
    /// - 씬이 로드되면 SceneLightController.Awake에서 예약 타입을 자동으로 소비
    /// - Inspector의 sceneType 설정 없이도 로드 방식에 따라 조명이 올바르게 등록됨
    /// </summary>
    public class MapLightExample : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // 1. Primary 씬 전환
        // -----------------------------------------------------------------------

        /// <summary>
        /// 메인 씬을 전환합니다.
        /// 이전 씬을 언로드하고 새 씬을 Primary로 등록합니다.
        /// </summary>
        public void LoadPrimaryScene(string sceneName)
        {
            // 씬 로드 전 Primary로 예약
            // -> SceneLightController.Awake에서 RegisterPrimary() 호출
            MapLightManager.Instance.ReserveNextSceneType(sceneName, SceneLightController.SceneType.Primary);

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        // -----------------------------------------------------------------------
        // 2. Additive 씬 로드 / 언로드
        // -----------------------------------------------------------------------

        /// <summary>
        /// Additive 씬을 추가 로드합니다.
        /// 기존 조명 위에 이 씬의 조명을 덮어씌웁니다.
        /// </summary>
        public void LoadAdditiveScene(string sceneName)
        {
            // 씬 로드 전 Additive로 예약
            // -> SceneLightController.Awake에서 PushAdditive() 호출
            MapLightManager.Instance.ReserveNextSceneType(sceneName, SceneLightController.SceneType.Additive);

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Additive 씬을 언로드합니다.
        /// SceneLightController.OnDestroy에서 PopAdditive()가 호출되어 이전 조명이 자동 복원됩니다.
        /// </summary>
        public void UnloadAdditiveScene(string sceneName)
        {
            // 언로드 시 별도 예약 불필요
            // -> SceneLightController.OnDestroy -> MapLightManager.PopAdditive() -> 이전 조명 자동 복원
            SceneManager.UnloadSceneAsync(sceneName);
        }

        // -----------------------------------------------------------------------
        // 3. 런타임 중 조명 교체 (낮/밤 전환 등)
        // -----------------------------------------------------------------------

        [Header("Day / Night")]
        public MapLightData dayLightData;
        public MapLightData nightLightData;

        /// <summary>
        /// 현재 씬의 조명을 낮 데이터로 교체합니다.
        /// Manager의 현재 슬롯(Primary or Additive 최상단)이 갱신됩니다.
        /// </summary>
        public void SwitchToDay()
        {
            MapLightManager.Instance.ApplyOverride(dayLightData);
        }

        /// <summary>
        /// 현재 씬의 조명을 밤 데이터로 교체합니다.
        /// </summary>
        public void SwitchToNight()
        {
            MapLightManager.Instance.ApplyOverride(nightLightData);
        }

        // -----------------------------------------------------------------------
        // 4. 전체 흐름 예시
        // -----------------------------------------------------------------------

        private void Start()
        {
            // -- 예시 흐름 --------------------------------------------------
            //
            // [1] LobbyScene 진입 (Primary)
            //     ReserveNextSceneType("LobbyScene", Primary)
            //     SceneManager.LoadScene("LobbyScene", Single)
            //     -> SceneLightController.Awake: RegisterPrimary(LobbyLight)
            //        적용: LobbyLight
            //
            // [2] DungeonScene Additive 로드
            //     ReserveNextSceneType("DungeonScene", Additive)
            //     SceneManager.LoadScene("DungeonScene", Additive)
            //     -> SceneLightController.Awake: PushAdditive(DungeonLight)
            //        적용: DungeonLight  (스택: [LobbyLight | DungeonLight])
            //
            // [3] DungeonScene 언로드
            //     SceneManager.UnloadSceneAsync("DungeonScene")
            //     -> SceneLightController.OnDestroy: PopAdditive(DungeonLight)
            //        적용: LobbyLight   (자동 복원)
            //
            // [4] 낮/밤 전환
            //     MapLightManager.Instance.ApplyOverride(nightLightData)
            //        적용: nightLightData (현재 Primary 슬롯 교체)
            // ---------------------------------------------------------------
        }
    }
}
