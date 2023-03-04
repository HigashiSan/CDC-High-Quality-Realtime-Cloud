using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader cloudShader;             
    [SerializeField] private RenderPassEvent evt = RenderPassEvent.BeforeRenderingPostProcessing;
    private Material cloudMat;                      
    private CloudRenderPass cloudPass;

    public RenderTexture worley3D;
    public RenderTexture weatherMap2D;
    public bool needUpdateNoise = true;

    public override void Create()
    {
        cloudPass = new CloudRenderPass();
        cloudPass.renderPassEvent = evt;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (cloudShader == null) return;
        if (cloudMat == null) cloudMat = CoreUtils.CreateEngineMaterial(cloudShader);

        var noise = FindObjectOfType<NoiseGenerator>();
        noise.CalculateNoise();
        worley3D = noise.noiseTexture;

        var weather = FindObjectOfType<WeatherMapGenerator>();
        weather.CalculateWeatherMap();
        weatherMap2D = weather.weatherMap2D;

        RenderTargetIdentifier currentRT = renderer.cameraColorTarget;
        cloudPass.Setup(currentRT, cloudMat, worley3D, weatherMap2D);
        renderer.EnqueuePass(cloudPass);
    }
}
