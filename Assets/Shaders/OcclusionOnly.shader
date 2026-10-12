Shader "Custom/OcclusionOnly"
{
    // AR 平面 occluder 模式 = 純隱形平面：不畫顏色、不寫深度。
    // 遮擋(模型被真實物體擋住) → Lightship；地板影子 → 模型腳下抬高的 ShadowCatcher quad。
    // 平面不能往上抬(AR 管的、鎖在地板)，所以不適合在這裡畫影子(會跟 Lightship 地板共平面被吃)。
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ColorMask 0      // 不輸出顏色 + 不寫深度 = 完全隱形、不干擾任何東西

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
