#ifndef FISHEFFECTINPUT_INCLUDED
#define FISHEFFECTINPUT_INCLUDED

// Property Template
// // 角色特效
// #pragma multi_compile_local _ _DISSOLVE_ON
// #pragma multi_compile_local _ _ENABLE_FLATTEN
// #pragma shader_feature_local _ _CUSTOM_FOV
//
// // Flatten
// [Main(_FlattenGroup, _ENABLE_FLATTEN)] _EnableFlatten("Flatten (Default Off)", Float) = 0
// [Sub(_FlattenGroup)] _FlattenWorldOriginPos("Flatten World Origin Pos", Vector) = (0, 0, 0, 0)
// [Sub(_FlattenGroup)] _FlattenMaxHeight("Flatten Object Origin Max Height", Float) = 1
// [Sub(_FlattenGroup)] _FlattenPlaneOffset("Flatten Plane Offset", Float) = 0
// [Sub(_FlattenGroup)] _FlattenFactor("Flatten Factor", Float) = 0.1
// [Sub(_FlattenGroup)] _CameraPos("Camera Position", Vector) = (0, 150, 0, 0)
//
// //Custom Fov
// [Main(_FovGroup, _CUSTOM_FOV)] _Enable_Custom_Fov("Enable Custom Fov (Default Off)", Float) = 0
// [Sub(_FovGroup)] _BiasPos("Bias Possition", Vector) = (0, 0, 0)
//
// // LightCookie
// [Main(_LightCookieGroup, _)] _EnableLightCookies("LightCookies (Default On)", Float) = 1
// [Sub(_LightCookieGroup)] _LKMinLightColor("LightCookie Min Light Color", Range(0, 1)) = 0.75
// [Sub(_LightCookieGroup)] _LKLightStrength("LightCookie Light Strength", Range(0, 10)) = 2.5
// [Sub(_LightCookieGroup)] _LKWaveSpeed("LightCookie Wave Speed", Vector) = (0.1, 0.1, 0, 0)
//
// // 溶解
// [Hidden]_DissolvePercent("Dissolve Percent", Range(0,1)) = 0
// [Hidden]_DissolveMask("Dissolve Mask", 2D) = "white" {}
// [Hidden]_RampMask("Ramp Mask", 2D) = "white" {}
// [Hidden]_EdgeColorLength("EdgeColor Length", Range(0,1)) = 0.079
// [Hidden][HDR]_EdgeColor("Edge Color", Color) = (1.8588,0.3698,0.2628,1)

#define FISH_INPUT_EFFECT_FLATTEN \
	float _FlattenPlaneOffset; \
	float _FlattenFactor; \
	float3 _FlattenWorldOriginPos; \
	float _FlattenMaxHeight; \
	float3 _CameraPos;

#define FISH_INPUT_EFFECT_FOV \
	float3 _BiasPos; \
	float4x4 _CustomVPMatrix; \
	float4x4 _PrevCustomVPMatrix; \
	float4x4 _CustomViewMatrix;

// LightCookies
// 配合 #define ENABLE_LIGHT_COOKIES 1 使用
#define FISH_INPUT_EFFECT_LIGHT_COOKIES \
	half _EnableLightCookies; \
	half _LKMinLightColor; \
	half _LKLightStrength; \
	half4 _LKWaveSpeed; 

// 溶解
#define FISH_INPUT_EFFECT_DISSOLVE_TEX \
	TEXTURE2D(_DissolveMask);				SAMPLER(sampler_DissolveMask); \
	TEXTURE2D(_RampMask);					SAMPLER(sampler_RampMask);
#define FISH_INPUT_EFFECT_DISSOLVE \
	half	_DissolvePercent; \
	float4	_DissolveMask_ST; \
	half	_EdgeColorLength; \
	half4	_EdgeColor; \
	float4	_RampMask_ST;

#define FISH_INPUT_EFFECT_FLATTEN_GLOBAL \
	float3 _RealCameraPos; \
	float3 _CameraForward;

#define FISH_INPUT_EFFECT_FOV_GLOBAL \
	float4x4 _JitterMatrix;

#endif
