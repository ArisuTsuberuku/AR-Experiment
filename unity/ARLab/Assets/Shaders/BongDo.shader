// Mặt phẳng trong suốt chỉ hiện bóng đổ của đèn chính (đặt dưới mô hình, bóng rơi lên mặt bàn thật).
Shader "ARLab/BongDo"
{
    Properties
    {
        _DoDam ("Độ đậm của bóng", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "BongDo"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _DoDam;
            CBUFFER_END

            struct VaoDinh { float4 viTri : POSITION; };
            struct RaDinh { float4 viTriCS : SV_POSITION; float3 viTriWS : TEXCOORD0; };

            RaDinh vert(VaoDinh i)
            {
                RaDinh o;
                o.viTriWS = TransformObjectToWorld(i.viTri.xyz);
                o.viTriCS = TransformWorldToHClip(o.viTriWS);
                return o;
            }

            half4 frag(RaDinh i) : SV_Target
            {
                half sang = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.viTriWS));
                return half4(0, 0, 0, (1.0 - sang) * _DoDam);
            }
            ENDHLSL
        }
    }
}
