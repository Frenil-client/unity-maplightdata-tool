using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapLightDataTool
{
    [CreateAssetMenu(fileName = "MapLightData", menuName = "Scriptable Objects/MapLightData", order = int.MaxValue)]
    public class MapLightData : ScriptableObject
    {
        public static MapLightData CurrentMapLightData;
        [Serializable]
        public class EnvironmentLightData
        {
            [ColorUsage(true, true)] public Color skyColor;
            [ColorUsage(true, true)] public Color equatorColor;
            [ColorUsage(true, true)] public Color groundColor;

            public EnvironmentLightData() { }

            public EnvironmentLightData(Color skyColor, Color equatorColor, Color groundColor)
            {
                this.skyColor = skyColor;
                this.equatorColor = equatorColor;
                this.groundColor = groundColor;
            }
            
            public void ApplyEnvLighting()
            {
                MapLightData.CurrentMapLightData = null;
                if(RenderSettings.ambientSkyColor != skyColor)
                    RenderSettings.ambientSkyColor = skyColor;
        
                if(RenderSettings.ambientEquatorColor != equatorColor)
                    RenderSettings.ambientEquatorColor = equatorColor;
        
                if(RenderSettings.ambientGroundColor != groundColor)
                    RenderSettings.ambientGroundColor = groundColor;
            }
        }
        
        [Header("Environment")] 
        public Material skybox;
        public Light sunSource;
        public Color shadowColor = Color.black;
        public AmbientMode ambientMode = AmbientMode.Trilight;
    //Environment Lighting
        public EnvironmentLightData environmentLightData;
    //Environment Reflections
        public float intensityMultiplier;
        public int reflectionBounces;

        [Header("Other Settings")] 
        public bool fog;
        public Color fogColor = Color.white;
        public int fogMode;
        public float density;
        public float fogStart;
        public float fogEnd;
        public float flareFadeSpeed;
        public float flareStrength;

        public void ApplyMapLightData()
        {
            if (CurrentMapLightData == this)
            {
                return;
            }
            CurrentMapLightData = this;
            ApplyEnvSky();
            ApplyEnvLighting();
            ApplyEnvReflections();
            ApplyEnvOtherSettings();
        }

        public void ApplyEnvSky()
        {
            if(RenderSettings.skybox != skybox)
                RenderSettings.skybox = skybox;
            
            if(RenderSettings.sun != sunSource)
                RenderSettings.sun = sunSource;
            
            if(RenderSettings.subtractiveShadowColor != shadowColor)
                RenderSettings.subtractiveShadowColor = shadowColor;
            
            if (RenderSettings.ambientMode != ambientMode)
                RenderSettings.ambientMode = ambientMode;
        }
        
        public void ApplyEnvLighting()
        {
            if(RenderSettings.ambientSkyColor != environmentLightData.skyColor)
                RenderSettings.ambientSkyColor = environmentLightData.skyColor;
        
            if(RenderSettings.ambientEquatorColor != environmentLightData.equatorColor)
                RenderSettings.ambientEquatorColor = environmentLightData.equatorColor;
        
            if(RenderSettings.ambientGroundColor != environmentLightData.groundColor)
                RenderSettings.ambientGroundColor = environmentLightData.groundColor;
        }

        public void ApplyEnvReflections()
        {
            if(RenderSettings.sun != sunSource)
                RenderSettings.sun = sunSource;
            
            if(RenderSettings.reflectionBounces != reflectionBounces)
                RenderSettings.reflectionBounces = reflectionBounces;
        }
        
        public void ApplyEnvOtherSettings()
        {
            if(RenderSettings.fog != fog) 
                RenderSettings.fog = fog;
            
            if (RenderSettings.fogColor != fogColor) 
                RenderSettings.fogColor = fogColor;
            
            if(RenderSettings.fogMode != (FogMode)fogMode) 
                RenderSettings.fogMode = (FogMode)fogMode;
            
            if(RenderSettings.fogDensity != density) 
                RenderSettings.fogDensity = density;
            
            if(RenderSettings.fogStartDistance != fogStart) 
                RenderSettings.fogStartDistance = fogStart;
            
            if(RenderSettings.fogEndDistance != fogEnd) 
                RenderSettings.fogEndDistance = fogEnd;
            
            if(RenderSettings.flareStrength != flareStrength) 
                RenderSettings.flareStrength = flareStrength;
            
            if(RenderSettings.flareFadeSpeed != flareFadeSpeed) 
                RenderSettings.flareFadeSpeed = flareFadeSpeed;
        }
#if UNITY_EDITOR
        public void SetRenderSetting()
        {
            skybox = RenderSettings.skybox;
            sunSource = RenderSettings.sun;
            shadowColor = RenderSettings.subtractiveShadowColor;
            ambientMode = RenderSettings.ambientMode;
            environmentLightData ??= new EnvironmentLightData();
            environmentLightData.skyColor = RenderSettings.ambientSkyColor;
            environmentLightData.equatorColor = RenderSettings.ambientEquatorColor;
            environmentLightData.groundColor = RenderSettings.ambientGroundColor;
            sunSource = RenderSettings.sun;
            reflectionBounces = RenderSettings.reflectionBounces;
            fog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = (int)RenderSettings.fogMode;
            density = RenderSettings.fogDensity;
            fogStart = RenderSettings.fogStartDistance; 
            fogEnd = RenderSettings.fogEndDistance;
            flareStrength = RenderSettings.flareStrength;
            flareFadeSpeed = RenderSettings.flareFadeSpeed;
        }
#endif

    }
}