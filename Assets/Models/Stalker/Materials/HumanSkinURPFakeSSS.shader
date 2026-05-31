Shader "Unlit/HumanSkinURPFakeSSS"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 0.78, 0.68, 1)

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 0.6

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 0.65

        _SmoothnessMap ("Smoothness Map", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.42

        _ThicknessMap ("Thickness / SSS Mask", 2D) = "black" {}
        [HDR] _SSSColor ("SSS Color", Color) = (1, 0.32, 0.18, 1)
        _SSSStrength ("SSS Strength", Range(0, 1)) = 0.16
        _SSSPower ("SSS Power", Range(0.5, 8)) = 2.5

        _RimStrength ("Skin Rim Strength", Range(0, 1)) = 0.08
        _RimPower ("Skin Rim Power", Range(0.5, 8)) = 3

        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            TEXTURE2D(_SmoothnessMap);
            SAMPLER(sampler_SmoothnessMap);

            TEXTURE2D(_ThicknessMap);
            SAMPLER(sampler_ThicknessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _OcclusionStrength;
                half _Smoothness;
                half4 _SSSColor;
                half _SSSStrength;
                half _SSSPower;
                half _RimStrength;
                half _RimPower;
                half _Cutoff;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            half3 GetNormalWS(Varyings input)
            {
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half3 normalTS = UnpackNormalScale(normalSample, _BumpScale);

                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);

                return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
            }

            void BuildSurfaceData(Varyings input, out SurfaceData surfaceData)
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half occlusionSample = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                half smoothnessSample = SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap, input.uv).r;

                surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseSample.rgb;
                surfaceData.alpha = 1.0h;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.028h, 0.028h, 0.028h);
                surfaceData.smoothness = saturate(smoothnessSample * _Smoothness);
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
                surfaceData.occlusion = lerp(1.0h, occlusionSample, _OcclusionStrength);
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;
            }

            half3 GetFakeSubsurface(Varyings input, half3 normalWS, half3 viewDirWS)
            {
                Light mainLight = GetMainLight(input.shadowCoord);
                half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;

                half backLight = pow(saturate(dot(viewDirWS, -mainLight.direction)), _SSSPower);
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;

                half attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 sss = _SSSColor.rgb * mainLight.color * thickness * attenuation * backLight * _SSSStrength;
                half3 skinRim = _SSSColor.rgb * thickness * rim;

                return sss + skinRim;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = GetNormalWS(input);
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                SurfaceData surfaceData;
                BuildSurfaceData(input, surfaceData);
                surfaceData.emission = GetFakeSubsurface(input, normalWS, viewDirWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}