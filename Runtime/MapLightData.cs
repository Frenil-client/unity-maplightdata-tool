using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapLightDataTool
{
    /// <summary>
    /// 씬별 조명 환경 데이터 컨테이너 (ScriptableObject).
    ///
    /// 핵심 설계:
    /// - RenderSettings 전체(Skybox / Ambient / Fog / Flare)를 에셋으로 저장
    /// - ApplyMapLightData() 호출 한 번으로 씬 조명 전체 교체
    /// - CurrentMapLightData 비교로 동일 에셋 중복 적용 방지
    /// - 변경된 항목만 RenderSettings에 기록해 불필요한 갱신 최소화
    /// - SetRenderSetting()으로 현재 씬 조명을 에디터에서 즉시 캡처 가능
    /// </summary>
    [CreateAssetMenu(fileName = "MapLightData", menuName = "Scriptable Objects/MapLightData", order = int.MaxValue)]
    public class MapLightData : ScriptableObject
    {
        /// <summary>현재 적용 중인 MapLightData. 중복 적용 방지에 사용됩니다.</summary>
        public static MapLightData CurrentMapLightData;

        // -----------------------------------------------------------------------

        /// <summary>
        /// Ambient Trilight 색상 3종 묶음.
        /// AmbientMode.Trilight 사용 시 Sky / Equator / Ground 색상을 함께 관리합니다.
        /// </summary>
        [Serializable]
        public class EnvironmentLightData
        {
            /// <summary>Ambient Sky 색상 (HDR)</summary>
            [ColorUsage(true, true)] public Color skyColor;

            /// <summary>Ambient Equator 색상 (HDR)</summary>
            [ColorUsage(true, true)] public Color equatorColor;

            /// <summary>Ambient Ground 색상 (HDR)</summary>
            [ColorUsage(true, true)] public Color groundColor;

            public EnvironmentLightData() { }

            public EnvironmentLightData(Color skyColor, Color equatorColor, Color groundColor)
            {
                this.skyColor    = skyColor;
                this.equatorColor = equatorColor;
                this.groundColor  = groundColor;
            }

            /// <summary>Ambient 색상 3종을 RenderSettings에 적용합니다.</summary>
            public void ApplyEnvLighting()
            {
                // EnvironmentLightData 단독 적용 시 CurrentMapLightData 무효화
                MapLightData.CurrentMapLightData = null;

                if (RenderSettings.ambientSkyColor != skyColor)
                    RenderSettings.ambientSkyColor = skyColor;

                if (RenderSettings.ambientEquatorColor != equatorColor)
                    RenderSettings.ambientEquatorColor = equatorColor;

                if (RenderSettings.ambientGroundColor != groundColor)
                    RenderSettings.ambientGroundColor = groundColor;
            }
        }

        // -----------------------------------------------------------------------

        [Header("Environment")]
        /// <summary>Skybox 머티리얼</summary>
        public Material skybox;

        /// <summary>태양광 방향 Light</summary>
        public Light sunSource;

        /// <summary>서브트랙티브 그림자 색상</summary>
        public Color shadowColor = Color.black;

        /// <summary>Ambient 모드 (Trilight / Flat / Skybox)</summary>
        public AmbientMode ambientMode = AmbientMode.Trilight;

        /// <summary>Ambient Trilight 색상 데이터</summary>
        public EnvironmentLightData environmentLightData;

        /// <summary>환경 반사 강도 배율</summary>
        public float intensityMultiplier;

        /// <summary>반사 바운스 횟수</summary>
        public int reflectionBounces;

        [Header("Other Settings")]
        /// <summary>Fog 활성화 여부</summary>
        public bool fog;

        /// <summary>Fog 색상</summary>
        public Color fogColor = Color.white;

        /// <summary>Fog 모드 (Exponential / ExponentialSquared / Linear)</summary>
        public int fogMode;

        /// <summary>Exponential Fog 밀도</summary>
        public float density;

        /// <summary>Linear Fog 시작 거리</summary>
        public float fogStart;

        /// <summary>Linear Fog 종료 거리</summary>
        public float fogEnd;

        /// <summary>Flare 페이드 속도</summary>
        public float flareFadeSpeed;

        /// <summary>Flare 강도</summary>
        public float flareStrength;

        // -----------------------------------------------------------------------

        /// <summary>
        /// 이 에셋의 조명 데이터를 RenderSettings에 전체 적용합니다.
        /// 이미 적용된 에셋이면 즉시 반환합니다.
        /// </summary>
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

        /// <summary>Skybox / Sun / Shadow / AmbientMode를 적용합니다.</summary>
        public void ApplyEnvSky()
        {
            if (RenderSettings.skybox != skybox)
                RenderSettings.skybox = skybox;

            if (RenderSettings.sun != sunSource)
                RenderSettings.sun = sunSource;

            if (RenderSettings.subtractiveShadowColor != shadowColor)
                RenderSettings.subtractiveShadowColor = shadowColor;

            if (RenderSettings.ambientMode != ambientMode)
                RenderSettings.ambientMode = ambientMode;
        }

        /// <summary>Ambient Trilight 색상 3종을 적용합니다.</summary>
        public void ApplyEnvLighting()
        {
            if (RenderSettings.ambientSkyColor != environmentLightData.skyColor)
                RenderSettings.ambientSkyColor = environmentLightData.skyColor;

            if (RenderSettings.ambientEquatorColor != environmentLightData.equatorColor)
                RenderSettings.ambientEquatorColor = environmentLightData.equatorColor;

            if (RenderSettings.ambientGroundColor != environmentLightData.groundColor)
                RenderSettings.ambientGroundColor = environmentLightData.groundColor;
        }

        /// <summary>환경 반사(Reflection Bounces)를 적용합니다.</summary>
        public void ApplyEnvReflections()
        {
            if (RenderSettings.sun != sunSource)
                RenderSettings.sun = sunSource;

            if (RenderSettings.reflectionBounces != reflectionBounces)
                RenderSettings.reflectionBounces = reflectionBounces;
        }

        /// <summary>Fog / Flare 설정을 적용합니다.</summary>
        public void ApplyEnvOtherSettings()
        {
            if (RenderSettings.fog != fog)
                RenderSettings.fog = fog;

            if (RenderSettings.fogColor != fogColor)
                RenderSettings.fogColor = fogColor;

            if (RenderSettings.fogMode != (FogMode)fogMode)
                RenderSettings.fogMode = (FogMode)fogMode;

            if (RenderSettings.fogDensity != density)
                RenderSettings.fogDensity = density;

            if (RenderSettings.fogStartDistance != fogStart)
                RenderSettings.fogStartDistance = fogStart;

            if (RenderSettings.fogEndDistance != fogEnd)
                RenderSettings.fogEndDistance = fogEnd;

            if (RenderSettings.flareStrength != flareStrength)
                RenderSettings.flareStrength = flareStrength;

            if (RenderSettings.flareFadeSpeed != flareFadeSpeed)
                RenderSettings.flareFadeSpeed = flareFadeSpeed;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 현재 씬의 RenderSettings 값을 이 에셋에 저장합니다. (Editor only)
        /// SceneInvalidCheckTool에서 MapLightData 에셋 일괄 생성 시 호출됩니다.
        /// </summary>
        public void SetRenderSetting()
        {
            skybox            = RenderSettings.skybox;
            sunSource         = RenderSettings.sun;
            shadowColor       = RenderSettings.subtractiveShadowColor;
            ambientMode       = RenderSettings.ambientMode;

            environmentLightData ??= new EnvironmentLightData();
            environmentLightData.skyColor     = RenderSettings.ambientSkyColor;
            environmentLightData.equatorColor = RenderSettings.ambientEquatorColor;
            environmentLightData.groundColor  = RenderSettings.ambientGroundColor;

            reflectionBounces = RenderSettings.reflectionBounces;
            fog               = RenderSettings.fog;
            fogColor          = RenderSettings.fogColor;
            fogMode           = (int)RenderSettings.fogMode;
            density           = RenderSettings.fogDensity;
            fogStart          = RenderSettings.fogStartDistance;
            fogEnd            = RenderSettings.fogEndDistance;
            flareStrength     = RenderSettings.flareStrength;
            flareFadeSpeed    = RenderSettings.flareFadeSpeed;
        }
#endif
    }
}
