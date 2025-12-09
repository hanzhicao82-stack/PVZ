#ifndef FISH_STANDARD_INPUT
	#define FISH_STANDARD_INPUT
	#include "Packages/com.ut.rendering/Shaders/URP/FALib/FABaseInputMacro.hlsl"
    #include "Packages/com.ut.rendering/Shaders/URP/FALib/FASceneShadowInputMacro.hlsl"
	#include "../Lib/FishEffectInput.hlsl"
	#include "Packages/com.ut.rendering/Shaders/URP/Lib/UT_Effect_Input.hlsl"


	// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
	CBUFFER_START(UnityPerMaterial)
	BASE_INPUT_BASE
    BASE_INPUT_NORMAL
    BASE_INPUT_COMBINED
    BASE_INPUT_EMISSION
    BASE_INPUT_FOG
    BASE_INPUT_ALPHACLIP
    half _Intensity;
    BASE_INPUT_FRESNEL
    BASE_INPUT_SHASOW
    BASE_INPUT_LIGHT
    BASE_INPUT_ALPHAPREMULTIPLIED

    // 五彩斑斓的白
    half _IridescenceReflectionPower;
	
	SCENE_SHADOW_INPUT

	real4 _ReflectionProbeMap_HDR;

	half _EnableFixLighting;

	// 边缘光
	half _ActorRimWidth;
	half _ActorRimSmoothness;
	half4 _ActorRimColor;
	half _ActorRimIntensity;
	half _ActorRimBlend;

	half _FresnelLockViewDir;

	//阴影强度
    //half _URPShadowIntensity;
    half _SelfShadowIntensityCtr;
    half _AddiShadowIntensity;
    half _AddiShadow2Intensity;

	// 压扁
	FISH_INPUT_EFFECT_FLATTEN

	// LightCookies
	#define ENABLE_LIGHT_COOKIES 1
	FISH_INPUT_EFFECT_LIGHT_COOKIES

	// ShadowBias
	float _TargetCastShadowHeight;
    float _HeightBias;

	//Fov
	FISH_INPUT_EFFECT_FOV

 	// Gerstner Wave
	uint _WaveCount;
	half4 _WavelengthAndWavesteepness;
	half4 _DirRandDirAndWaveSpeed;

	float _MainLightStrength;
	float _AdditionalLightStrength;

	    // Dither_FadeOut
    UT_INPUT_EFFECT_DITHER_FADEOUT
    // Dissolve, Evolve2 public
    UT_INPUT_EFFECT_DISSOLVE_PUBLIC
    // Dissolve
    UT_INPUT_EFFECT_DISSOLVE
    // Evolve2
    UT_INPUT_EFFECT_EVOLVE2
    // Hologram2
    UT_INPUT_EFFECT_HOLOGRAM2

	CBUFFER_END

	FISH_INPUT_EFFECT_FLATTEN_GLOBAL
	FISH_INPUT_EFFECT_FOV_GLOBAL

	BASE_INPUT_BASE_TEX
	BASE_INPUT_COMBINED_TEX
	BASE_INPUT_REFLECTIONPROBE_TEX
	
	// 五彩斑斓的白
	TEXTURE2D(_IridescenceMatCap);                  SAMPLER(sampler_IridescenceMatCap);
	
	UT_INPUT_EFFECT_DISSOLVE_TEX
	UT_INPUT_EFFECT_EVOLVE2_TEX
	UT_INPUT_EFFECT_HOLOGRAM2_TEX


#endif