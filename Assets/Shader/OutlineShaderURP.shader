Shader "Custom/URP/OutlineURP"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry+1"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float _OutlineWidth;
            half4 _OutlineColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 normal = normalize(IN.normalOS);
                float3 expandedPosition = IN.positionOS.xyz + normal * _OutlineWidth;

                OUT.positionHCS = TransformObjectToHClip(expandedPosition);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}