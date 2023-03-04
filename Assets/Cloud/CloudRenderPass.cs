using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudRenderPass : ScriptableRenderPass
{
    const string passTag = "Cloud Pass";
    private CloudManager cloudVolume;
    private Material cloudMat;
    private RenderTargetIdentifier currentCameraRT;
    private RenderTargetHandle tempRT;
    private RenderTexture worley3D;
    private RenderTexture weatherMap2D;

    public void Setup(RenderTargetIdentifier currentRT, Material material, RenderTexture worleyNoise, RenderTexture weatherMap2D)
    {
        this.currentCameraRT = currentRT;
        this.cloudMat = material;
        this.worley3D = worleyNoise;
        this.weatherMap2D = weatherMap2D;
    }

    public void Render(CommandBuffer myCommandBuffer, ref RenderingData renderingData)
    {
        if (cloudVolume.IsActive())
        {
            cloudVolume.LoadMaterialArguement(cloudMat, ref renderingData);
            RenderTextureDescriptor tempRTDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            tempRTDescriptor.depthBufferBits = 0;
            myCommandBuffer.GetTemporaryRT(tempRT.id, tempRTDescriptor);
            cloudMat.SetTexture("_WorleyNoise3D", worley3D);
            cloudMat.SetTexture("_WeatherMap2D", weatherMap2D);
            
            myCommandBuffer.Blit(currentCameraRT, tempRT.Identifier(), cloudMat);
            myCommandBuffer.Blit(tempRT.Identifier(), currentCameraRT);
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        VolumeStack stack = VolumeManager.instance.stack;
        cloudVolume = stack.GetComponent<CloudManager>();

        CommandBuffer cmd = CommandBufferPool.Get(passTag);
        Render(cmd, ref renderingData);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        cmd.ReleaseTemporaryRT(tempRT.id);
    }
}
