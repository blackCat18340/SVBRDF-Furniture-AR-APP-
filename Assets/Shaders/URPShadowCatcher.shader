Shader "Custom/URPShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _Strength ("Shadow Strength", Range(0, 1)) = 0.6
        // 把影子「比深度用」的位置沿視線往相機推這麼多公尺，贏過共平面的 Lightship 地板深度。
        // 畫面位置不動(沿視線推) → 影子仍黏在地板；模型離相機近得多，照樣擋住影子。
        _DepthBias ("Depth Bias toward camera (m)", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            half4 _ShadowColor;
            half _Strength;
            half _DepthBias;
            half _ShadowStrengthMul;   // 全域：由 CameraLightEstimation 依光照亮度設定(0~1)。未設(=0)時當 1。

            Varyings vert(Attributes IN)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                o.positionWS = wp;   // 取樣陰影用真實地板位置
                // 只把「算深度」的位置沿視線往相機推 _DepthBias：畫面位置幾乎不動，但深度更近 → 贏過 Lightship 地板，
                // 又因為模型離相機近得多，模型仍正常擋住影子(ZTest LEqual)。
                float3 camWS = GetCameraPositionWS();
                float3 biased = wp + normalize(camWS - wp) * _DepthBias;
                o.positionHCS = TransformWorldToHClip(biased);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                // shadowAttenuation: 1 = 受光, 0 = 全陰影
                half shadow = 1.0 - mainLight.shadowAttenuation;
                half mul = _ShadowStrengthMul <= 0.0 ? 1.0 : _ShadowStrengthMul; // 未設時當 1，編輯器無腳本也正常
                half a = shadow * _Strength * _ShadowColor.a * mul;
                return half4(_ShadowColor.rgb, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
