using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("Cloud Manager")]
public class CloudManager : VolumeComponent,IPostProcessComponent
{
    public bool IsActive() => true;
    public bool IsTileCompatible() => false;

    public IntParameter stepNums = new IntParameter(20);

    public Vector3Parameter BoundsMin = new Vector3Parameter(Vector3.one);
    public Vector3Parameter BoundsMax = new Vector3Parameter(Vector3.one);

    public Vector3Parameter DensityNoise_Offset = new Vector3Parameter(Vector3.one);

    public FloatParameter CloudScale = new FloatParameter(2.0f);
    public Vector3Parameter VolumeOffset = new Vector3Parameter(Vector3.zero);
    public FloatParameter DensityThreshold = new FloatParameter(0.3f);
    public FloatParameter DensityMultiplier = new FloatParameter(2.0f);
    public FloatParameter AddBaseDensity = new FloatParameter(0.5f);
    public FloatParameter DetailNoiseMultiplier = new FloatParameter(0.5f);

    public FloatParameter HeightMapFactor = new FloatParameter(0.5f);
    public FloatParameter ContainerEdgeDst = new FloatParameter(1.0f);

    public FloatParameter InScatterMultiplier = new FloatParameter(0.5f);
    public FloatParameter OutScatterMultiplier = new FloatParameter(0.5f);
    public FloatParameter ForwardScatter = new FloatParameter(0.5f);
    public FloatParameter BackwardScatter = new FloatParameter(0.5f);
    public FloatParameter PhaseMultiplier = new FloatParameter(0.5f);
    public FloatParameter BrightNess = new FloatParameter(0.5f);
    public ClampedIntParameter MultipleScatteringAdded = new ClampedIntParameter(5, 0, 8);

    public IntParameter StepsTowardsSun = new IntParameter(10);
    public FloatParameter TimeScale = new FloatParameter(1.0f);
    public FloatParameter MoveSpeed = new FloatParameter(2.0f);

    public TextureParameter WeatherMap = new TextureParameter(null);
    public TextureParameter BlueNoise = new TextureParameter(null);

    public void LoadMaterialArguement(Material material,ref RenderingData rendering)
    {
        //CloudBox = GameObject.Find("CloudBox").GetComponent<Transform>();

        material.SetVector("BoundsMin", BoundsMin.value);
        material.SetVector("BoundsMax", BoundsMax.value);
        material.SetVector("_CloudOffset", DensityNoise_Offset.value);
        material.SetInt("stepNums", stepNums.value);
        material.SetFloat("_cloudScale", CloudScale.value);
        material.SetVector("_volumeOffset", VolumeOffset.value);
        material.SetFloat("_DensityThreshold", DensityThreshold.value);
        material.SetFloat("_DensityMultiplier", DensityMultiplier.value);
        material.SetFloat("_addBaseDensity", AddBaseDensity.value); 
        material.SetFloat("detailNoiseMultiplier", DetailNoiseMultiplier.value);

        material.SetFloat("heightMapFactor", HeightMapFactor.value);
        material.SetTexture("_WeatherMap2D", WeatherMap.value);
        material.SetTexture("_BlueNoise2D", BlueNoise.value);
        material.SetFloat("containerEdgeDst", ContainerEdgeDst.value);

        material.SetFloat("inScatterMultiplier", InScatterMultiplier.value);
        material.SetFloat("outScatterMultiplier", OutScatterMultiplier.value);
        material.SetFloat("forwardScatter", ForwardScatter.value);
        material.SetFloat("backwardScatter", BackwardScatter.value);
        material.SetFloat("phaseMultiplier", PhaseMultiplier.value);
        material.SetFloat("brightNess", BrightNess.value);
        material.SetInt("stepsTowardsSun", StepsTowardsSun.value);
        material.SetInt("multipleScatteringAdded", MultipleScatteringAdded.value);
        

        material.SetFloat("timeScale", TimeScale.value);
        material.SetFloat("moveSpeed", MoveSpeed.value);
        
    }
}
