#ifndef FISHMOTIONVECTORS_INCLUDED
#define FISHMOTIONVECTORS_INCLUDED

#pragma exclude_renderers d3d11_9x
#pragma target 3.5
// #pragma enable_d3d11_debug_symbols

#pragma vertex vert
#pragma fragment frag

#pragma multi_compile_local _ _ENABLE_FLATTEN
#pragma shader_feature_local _ _CUSTOM_FOV

// -------------------------------------
// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#undef ENABLE_LIGHT_COOKIES

#ifndef HAVE_VFX_MODIFICATION
#pragma multi_compile _ DOTS_INSTANCING_ON
#if UNITY_PLATFORM_ANDROID || UNITY_PLATFORM_WEBGL || UNITY_PLATFORM_UWP
        #pragma target 3.5 DOTS_INSTANCING_ON
#else
#pragma target 4.5 DOTS_INSTANCING_ON
#endif
#endif // HAVE_VFX_MODIFICATION

#include "../Lib/FishEffect.hlsl"

// -------------------------------------
// Structs
struct Attributes
{
    float4 position : POSITION;
    float3 positionOld : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionCSNoJitter : TEXCOORD0;
    float4 previousPositionCSNoJitter : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// -------------------------------------
// Vertex
Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);

    #if defined(_ENABLE_FLATTEN)
        vertexInput.positionWS = FlattenWorldPos(vertexInput.positionWS);
        #if defined(_CUSTOM_FOV)
            // 1. GL(P) + GL(jitter) = VP * I_V
            // 2. GL(P) + GL(jitter) - GL(P) * V * I_V = GL(jitter) = VP * I_V - _NonJitterVP * I_V
	        // GL() means GL.GetGPUProjectionMatrix()
            float4x4 jitterMat = mul(UNITY_MATRIX_VP - _NonJitteredViewProjMatrix, UNITY_MATRIX_I_V);
            vertexInput.positionCS = CustomFOVForJitterMotionVector(vertexInput.positionWS, jitterMat);
        #else
            vertexInput.positionCS = TransformWorldToHClip(vertexInput.positionWS);
        #endif
    #endif

    // Jittered. Match the frame.
    output.positionCS = vertexInput.positionCS;

    // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
    #if defined(UNITY_REVERSED_Z)
        output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
    #else
        output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
    #endif

    float4 noJitterPosWS = mul(UNITY_MATRIX_M, input.position);
    #if defined(_ENABLE_FLATTEN)
        noJitterPosWS.xyz = FlattenWorldPos(noJitterPosWS.xyz);
        #if defined(_CUSTOM_FOV)
            output.positionCSNoJitter = CustomFOVForNoJitterMotionVector(noJitterPosWS, _CustomVPMatrix);
        #else
            output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, noJitterPosWS);
        #endif
    #else
        output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, noJitterPosWS);
    #endif

    const float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;
    float4 prevPosWS = mul(UNITY_PREV_MATRIX_M, prevPos);
    #if defined(_ENABLE_FLATTEN)
        prevPosWS.xyz = FlattenWorldPos(prevPosWS.xyz);
        #if defined(_CUSTOM_FOV)
            output.previousPositionCSNoJitter = CustomFOVForNoJitterMotionVector(prevPosWS, _PrevCustomVPMatrix);
        #else
            output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, prevPosWS);
        #endif
    #else
        output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, prevPosWS);
    #endif

    return output;
}

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
    // since uv remap functions use floats
    #define POS_NDC_TYPE float2
#else
    #define POS_NDC_TYPE half2
#endif

// -------------------------------------
// Fragment
half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        return half4(0.0, 0.0, 0.0, 0.0);
    }

    // Calculate positions
    float4 posCS = input.positionCSNoJitter;
    float4 prevPosCS = input.previousPositionCSNoJitter;

    POS_NDC_TYPE posNDC = posCS.xy * rcp(posCS.w);
    POS_NDC_TYPE prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

    half2 velocity;
    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        // Convert velocity from NDC space (-1..1) to screen UV 0..1 space since FoveatedRendering remap needs that range.
        half2 posUV = RemapFoveatedRenderingLinearToNonUniform(posNDC * 0.5 + 0.5);
        half2 prevPosUV = RemapFoveatedRenderingPrevFrameLinearToNonUniform(prevPosNDC * 0.5 + 0.5);
        
        // Calculate forward velocity
        velocity = (posUV - prevPosUV);
    #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
    #endif
    }
    else
    #endif
    {
        // Calculate forward velocity
        velocity = (posNDC.xy - prevPosNDC.xy);
        #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
        #endif

        // Convert velocity from NDC space (-1..1) to UV 0..1 space
        // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
        // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
        velocity.xy *= 0.5;
    }

    return half4(velocity, 0, 0);
}

#endif
