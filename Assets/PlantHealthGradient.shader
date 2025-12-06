Shader "Custom/PlantHealthGradient"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _HealthyColor ("Healthy Color", Color) = (0, 1, 0, 1)
        _DamagedColor ("Damaged Color", Color) = (1, 1, 0, 1)
        _CriticalColor ("Critical Color", Color) = (0.6, 0.3, 0, 1)
        _DeadColor ("Dead Color", Color) = (0.3, 0.15, 0, 1)
        _HealthRatio ("Health Ratio", Range(0, 1)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
        _WiltAmount ("Wilt Amount", Range(0, 1)) = 0
        _WiltStrength ("Wilt Strength", Range(0, 0.5)) = 0.1
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
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _HealthyColor;
                float4 _DamagedColor;
                float4 _CriticalColor;
                float4 _DeadColor;
                float _HealthRatio;
                float _Smoothness;
                float _WiltAmount;
                float _WiltStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Vertex position dengan wilt effect (sedikit bengkok ke bawah saat HP rendah)
                float3 positionOS = input.positionOS.xyz;
                float wiltFactor = _WiltAmount * _WiltStrength;
                positionOS.y -= wiltFactor * (input.positionOS.y + 0.5); // Bengkok ke bawah
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate health color gradient
                half3 healthColor;
                
                if (_HealthRatio > 0.66)
                {
                    // Interpolate between Damaged and Healthy (66% - 100%)
                    float t = (_HealthRatio - 0.66) / 0.34;
                    healthColor = lerp(_DamagedColor.rgb, _HealthyColor.rgb, t);
                }
                else if (_HealthRatio > 0.33)
                {
                    // Interpolate between Critical and Damaged (33% - 66%)
                    float t = (_HealthRatio - 0.33) / 0.33;
                    healthColor = lerp(_CriticalColor.rgb, _DamagedColor.rgb, t);
                }
                else if (_HealthRatio > 0.0)
                {
                    // Interpolate between Dead and Critical (0% - 33%)
                    float t = _HealthRatio / 0.33;
                    healthColor = lerp(_DeadColor.rgb, _CriticalColor.rgb, t);
                }
                else
                {
                    // Completely dead
                    healthColor = _DeadColor.rgb;
                }
                
                // Combine texture with health color
                half3 baseColor = texColor.rgb * healthColor;
                
                // Simple lighting (Half-Lambert)
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float NdotL = dot(normalWS, mainLight.direction);
                float halfLambert = NdotL * 0.5 + 0.5;
                
                // Apply lighting
                half3 lighting = mainLight.color * halfLambert;
                half3 ambient = half3(0.2, 0.2, 0.2); // Ambient light
                
                half3 finalColor = baseColor * (lighting + ambient);
                
                // Add specular highlight (subtle)
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float specular = pow(saturate(dot(normalWS, halfDir)), 32.0) * _Smoothness;
                finalColor += mainLight.color * specular * 0.3;
                
                half4 color = half4(finalColor, 1.0);
                
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