using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GenerateWorley3D : MonoBehaviour
{
    [SerializeField] const int computeThreadGroupSize = 8;
    [SerializeField] const string worleyNoiseName = "WorleyNoise";

    public ComputeShader worleyNoiseCompute;

    [Header("Noise Settings")]
    public int noiseResolution = 50;
    public int cellsInXAxis = 5;
    public int cellsInYAxis = 10;
    public int cellsInZAxis = 7;
    public int seed = 1;
    public bool updateNoise = true;


    [SerializeField,HideInInspector]
    public RenderTexture worleyNoise3D;
    public RenderTexture texTest;

    List<ComputeBuffer> buffersToRelease;
    private ComputeBuffer resultBuffer;
    public void CreateTexture(ref RenderTexture texture, int resolution, string name)
    {
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
        if (texture == null || !texture.IsCreated() || texture.width != resolution || texture.height != resolution || texture.volumeDepth != resolution || texture.graphicsFormat != format)
        {
            //Debug.Log ("Create tex: update noise: " + updateNoise);
            if (texture != null)
            {
                texture.Release();
            }
            texture = new RenderTexture(resolution, resolution, 0);
            texture.graphicsFormat = format;
            texture.volumeDepth = resolution;
            texture.enableRandomWrite = true;
            texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture.name = name;

            texture.Create();
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
    }


    public void CalculateNoise()
    {
        ValidateParameters();
        CreateTexture(ref worleyNoise3D, noiseResolution, worleyNoiseName);

        if (updateNoise == true && worleyNoiseCompute)
        {
            updateNoise = false;

            buffersToRelease = new List<ComputeBuffer>();
            worleyNoiseCompute.SetInt("resolution", noiseResolution);
            worleyNoiseCompute.SetTexture(0, "Result", worleyNoise3D);

            int threadGroupNums = Mathf.CeilToInt(noiseResolution / (float)computeThreadGroupSize);

            var prng = new System.Random(seed);
            SetWorleyComputeBuffer(prng, cellsInXAxis, "pointsXAxis");
            SetWorleyComputeBuffer(prng, cellsInYAxis, "pointsYAxis");
            SetWorleyComputeBuffer(prng, cellsInZAxis, "pointsZAxis");

            worleyNoiseCompute.Dispatch(0, threadGroupNums, threadGroupNums, threadGroupNums);
            if (buffersToRelease == null)
            {
                Debug.Log("no add");
            }
        }

        if (buffersToRelease == null)
        {
            Debug.Log("yy");
        }

        foreach (var buffer in buffersToRelease)
        {
            buffer.Release();
        }

    }

    public void SetWorleyComputeBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
    {
        var randomPointsPos = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
        float cellSize = 1f / numCellsPerAxis;

        for (int x = 0; x < numCellsPerAxis; x++)
        {
            for (int y = 0; y < numCellsPerAxis; y++)
            {
                for (int z = 0; z < numCellsPerAxis; z++)
                {
                    float randomX = (float)prng.NextDouble();
                    float randomY = (float)prng.NextDouble();
                    float randomZ = (float)prng.NextDouble();

                    Vector3 randomPointOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCenter = new Vector3(x, y, z) / cellSize;

                    int cellID = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                    randomPointsPos[cellID] = cellCenter + randomPointOffset;
                }
            }
        }

        var randomPointsBuffer = new ComputeBuffer(randomPointsPos.Length, 3 * sizeof(float), ComputeBufferType.Structured);
        buffersToRelease.Add(randomPointsBuffer);
        randomPointsBuffer.SetData(randomPointsPos);
        worleyNoiseCompute.SetBuffer(0, bufferName, randomPointsBuffer);

        //resultBuffer = new ComputeBuffer(noiseResolution * noiseResolution * noiseResolution, 1 * sizeof(float));
        //worleyNoiseCompute.SetBuffer(0, "noiseResult", resultBuffer);
    }

    public void ValidateParameters()
    {
        noiseResolution = Mathf.Max(1, noiseResolution);
    }
}
