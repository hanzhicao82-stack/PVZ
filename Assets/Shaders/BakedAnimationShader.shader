// 动画烘焙 GPU 动画 Shader
// 使用烘焙的贴图播放动画
Shader "Custom/BakedAnimationShader"
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint vertexID : SV_VertexID; // 使用顶点 ID
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            sampler2D _PositionMap;
            sampler2D _NormalMap;
            
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _FrameRate)
                UNITY_DEFINE_INSTANCED_PROP(int, _TotalFrames)
                UNITY_DEFINE_INSTANCED_PROP(int, _VertexCount)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // 获取实例化参数
                float animTime = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationTime);
                float frameRate = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameRate);
                int totalFrames = UNITY_ACCESS_INSTANCED_PROP(Props, _TotalFrames);
                int vertexCount = UNITY_ACCESS_INSTANCED_PROP(Props, _VertexCount);

                // 计算当前帧
                float frame = animTime * frameRate;
                float frameIndex = floor(frame);
                float frameFrac = frac(frame);
                
                // 确保帧索引在有效范围内
                frameIndex = fmod(frameIndex, (float)totalFrames);

                // 计算下一帧
                float nextFrameIndex = fmod(frameIndex + 1.0, (float)totalFrames);

                // UV 坐标：直接使用顶点 ID，每个 Mesh 都有独立贴图
                // 确保 vertexID 在有效范围内
                uint safeVertexID = min(v.vertexID, (uint)(vertexCount - 1));
                
                // 添加半个像素偏移以正确采样像素中心
                float pixelOffsetU = 0.5 / (float)vertexCount;
                float pixelOffsetV = 0.5 / (float)totalFrames;
                
                float vertexU = ((float)safeVertexID + 0.5) / (float)vertexCount;
                float frameV = (frameIndex + 0.5) / (float)totalFrames;
                float frameVNext = (nextFrameIndex + 0.5) / (float)totalFrames;
                
                // 钳制 UV 到 [0, 1] 范围
                float2 uv = float2(saturate(vertexU), saturate(frameV));
                float2 uvNext = float2(saturate(vertexU), saturate(frameVNext));

                // 从贴图读取位置
                float3 pos = tex2Dlod(_PositionMap, float4(uv, 0, 0)).xyz;
                float3 posNext = tex2Dlod(_PositionMap, float4(uvNext, 0, 0)).xyz;

                // 插值
                float3 finalPos = lerp(pos, posNext, frameFrac);
                
                // 调试：如果位置异常，使用原始顶点位置
                if (any(isnan(finalPos)) || any(isinf(finalPos)))
                {
                    finalPos = v.vertex.xyz;
                }

                // 从贴图读取法线
                float3 normal = tex2Dlod(_NormalMap, float4(uv, 0, 0)).xyz;
                float3 normalNext = tex2Dlod(_NormalMap, float4(uvNext, 0, 0)).xyz;
                
                // 法线从 [0,1] 转换到 [-1,1]
                normal = normal * 2.0 - 1.0;
                normalNext = normalNext * 2.0 - 1.0;
                
                float3 finalNormal = normalize(lerp(normal, normalNext, frameFrac));

                // 应用变换
                o.vertex = UnityObjectToClipPos(float4(finalPos, 1.0));
                o.worldNormal = UnityObjectToWorldNormal(finalNormal);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // 简单光照
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(i.worldNormal, lightDir));
                
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= NdotL * 0.8 + 0.2; // 添加环境光
                
                return col;
            }
            ENDCG
        }
    }
}
