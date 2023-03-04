Shader "CDC/Cloud"
{
    Properties{
        _MainTex("Main Texture", 2D) = "white"{}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader{
        Tags{
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        pass{

            Cull Off
            ZTest Always
            ZWrite Off
            
            HLSLPROGRAM

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

                #pragma vertex Vertex
                #pragma fragment Frag

                //Texture
                Texture2D _MainTex;
                SamplerState sampler_MainTex;
                Texture3D _WorleyNoise3D;
                SamplerState sampler_WorleyNoise3D;
                Texture3D _DetailNoise3D;
                SamplerState sampler_DetailNoise3D;
                Texture2D _WeatherMap2D;
                SamplerState sampler_WeatherMap2D;
                Texture2D _BlueNoise2D;
                SamplerState sampler_BlueNoise2D;

                //Position And Transform
                float3 BoundsMin;
                float3 BoundsMax;

                //Cloud Settings
                float _cloudScale;
                float _DensityThreshold;
                float _DensityMultiplier;
                float3 _volumeOffset;
                float _addBaseDensity;

                //Detail
                float detailNoiseScale;
			    float detailNoiseMultiplier;

                //Height and Edge
                float heightMapFactor;
                float containerEdgeDst;

                //Lighting
                float forwardScatter;
                float backwardScatter;
                float inScatterMultiplier;
                float outScatterMultiplier;
                float phaseMultiplier;
                float brightNess;
                int stepsTowardsSun;
                int multipleScatteringAdded;

                //Move
                float timeScale;
                float moveSpeed;

                int stepNums;

                struct vertexInput{
                    float4 vertex: POSITION;
                    float2 uv: TEXCOORD0;
                };
                struct vertexOutput{
                    float4 pos: SV_POSITION;
                    float2 uv: TEXCOORD0;
                    float3 viewVector : TEXCOORD1;
                };

                // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero)
                float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir) {
                    // From http://jcgt.org/published/0007/03/04/
                    // via https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
                    float3 t0 = (boundsMin - rayOrigin) / rayDir;
                    float3 t1 = (boundsMax - rayOrigin) / rayDir;
                    float3 tmin = min(t0, t1);
                    float3 tmax = max(t0, t1);
                    
                    float dstA = max(max(tmin.x, tmin.y), tmin.z);
                    float dstB = min(tmax.x, min(tmax.y, tmax.z));

                    // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                    // dstA is dst to nearest intersection, dstB dst to far intersection

                    // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                    // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                    // CASE 3: ray misses box (dstA > dstB)

                    float dstToBox = max(0, dstA);
                    float dstInsideBox = max(0, dstB - dstToBox);
                    return float2(dstToBox, dstInsideBox);
                }

                vertexOutput Vertex(vertexInput v){

                    vertexOutput o;
                    o.pos = TransformObjectToHClip(v.vertex.xyz);
                    o.uv = v.uv;
                    float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1)).xyz;
                    o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0)).xyz; 
                    return o;
                }

                float beer(float d) 
                {
				    return exp(-d);
			    }

                //Range Remap
                float remap(float original_value, float original_min, float original_max, float new_min, float new_max)
		        {
			        return new_min + ((original_value - original_min) / (original_max - original_min)) * (new_max - new_min);
		        }

                float HenyeyGreenstein(float angle, float g) 
                {
				    return (1.0f - pow(g,2)) / (4.0f * 3.14159 * pow(1 + pow(g, 2) - 2.0f * g * angle, 1.5f));
			    }

                float HGScatter(float angle)
                {
                    float scatterValue = (HenyeyGreenstein(angle, forwardScatter) + HenyeyGreenstein(angle, - backwardScatter)) / 2.0;

                    return brightNess + scatterValue * phaseMultiplier;
                }

                float2 squareUV(float2 uv) {
                    float width = _ScreenParams.x;
                    float height =_ScreenParams.y;
                    //float minDim = min(width, height);
                    float scale = 1000;
                    float x = uv.x * width;
                    float y = uv.y * height;
                    return float2 (x/scale, y/scale);
                }

                float SampleCloudDensity(float3 pos)
                {
                    float time = _Time.x * timeScale;
                    float3 boxSize = BoundsMax - BoundsMin;
                    float3 center = (BoundsMax + BoundsMin) * 0.5;
                    float3 uvw = (boxSize * 0.5 + pos) * 0.01 * _cloudScale;
                    float3 samplePos = uvw + _volumeOffset * 0.01 + float3(time,time*0.1,time*0.2) * moveSpeed;;

                    //Soft Edge
                    float dstToEdgeX = min(containerEdgeDst, min(pos.x - BoundsMin.x, BoundsMax.x - pos.x));
                    float dstToEdgeZ = min(containerEdgeDst, min(pos.z - BoundsMin.z, BoundsMax.z - pos.z));
                    float edgeWeight = min(dstToEdgeX, dstToEdgeZ) / containerEdgeDst;
                    
                    //Set Density Height
                    float2 weatherMapUV = (boxSize.xz * 0.5 + (pos.xz - center.xz)) / max(boxSize.x, boxSize.z);
                    float weather = _WeatherMap2D.SampleLevel(sampler_WeatherMap2D, weatherMapUV, 0).r;
                    float gMin = remap(weather,0,1,0.1,0.5);
                    float gMax = remap(weather,0,1,gMin,0.9);
                    float heightPercent = (pos.y - BoundsMin.y) / boxSize.y;
                    float heightGradient = saturate(remap(heightPercent, 0.0, gMin, 0, 1)) * saturate(remap(heightPercent, 1, gMax, 0, 1));
                    heightGradient *= edgeWeight;

                    //Sample Base Texture
                    float4 shape = _WorleyNoise3D.SampleLevel(sampler_WorleyNoise3D, samplePos, 0);
                    float baseShape = max(0, shape.r - _DensityThreshold) * heightGradient + _addBaseDensity * 0.1;//max(0, shape.r - _DensityThreshold) * _DensityMultiplier * heightGradient;
                    float low_freq_fbm = shape.g;

                    if(baseShape <= 0) return 0;

                    float finalShape = baseShape - low_freq_fbm * pow(1 - baseShape, 3) * (1 - heightGradient) * detailNoiseMultiplier;

                    return finalShape * _DensityMultiplier * 0.1;
                }

                float MarchingAlongLight(float3 pos)
                {
                    float3 lightDir = _MainLightPosition;
                    float dstInsideBox = rayBoxDst(BoundsMin, BoundsMax, pos, 1/lightDir).y;
                    float stepSize = dstInsideBox / stepsTowardsSun;

                    float densityTowardsSun = 0;
                    for(int i = 0; i < stepsTowardsSun; i++)
                    {
                        pos += lightDir * stepSize;
                        densityTowardsSun += max(0, SampleCloudDensity(pos) * stepSize);
                    }

                    float transmit = beer(densityTowardsSun * inScatterMultiplier);
                    return transmit;
                }

                float MultipleOctaveScattering(float density, float angle, float stepSize)
                {
                    float attenuation = 0.2;
                    float contribution = 0.4;
                    float phaseAttenuation = 0.1;

                    float a = 1.0;
                    float b = 1.0;
                    float c = 1.0;
                    float g = 0.85;

                    float luminance = 0.0;

                    for(int i = 0; i < multipleScatteringAdded; i++)
                    {
                        float phaseValue = HenyeyGreenstein(0.3 * c, angle);
                        float beerValue = beer(density * a * stepSize); 
                        luminance += b * phaseValue * beerValue;

                        a *= attenuation;
                        b *= contribution;
                        c *= (1 - phaseAttenuation);
                    }
                    return luminance;
                }

                float4 Frag(vertexOutput o): SV_TARGET{

                    float4 col = _MainTex.Sample(sampler_MainTex, o.uv);
                    float3 rayOrigin = _WorldSpaceCameraPos;
                    float3 rayDir = normalize(o.viewVector);

                    float nonLinearDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, o.uv).r;
                    float depth = LinearEyeDepth(nonLinearDepth, _ZBufferParams) * length(o.viewVector);
                 
                    float2 rayBoxInfo = rayBoxDst(BoundsMin, BoundsMax, rayOrigin, rayDir);
                    float dstToBox = rayBoxInfo.x;
                    float dstInsideBox = rayBoxInfo.y;

                    float3 startPos = rayOrigin + rayDir * dstToBox;

                    float randomOffset = _BlueNoise2D.Sample(sampler_BlueNoise2D, squareUV(o.uv*3));
                   
                    float dstTravelled = randomOffset;
                    float stepSize = dstInsideBox / stepNums;
                    float dstShouldTravel = min(depth - dstToBox, dstInsideBox);
                    bool isHitBox = dstInsideBox > 0 && depth > dstToBox;

                    float phaseAngle = dot(rayDir, _MainLightPosition.xyz);
                    float phaseValue = HGScatter(phaseAngle);

                    float3 currentRayPos;
                    float Illumination = 0;
                    float transmit  = 1;
                    while(dstTravelled < dstShouldTravel)
                    {
                        currentRayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
                        float cloudDensity = SampleCloudDensity(currentRayPos);

                        if(cloudDensity > 0)
                        {
                            Illumination += cloudDensity * transmit * stepSize * MarchingAlongLight(currentRayPos) * phaseValue;
                            Illumination += MultipleOctaveScattering(cloudDensity, phaseAngle, stepSize) * cloudDensity * transmit;
                            transmit *= beer(cloudDensity * stepSize * outScatterMultiplier);
                            if (transmit < 0.001) break;
                        }
                        dstTravelled += stepSize;
                    }
                    return col * transmit + float4(Illumination * _MainLightColor.rgb, 1.0);
                }
            ENDHLSL
        }
    }
}
