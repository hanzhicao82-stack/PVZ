// 动画烘焙 GPU 动画 Shader - 修复版
// 正确处理局部坐标到世界坐标的转换
Shader "Custom/BakedAnimationShaderFixed"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PositionMap ("Position Map", 2D) = "black" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _AnimationTime ("Animation Time", Float) = 0
        _FrameRate ("Frame Rate", Float) = 30
        _TotalFrames ("Total Frames", Int) = 60
        _VertexCount ("Vertex Count", Int) = 100
        [Toggle] _DebugPosDiff ("Debug Position Difference", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 debugColor : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _PositionMap;
            sampler2D _NormalMap;
            float _AnimationTime;
            float _FrameRate;
            int _TotalFrames;
            int _VertexCount;
            float _DebugPosDiff;

            v2f vert (appdata v)
            {
                v2f o;

                // 计算当前帧
                float frame = _AnimationTime * _FrameRate;
                float frameIndex = floor(fmod(frame, (float)_TotalFrames));
                float frameFrac = frac(frame);

                // 计算下一帧
                float nextFrameIndex = fmod(frameIndex + 1.0, (float)_TotalFrames);

                // UV 坐标
                float vertexU = (float)(v.vertexID) / max(1.0, (float)(_VertexCount - 1));
                float frameV = frameIndex / max(1.0, (float)(_TotalFrames - 1));
                float frameVNext = nextFrameIndex / max(1.0, (float)(_TotalFrames - 1));
                
                float2 uv = float2(vertexU, frameV);
                float2 uvNext = float2(vertexU, frameVNext);

                // 从贴图读取位置（局部坐标）
                float3 localPos = tex2Dlod(_PositionMap, float4(uv, 0, 0)).xyz;
                float3 localPosNext = tex2Dlod(_PositionMap, float4(uvNext, 0, 0)).xyz;

                // 插值得到最终局部坐标
                float3 finalLocalPos = lerp(localPos, localPosNext, frameFrac);

                // 调试：计算位置差异
                float posDiff = length(localPosNext - localPos);

                // 从贴图读取法线（局部空间）
                float3 localNormal = tex2Dlod(_NormalMap, float4(uv, 0, 0)).xyz;
                float3 localNormalNext = tex2Dlod(_NormalMap, float4(uvNext, 0, 0)).xyz;
                
                // 法线从 [0,1] 转换到 [-1,1]
                localNormal = localNormal * 2.0 - 1.0;
                localNormalNext = localNormalNext * 2.0 - 1.0;
                
                float3 finalLocalNormal = normalize(lerp(localNormal, localNormalNext, frameFrac));

                // 局部坐标转世界坐标
                float4 worldPos = mul(unity_ObjectToWorld, float4(finalLocalPos, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                
                // 法线转世界空间
                o.worldNormal = UnityObjectToWorldNormal(finalLocalNormal);
                o.uv = v.uv;

                // 调试：用颜色显示帧变化或位置差异
                if (_DebugPosDiff > 0.5)
                {
                    // 显示位置差异（红色=有移动，蓝色=无移动）
                    o.debugColor = float3(posDiff * 10.0, 0, 1.0 - posDiff * 10.0);
                }
                else
                {
                    // 显示帧变化
                    o.debugColor = float3(frameFrac, frameIndex / (float)_TotalFrames, 0);
                }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 简单光照
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(normalize(i.worldNormal), lightDir));
                
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= NdotL * 0.8 + 0.2;
                
                // 添加一点帧变化的可视化（用于调试）
                col.rgb += i.debugColor * 0.3;
                
                return col;
            }
            ENDCG
        }
    }
}
