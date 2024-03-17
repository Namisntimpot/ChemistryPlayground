Shader "MyCustom/Glass"
{
    // _BaseMap 变量在材质的 Inspector 中显示为一个名为
    // Base Map 的字段。
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness",float)=30.0
        _Cutoff("Cutoff",float)=0.5
        _Refractivity("Refractivity", float) = 1.02  // 折射率
        _Fresnel("Fresnel", float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5" 
               "Queue" = "Transparent"}

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normal       : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : VAR_POSITIONWS;
                float2 uv           : TEXCOORD1;
                float3 normalWS     : VAR_NORMALWS;
            };

            // 此宏将 _BaseMap 声明为 Texture2D 对象。
            TEXTURE2D(_BaseMap);
            //TEXTURE2D(_Skybox);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_BaseMap);
            SAMPLER(_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SpecularColor;
                float _Smoothness;
                float _Cutoff;
                float _Refractivity;
                float _Fresnel;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = mul(unity_ObjectToWorld, float4(IN.positionOS.xyz, 1.0)).xyz;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                // 获取屏幕uv.
                float4 posCS = TransformWorldToHClip(IN.positionWS);
                float2 screenUV = (ComputeScreenPos(posCS) / posCS.w).xy; 

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                
                // 视线方向.
                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);  // 从物体指向相机.
                float3 refl = reflect(-viewDir, IN.normalWS);  // 反射方向
                float3 refr = refract(-viewDir, IN.normalWS, _Refractivity);  // 折射方向. 保证结果标准化

                // 偏移screenUV
                float2 delta = (refr + viewDir).xy;
                screenUV += float2(delta.x, -delta.y);
                half4 transmission = tex2D(_CameraOpaqueTexture, clamp(screenUV, 0.0, 1.0));

                // 菲涅尔项.
                float R_theta = _Fresnel + (1-_Fresnel) * pow(1.0 - dot(viewDir, IN.normalWS), 5);
                

                // 处理光照.
                Light light = GetMainLight();
                //half3 diffuse_light = LightingLambert(light.color, light.direction, IN.normalWS);
                half3 specular_light= LightingSpecular(light.color, light.direction, normalize(IN.normalWS), viewDir, _SpecularColor, _Smoothness).rgb;
                uint pixelLightCount = GetAdditionalLightsCount();
                for(uint lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex){
                    light = GetAdditionalLight(lightIndex, IN.positionWS);
                    //diffuse_light += LightingLambert(light.color, light.direction, IN.normalWS) * 0.05;
                    specular_light += LightingSpecular(light.color, light.direction, normalize(IN.normalWS), viewDir, _SpecularColor, _Smoothness).rgb * 0.05;
                }
                // 环境光
                half3 ambient_color = _GlossyEnvironmentColor.rgb;

                //half3 color =  ambient_color;
                //return half4(color, baseMap.a * _BaseColor.a);
                return half4((1-R_theta)*(transmission.rgb * baseMap.rgb * _BaseColor.rgb + specular_light) + R_theta * ambient_color, baseMap.a * _BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Unlit/DepthOnly"
        UsePass "Universal Render Pipeline/Unlit/DepthNormalsOnly"
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}