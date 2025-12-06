Shader "Custom/PlayerShootGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (0, 1, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _Smoothness;
                float _Metallic;
                float _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Base color
                half3 albedo = texColor.rgb * _BaseColor.rgb;
                
                // Lighting setup
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Diffuse lighting (Half-Lambert)
                float NdotL = dot(normalWS, mainLight.direction);
                float halfLambert = NdotL * 0.5 + 0.5;
                half3 diffuse = mainLight.color * halfLambert;
                
                // Specular (Blinn-Phong)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = lerp(16.0, 256.0, _Smoothness);
                float specular = pow(NdotH, specularPower) * _Smoothness;
                
                // Metallic workflow (simplified)
                half3 f0 = lerp(half3(0.04, 0.04, 0.04), albedo, _Metallic);
                half3 specularColor = lerp(mainLight.color, albedo, _Metallic) * specular;
                
                // Combine lighting
                half3 ambient = half3(0.2, 0.2, 0.2);
                half3 litColor = albedo * (diffuse + ambient) + specularColor;
                
                // Emission glow (pulse effect optional)
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                half3 emission = _EmissionColor.rgb * _EmissionIntensity;
                emission *= (1.0 + pulse * 0.3); // Subtle pulse
                
                // Fresnel rim glow saat emission aktif
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0);
                half3 rimGlow = _EmissionColor.rgb * fresnel * _EmissionIntensity * 0.5;
                
                // Final color
                half3 finalColor = litColor + emission + rimGlow;
                
                half4 color = half4(finalColor, texColor.a);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}