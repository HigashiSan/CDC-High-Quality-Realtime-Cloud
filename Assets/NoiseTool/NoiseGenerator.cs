using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
    const int computeThreadGroupSize = 8;
    public const string shapeTextureName = "ShapeTexture";
    public const string detailTextureName = "DetailTexture";

    [Header("Editor Settings")]
    public bool autoUpdate;
    public bool logComputeTime;

    [Header("Noise Settings")]
    public int noiseTextureResolution = 132;
    public int detailTextureResolution = 64;
    public int seed;
    [Range(1, 50)]
    public int worleyFrequencyA = 5;
    [Range(1, 50)]
    public int worleyFrequencyB = 10;
    [Range(1, 50)]
    public int worleyFrequencyC = 15;
    [Range(1, 50)]
    public int perlinFrequencyA = 15;
    [Range(1, 50)]
    public int perlinFrequencyB = 15;

    [Range(0, 1.0f)]
    public float perlinWorleyMixture = 0.5f;

    public float fbmFactor = .5f;
    public int tile = 1;
    public bool invert = true;

    public ComputeShader noiseCompute;

    // Internal
    List<ComputeBuffer> buffersToRelease;
    bool updateNoise;

    [SerializeField, HideInInspector]
    public RenderTexture noiseTexture;
    public RenderTexture detailTexture;

    void CreateTexture(ref RenderTexture texture, int resolution, string name)
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
        ValidateParamaters();
        CreateTexture(ref noiseTexture, noiseTextureResolution, shapeTextureName);
        CreateTexture(ref detailTexture, detailTextureResolution, detailTextureName);

        if (updateNoise && noiseCompute)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            updateNoise = false;

            buffersToRelease = new List<ComputeBuffer>();

            // Set values:
            noiseCompute.SetFloat("persistence", fbmFactor);
            noiseCompute.SetInt("resolution", noiseTextureResolution);

            // Set noise gen kernel data:
            noiseCompute.SetTexture(0, "Result", noiseTexture);
            var minMaxBuffer = CreateBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "minMax", 0);
            UpdateNoiseSettings();
            noiseCompute.SetTexture(0, "Result", noiseTexture);

            // Dispatch noise gen kernel
            int numThreadGroups = Mathf.CeilToInt(noiseTextureResolution / (float)computeThreadGroupSize);
            noiseCompute.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

            //Set normalization kernel data:
            noiseCompute.SetBuffer(1, "minMax", minMaxBuffer);
            noiseCompute.SetTexture(1, "Result", noiseTexture);
            //Dispatch normalization kernel
            noiseCompute.Dispatch(1, numThreadGroups, numThreadGroups, numThreadGroups);

            if (logComputeTime)
            {
                // Get minmax data just to force main thread to wait until compute shaders are finished.
                // This allows us to measure the execution time.
                var minMax = new int[2];
                minMaxBuffer.GetData(minMax);

                Debug.Log($"Noise Generation: {timer.ElapsedMilliseconds}ms");
            }

            // Release buffers
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }
    }

    void UpdateNoiseSettings()
    {
        var prng = new System.Random(seed);
        CreatePointsBuffer(prng, worleyFrequencyA, "pointsA");
        CreatePointsBuffer(prng, worleyFrequencyB, "pointsB");
        CreatePointsBuffer(prng, worleyFrequencyC, "pointsC");
        CreatePointsBuffer(prng, perlinFrequencyA, "pointsD");
        CreatePointsBuffer(prng, perlinFrequencyB, "pointsE");

        noiseCompute.SetInt("worleyFrequencyA", worleyFrequencyA);
        noiseCompute.SetInt("worleyFrequencyB", worleyFrequencyB);
        noiseCompute.SetInt("worleyFrequencyC", worleyFrequencyC);
        noiseCompute.SetInt("perlinFrequencyA", perlinFrequencyA);
        noiseCompute.SetInt("perlinFrequencyB", perlinFrequencyB);
        noiseCompute.SetFloat("perlinWorleyMixture", perlinWorleyMixture);
        noiseCompute.SetBool("invertNoise", invert);
        noiseCompute.SetInt("tile", tile);

    }

    void CreatePointsBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
    {
        var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
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
                    Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCorner = new Vector3(x, y, z) * cellSize;

                    int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                    points[index] = cellCorner + randomOffset;
                }
            }
        }

        //CreateBuffer(points, sizeof(float) * 3, bufferName);
        var buffer = new ComputeBuffer(points.Length, sizeof(float) * 3, ComputeBufferType.Structured);
        buffersToRelease.Add(buffer);
        buffer.SetData(points);
        noiseCompute.SetBuffer(0, bufferName, buffer);
    }

    ComputeBuffer CreateBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
        buffersToRelease.Add(buffer);
        buffer.SetData(data);
        noiseCompute.SetBuffer(kernel, bufferName, buffer);
        return buffer;
    }

    public void ManualUpdate()
    {
        updateNoise = true;
        CalculateNoise();
    }

    void OnValidate() { }

    public void ActiveNoiseSettingsChanged()
    {
        if (autoUpdate)
        {
            updateNoise = true;
        }
    }

    void ValidateParamaters()
    {
        noiseTextureResolution = Mathf.Max(1, noiseTextureResolution);
    }
}