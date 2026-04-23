Shader "Custom/URP/MedievalBrickWall"
{
    Properties
    {
        [Header(Brick Colors)]
        _BrickColor1 ("Brick Color 1", Color) = (0.55, 0.35, 0.25, 1)
        _BrickColor2 ("Brick Color 2", Color) = (0.45, 0.28, 0.20, 1)
        _BrickColor3 ("Brick Color 3", Color) = (0.38, 0.22, 0.15, 1)
        _MortarColor ("Mortar Color", Color) = (0.3, 0.28, 0.25, 1)
        
        [Header(Moss Colors)]
        _MossColor1 ("Moss Bright", Color) = (0.25, 0.35, 0.15, 1)
        _MossColor2 ("Moss Mid", Color) = (0.15, 0.25, 0.10, 1)
        _MossColor3 ("Moss Dark", Color) = (0.08, 0.15, 0.05, 1)
        
        [Header(Brick Pattern)]
        _BrickScale ("Brick Scale", Range(0.5, 5)) = 1.5
        _BrickWidth ("Brick Width", Range(1, 3)) = 2.0
        _BrickHeight ("Brick Height", Range(0.3, 1.5)) = 0.5
        _MortarSize ("Mortar Size", Range(0.02, 0.15)) = 0.08
        _BrickRoughness ("Brick Roughness", Range(0.3, 1)) = 0.75
        
        [Header(Moss Settings)]
        _MossCoverage ("Moss Coverage", Range(0, 1)) = 0.45
        _MossHeight ("Moss Height", Range(0, 0.5)) = 0.15
        _MossRoughness ("Moss Roughness", Range(0.5, 1)) = 0.95
        _MossScale ("Moss Detail Scale", Range(5, 30)) = 15
        
        [Header(Surface Details)]
        _BrickDamage ("Brick Damage", Range(0, 1)) = 0.35
        _SurfaceDetail ("Surface Detail", Range(0, 2)) = 1.2
        _NormalStrength ("Normal Strength", Range(0, 3)) = 1.8
        _AOStrength ("AO Strength", Range(0, 1)) = 0.7
        
        [Header(Wetness and Aging)]
        _Wetness ("Wetness", Range(0, 1)) = 0.4
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.5
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.6
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BrickColor1, _BrickColor2, _BrickColor3;
                half4 _MortarColor;
                half4 _MossColor1, _MossColor2, _MossColor3;
                half _BrickScale, _BrickWidth, _BrickHeight, _MortarSize;
                half _MossCoverage, _MossHeight, _MossScale;
                half _BrickDamage, _SurfaceDetail, _NormalStrength;
                half _Wetness, _DirtAmount, _ColorVariation;
                half _BrickRoughness, _MossRoughness, _AOStrength;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };
            
            // ==================== NOISE LIBRARY ====================
            
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }
            
            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }
            
            float3 hash32(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yzz) * p3.zyx);
            }
            
            // Value noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash12(i);
                float b = hash12(i + float2(1, 0));
                float c = hash12(i + float2(0, 1));
                float d = hash12(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal Brownian Motion
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            // Voronoi noise
            float voronoi(float2 p)
            {
                float2 n = floor(p);
                float2 f = frac(p);
                
                float minDist = 1.0;
                
                for(int j = -1; j <= 1; j++)
                {
                    for(int i = -1; i <= 1; i++)
                    {
                        float2 b = float2(i, j);
                        float2 r = b + hash22(n + b) - f;
                        float d = length(r);
                        minDist = min(minDist, d);
                    }
                }
                
                return minDist;
            }
            
            // Brick pattern
            float3 brickPattern(float2 uv)
            {
                float2 brickUV = uv * float2(_BrickWidth, 1.0);
                
                // Offset every other row
                float row = floor(brickUV.y);
                brickUV.x += step(0.5, frac(row * 0.5)) * 0.5 * _BrickWidth;
                
                float2 brickID = floor(brickUV);
                float2 brickLocal = frac(brickUV);
                
                // Mortar gaps
                float2 mortarDist = smoothstep(0.0, _MortarSize, brickLocal) * 
                                    smoothstep(0.0, _MortarSize, 1.0 - brickLocal);
                float mortar = mortarDist.x * mortarDist.y;
                
                return float3(mortar, brickID.x, brickID.y);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.bitangentWS = normInputs.bitangentWS;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Base UV
                float2 uv = input.positionWS.xz * _BrickScale;
                
                // ==================== BRICK PATTERN ====================
                float3 brickData = brickPattern(uv);
                float brickMask = brickData.x;
                float2 brickID = brickData.yz;
                
                // Per-brick randomness
                float brickHash = hash12(brickID);
                float brickHash2 = hash12(brickID + 7.89);
                
                // ==================== BRICK COLOR ====================
                // Base brick color variation
                float colorSelect = hash12(brickID + 3.14);
                float3 brickBaseColor;
                
                if(colorSelect < 0.33)
                    brickBaseColor = _BrickColor1.rgb;
                else if(colorSelect < 0.66)
                    brickBaseColor = _BrickColor2.rgb;
                else
                    brickBaseColor = _BrickColor3.rgb;
                
                // Add color variation within brick
                float brickNoise = fbm(uv * 8.0 + brickHash * 100.0, 4);
                brickBaseColor = lerp(brickBaseColor * 0.7, brickBaseColor * 1.2, brickNoise);
                
                // Detailed surface texture
                float surfaceDetail = fbm(uv * 25.0, 3);
                brickBaseColor += (surfaceDetail - 0.5) * 0.2 * _SurfaceDetail;
                
                // Damage and cracks
                float damage = fbm(uv * 12.0 + brickHash * 50.0, 3);
                float cracks = voronoi(uv * 15.0 + brickHash * 30.0);
                cracks = smoothstep(0.1, 0.3, cracks);
                
                float damageMask = smoothstep(0.6, 0.8, damage) * _BrickDamage;
                brickBaseColor = lerp(brickBaseColor * 0.5, brickBaseColor, cracks);
                brickBaseColor = lerp(brickBaseColor, brickBaseColor * 0.6, damageMask);
                
                // ==================== MOSS ====================
                // Multiple layers of moss
                float mossBase = fbm(uv * 4.0 + 1000.0, 5);
                float mossDetail = fbm(uv * _MossScale, 4);
                float mossFine = fbm(uv * _MossScale * 3.0, 3);
                
                // Moss grows in cracks, damaged areas, and bottom parts
                float verticalGradient = saturate(frac(uv.y) * 2.0 - 0.3); // More moss at bottom
                
                float mossFactor = mossBase * _MossCoverage;
                mossFactor += (1.0 - cracks) * 0.25;
                mossFactor += damageMask * 0.3;
                mossFactor += (1.0 - brickMask) * 0.4; // Moss in mortar
                mossFactor -= verticalGradient * 0.3;
                mossFactor = saturate(mossFactor);
                mossFactor = smoothstep(0.3, 0.7, mossFactor);
                
                // Moss color layers
                float3 mossColor = lerp(_MossColor3.rgb, _MossColor2.rgb, mossDetail);
                mossColor = lerp(mossColor, _MossColor1.rgb, mossFine * mossFine);
                
                // Add some brown/yellow variation to moss
                float mossVariation = fbm(uv * 10.0 + 500.0, 3);
                mossColor = lerp(mossColor, mossColor * float3(0.8, 0.7, 0.4), mossVariation * 0.3);
                
                // ==================== DIRT AND AGING ====================
                float dirt = fbm(uv * 6.0 + 2000.0, 4);
                float dirtMask = smoothstep(0.4, 0.8, dirt) * _DirtAmount;
                
                float3 dirtColor = float3(0.2, 0.18, 0.15);
                brickBaseColor = lerp(brickBaseColor, dirtColor, dirtMask * 0.4);
                
                // ==================== WETNESS ====================
                float wetPattern = fbm(uv * 5.0 + 3000.0, 4);
                float wetMask = smoothstep(0.3, 0.7, wetPattern) * _Wetness;
                
                // Wet areas are darker
                float wetDarken = 1.0 - wetMask * 0.4;
                
                // More wetness on moss
                wetMask = lerp(wetMask, wetMask * 1.5, mossFactor);
                
                // ==================== FINAL ALBEDO ====================
                float3 albedo = lerp(brickBaseColor, mossColor, mossFactor);
                albedo = lerp(_MortarColor.rgb, albedo, brickMask);
                albedo *= wetDarken;
                
                // ==================== NORMAL MAPPING ====================
                float heightMap = surfaceDetail * 0.5 + mossDetail * 0.3 + damage * 0.2;
                heightMap += mossFactor * _MossHeight;
                heightMap -= (1.0 - brickMask) * 0.3; // Mortar is lower
                
                float3 tangentNormal;
                tangentNormal.xy = float2(ddx(heightMap), ddy(heightMap)) * _NormalStrength;
                tangentNormal.z = 1.0;
                tangentNormal = normalize(tangentNormal);
                
                // Transform to world space
                half3 bitangent = input.bitangentWS * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(tangentNormal, TBN));
                
                // ==================== MATERIAL PROPERTIES ====================
                float smoothness = lerp(_BrickRoughness, _MossRoughness, mossFactor);
                smoothness = 1.0 - smoothness;
                smoothness = lerp(smoothness, 0.6, wetMask); // Wet is smoother
                
                // Ambient Occlusion
                float ao = 1.0 - (1.0 - brickMask) * 0.5 * _AOStrength;
                ao *= lerp(1.0, 0.85, mossFactor * 0.5);
                ao *= lerp(1.0, 0.7, damageMask * 0.3);
                
                // ==================== LIGHTING ====================
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lightingInput.fogCoord = input.fogFactor;
                lightingInput.bakedGI = SampleSH(normalWS) * ao;
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.0;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = tangentNormal;
                surfaceData.emission = 0;
                surfaceData.occlusion = ao;
                surfaceData.alpha = 1.0;
                
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}