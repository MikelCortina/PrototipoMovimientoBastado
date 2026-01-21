Shader "Boomer/QuantizedLighting_PlayerLight"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LightSteps ("Light Steps", Range(1,6)) = 3
        _MinLight ("Minimum Light", Range(0,1)) = 0.15
        _PlayerPos ("Player Position", Vector) = (0,0,0,0)
        _PlayerIntensity ("Player Light Intensity", Range(0,5)) = 1
        _PlayerRadius ("Player Light Radius", Range(0,10)) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 worldPosWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _Color;
            float _LightSteps;
            float _MinLight;

            float3 _PlayerPos;
            float _PlayerIntensity;
            float _PlayerRadius;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.worldPosWS = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 normal = normalize(i.normalWS);
                float totalLight = 0;

                // === MAIN LIGHT ===
                Light mainLight = GetMainLight();
                float3 mainDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normal, -mainDir));
                totalLight += NdotL;

                // === ADDITIONAL LIGHTS ===
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint li = 0; li < lightCount; li++)
                {
                    Light light = GetAdditionalLight(li, i.worldPosWS);

                    float3 lightDir;
                    float attenuation = 1.0;

                    if (light.lightType == LightType_Point)
                    {
                        lightDir = normalize(light.position - i.worldPosWS);
                        float dist = length(light.position - i.worldPosWS);
                        attenuation = saturate(1.0 - dist / light.range);
                    }
                    else if (light.lightType == LightType_Spot)
                    {
                        lightDir = normalize(light.position - i.worldPosWS);
                        float dist = length(light.position - i.worldPosWS);
                        attenuation = saturate(1.0 - dist / light.range);
                    }
                    else
                    {
                        continue;
                    }

                    float NdotL_add = saturate(dot(normal, lightDir));
                    totalLight += NdotL_add * attenuation;
                }
                #endif

                // === PLAYER LIGHT ===
                float3 toPlayer = _PlayerPos - i.worldPosWS;
                float distPlayer = length(toPlayer);
                float playerAtten = saturate(1.0 - distPlayer / _PlayerRadius);
                float NdotPlayer = saturate(dot(normal, normalize(toPlayer)));
                totalLight += NdotPlayer * playerAtten * _PlayerIntensity;

                // === CUANTIZACIÓN ===
                float steps = max(1, _LightSteps);
                float quantizedLight = floor(totalLight * steps) / steps;
                quantizedLight = max(quantizedLight, _MinLight);

                // === TEXTURA ===
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _Color;
                albedo.rgb *= quantizedLight;

                return albedo;
            }

            ENDHLSL
        }
    }
}
