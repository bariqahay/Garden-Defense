Shader "Custom/SprayGlowDissolve"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Glow Color", Color) = (0, 1, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.5)) = 0.1
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        _Transparency ("Transparency", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline" 
        }
        
        // Additive blending untuk glow effect
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _GlowIntensity;
                float _DissolveAmount;
                float _DissolveEdgeWidth;
                float4 _DissolveEdgeColor;
                float _Transparency;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Animate noise UV untuk dynamic effect
                output.noiseUV = input.uv + float2(_Time.y * 0.2, _Time.y * 0.3);
                
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample textures
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half noiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.noiseUV).r;
                
                // Dissolve effect
                half dissolveThreshold = noiseTex - _DissolveAmount;
                
                // Clip pixels below threshold (dissolve)
                clip(dissolveThreshold);
                
                // Calculate dissolve edge
                half edgeMask = step(dissolveThreshold, _DissolveEdgeWidth);
                
                // Base color dengan glow
                half3 baseColor = mainTex.rgb * _Color.rgb * _GlowIntensity;
                
                // Add dissolve edge glow
                half3 edgeGlow = _DissolveEdgeColor.rgb * edgeMask * 5.0;
                
                // Fresnel effect untuk rim glow
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0);
                half3 fresnelGlow = _Color.rgb * fresnel * 2.0;
                
                // Combine all effects
                half3 finalColor = baseColor + edgeGlow + fresnelGlow;
                
                // Alpha calculation
                half alpha = mainTex.a * _Transparency * (1.0 - edgeMask * 0.5);
                alpha *= (1.0 - _DissolveAmount); // Fade out saat dissolve
                
                half4 color = half4(finalColor, alpha);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}