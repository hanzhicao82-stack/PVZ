// 动画烘焙调试 Shader - 可视化帧变化
Shader "Custom/BakedAnimationDebug"
{
    Properties
    {
        _PositionMap ("Position Map", 2D) = "black" {}
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };

            sampler2D _PositionMap;
            float _AnimationTime;
            float _FrameRate;
            int _TotalFrames;
            int _VertexCount;

            v2f vert (appdata v)
            {
                v2f o;

                // 计算当前帧
                float frame = _AnimationTime * _FrameRate;
                float frameIndex = floor(fmod(frame, (float)_TotalFrames));

                // UV 坐标
                float vertexU = (float)(v.vertexID) / max(1.0, (float)(_VertexCount - 1));
                float frameV = frameIndex / max(1.0, (float)(_TotalFrames - 1));
                
                float2 uv = float2(vertexU, frameV);

                // 从贴图读取位置
                float3 pos = tex2Dlod(_PositionMap, float4(uv, 0, 0)).xyz;

                // 应用变换
                o.vertex = UnityObjectToClipPos(float4(pos, 1.0));
                
                // 用颜色显示当前帧（红色=帧0，绿色=帧中，蓝色=帧尾）
                float frameRatio = frameIndex / max(1.0, (float)_TotalFrames);
                o.color = float3(1.0 - frameRatio, frameRatio, frac(frame));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 显示帧颜色，应该能看到颜色变化
                return fixed4(i.color, 1.0);
            }
            ENDCG
        }
    }
}
