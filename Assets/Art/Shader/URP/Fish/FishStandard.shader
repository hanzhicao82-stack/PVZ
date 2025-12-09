Shader "URP/Aquaman/FishStandard"
{
	Properties
	{
		// Specular vs Metallic 工作流
		[RequireComponent(EffectRendererCtrl, 1.5)][Preset(_, Standard_RenderingPreset)] _Mode("Rendering Mode", Float) = 0.0
		
		// Fog Mode
	    [Enum(HeightFog, 0, CustomHeightFog, 1)] _FogMode("Fog Mode", Float) = 0.0
        [ShowIf(_FogMode, Equal, 1.0)]
		_FogIntensity("Fog Intensity", Range(0.0, 1.0)) = 0.5
		
		// Two Sided
		[Enum(Both, 0, Back, 1, Front, 2)]_Cull("RenderFace", Float) = 2.0
		
		// Iridescence
		// 五彩斑斓的白
		[KWEnum(_, None, _, ThinFilm, _IRIDESCENCE)] _Iridescence("Iridescence", Float) = 0.0
		//_IridescenceThickness("Iridescence Layer Thickness", Range(0.0, 2.5)) = 0.5
		//[HideInInspector]_IridescenceReflectionCubeMap("Reflection Cube Map", Cube) = "_Skybox" {}
		[ShowIf(_Iridescence, Equal, 1.0)]
		_IridescenceReflectionPower("反射强度", Range(0.0, 10.0)) = 1.0
		[ShowIf(_Iridescence, Equal, 1.0)]
		_IridescenceMatCap("MatCap", 2D) = "white" {}
		
		// 反射球贴图
		// 环境反射探针
		// [HideInInspector]_ReflectionProbe("Actor Use Reflection Probe", float) = 1.0 
		_ReflectionProbeMap("反射球贴图", Cube) = "_Skybox"{}
		[HideInInspector]_ReflectionProbeMap_HDR("Reflection Probe HDR Param", vector) = (0,0,0,0)
		
		[Toggle] _EnableFixLighting("固定灯光方向，使其关于Z轴对称", Float) = 0
		
		// Cast Shadows
		// 是否透射阴影
		[PassSwitch(ShadowCaster)] [SubToggle(_, _)] _CastShadow("Cast URP Shadow", Float) = 1.0  // 主要用来去掉眼镜对脸产生的阴影
		
		// Ignore MainShadowAtten
		// 是否忽略主光源shadowAttenuation的影响
		[Toggle] _IgnoreMainShadowAtten("Ignore MainShadowAtten", Int) = 0
		
		//[HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0
		[HideInInspector]_TPA("__TPA", Float) = 1.0
		_EnvironmentReflectionIntensity("_Environment Reflection Intensity", Range(0.0,1.0)) = 1.0

		[MainColor]_Color("Color", Color) = (0.5,0.5,0.5,1)
		[HideInInspector]_Intensity("Intensity", Float) = 0.6
		[MainTexture]_MainTex("Albedo", 2D) = "white" {}
		
		_MainLightStrength("主光强度(MainLightStrength)", Float) = 1
		_AdditionalLightStrength("额外光强度(AdditionalLightStrength)", Float) = 1

		_Cutoff("Alpha Cutoff", Range(0.0, 0.99)) = 0.5

		_CombinedScaledParams("Scaled_Ao_Metal_Smooth", Vector) = (1.0,1.0,1.0,0.2)
		// AO贴图，R通道是AO，G通道是金属度，B通道是光滑度
		_CombinedAO("AO_Metal_Smoothess" , 2D) = "white" {}
		_Emissive_Intensity("Emissive Intensity", Range(0.0,100.0)) = 0.0
		[HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)

		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Float) = 1.0

		// [HideInInspector]_UseUV2AsAO("Use UV2 To Sample AO", Float) = 0.0

		// Blending State
		//[HideInInspector]_Surface("__surface", Float) = 0.0
		//[HideInInspector]_Blend("__blend", Float) = 0.0
		//[HideInInspector]_AlphaClip("__clip", Float) = 0.0
		[HideInInspector]_SrcBlend("__src", Float) = 1.0
		[HideInInspector]_DstBlend("__dst", Float) = 0.0
		[HideInInspector]_ZWrite("__zw", Float) = 1.0
		[HideInInspector]_ZTest("__zt", Float) = 4.0

		// 特殊效果
		[HideInInspector]_DissolvePercent("Dissolve Percent", Range(0,1)) = 0
		
		//// 进化2
		//_ModelHeight("Model Height", Float) = 2
		//[HideInInspector]_EvolveDirection("Evolve Direction", Vector) = (0, 1, 0, 0)
		//[HideInInspector]_HueOffset("Hue Offset", Range( 0 , 1)) = 0
		//[HideInInspector]_HexPattern("Hex Pattern", 2D) = "white" {}
		//[HideInInspector]_Tiling("Tiling", Float) = 1.1
		//[HideInInspector]_Falloff("Falloff", Float) = 6.54
		//[HideInInspector]_HexMaxOffset("Hex Max Offset", Float) = 0.44
		//[HideInInspector][HDR]_HexColor("Hex Color", Color) = (0,10.425,29.85706,0)
		//[HideInInspector]_DistanceMin("Distance Min", Float) = -0.45
		//[HideInInspector]_DistanceMax("Distance Max", Float) = 0.13
		//[HideInInspector][HDR]_HexColor2("Hex Color 2", Color) = (0,1.987024,15.8134,0.572549)
		//[HideInInspector]_Distance2Min("Distance 2 Min", Float) = -0.6
		//[HideInInspector]_Distance2Max("Distance 2 Max", Float) = -0.07
		//[HideInInspector]_FresnelColor_E("Fresnel Color", Color) = (0.06726903,0.6830394,1.257414,1)
		//[HideInInspector]_FresnelScale_E("Fresnel Scale", Float) = 2.67
		//[HideInInspector]_FresnelPower_E("Fresnel Power", Float) = 2.92
		//[HideInInspector]_Distance3Min("Distance 3 Min", Float) = -0.75
		//[HideInInspector]_Distance3Max("Distance 3 Max", Float) = 0.45
		//[HideInInspector]_VertexOffset("Vertex Offset", Float) = 0.1
		//[HideInInspector]_LevelsStart("Levels Start", Vector) = (0.14,1,0,1)
		//[HideInInspector]_LevelsEnd("Levels End", Vector) = (0,1,0,1)

		// 溶解
		[HideInInspector]_DissolveMask("Dissolve Mask", 2D) = "white" {}
		[HideInInspector]_RampMask("Ramp Mask", 2D) = "white" {}
		[HideInInspector]_EdgeColorLength("EdgeColor Length", Range(0,1)) = 0.079
		[HideInInspector][HDR]_EdgeColor("Edge Color", Color) = (1.8588,0.3698,0.2628,1)

		//// 全息2
		//[HideInInspector][HDR]_Hologram2_Color("Main Color", Color) = (0.620945,1.420074,3.953349,0.05098039)
  //      [HideInInspector]_Hologram2_RandomOffset("Random Offset", Float) = 100        
  //      [HideInInspector][Enum(UnityEngine.Rendering.SpaceType)] _Hologram2_PositionSpaceFeature("Position Space Feature", Float) = 0
  //      [HideInInspector][Enum(UnityEngine.Rendering.PositionAxis)] _Hologram2_PositionFeature("Position Feature", Float) = 1
  //      [HideInInspector]_Hologram2_PositionDirection("Position Direction", Float) = 1       
  //      [HideInInspector]_Hologram2_FresnelRGBScale("Fresnel Scale", Float) = 6.12
  //      [HideInInspector]_Hologram2_FresnelRGBPower("Fresnel Power", Float) = 2
  //      [HideInInspector]_Hologram2_Line1("Line 1", 2D) = "white" {}
  //      [HideInInspector]_Hologram2_Line1Speed("Line 1 Speed", Float) = -2
  //      [HideInInspector]_Hologram2_Line1Frequency("Line 1 Frequency", Float) = 40
  //      [HideInInspector]_Hologram2_Line1Hardness("Line 1 Hardness", Float) = 4.6
  //      [HideInInspector]_Hologram2_Line1InvertedThickness("Line 1 Inverted Thickness", Range( 0 , 1)) = 0.078
  //      [HideInInspector]_Hologram2_Line1Alpha("Line 1 Alpha", Float) = 0.7
  //      [HideInInspector]_Hologram2_LineGlitch("Line Glitch", 2D) = "white" {}
  //      [HideInInspector]_Hologram2_LineGlitchOffset("Line Glitch Offset", Vector) = (0.03,0,0,0)
  //      [HideInInspector]_Hologram2_LineGlitchSpeed("Line Glitch Speed", Float) = -0.26
  //      [HideInInspector]_Hologram2_LineGlitchFrequency("Line Glitch Frequency", Float) = 0.8
  //      [HideInInspector]_Hologram2_LineGlitchHardness("Line Glitch Hardness", Float) = 5
  //      [HideInInspector]_Hologram2_LineGlitchInvertedThickness("Line Glitch Inverted Thickness", Range( 0 , 1)) = 0.825
  //      [HideInInspector]_Hologram2_RandomGlitchOffset("Random Glitch Offset", Vector) = (-0.3,0,0,0)
  //      [HideInInspector]_Hologram2_RandomGlitchAmount("Random Glitch Amount", Range( 0 , 1)) = 0.063
  //      [HideInInspector]_Hologram2_RandomGlitchConstant("Random Glitch Constant", Range( 0 , 1)) = 0
  //      [HideInInspector]_Hologram2_RandomGlitchTiling("Random Glitch Tiling", Float) = 2.13
  //      [HideInInspector]_Hologram2_ColorGlitchAffect("Color Glitch Affect", Range( 0 , 1)) = 0.242
	
		_stencilRef("Stencil Ref", Float) = 4
		_stencilReadMask("Stencil ReadMask", Float) = 255
		_stencilWriteMask("Stencil WriteMask", Float) = 255
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Comparison", Float) = 8
		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOp("Stencil Op", Float) = 2

		// Dithering
		[Main(_DitheringGroup, _DITHER_FADEOUT)] _EnableDithering("Dithering (Default Off)", Float) = 0
		[Sub(_DitheringGroup)] _DitherOpacity("Dither Opacity", Range(0, 1)) = 1
		
		// LightCookie
		[Main(_LightCookieGroup, _)] _EnableLightCookies("LightCookies (Default On)", Float) = 1
		[Sub(_LightCookieGroup)] _LKMinLightColor("LightCookie Min Light Color", Range(0, 1)) = 0.75
		[Sub(_LightCookieGroup)] _LKLightStrength("LightCookie Light Strength", Range(0, 10)) = 2.5
		[Sub(_LightCookieGroup)] _LKWaveSpeed("LightCookie Wave Speed", Vector) = (0.1, 0.1, 0, 0)
		
		// URP Shadow Bias
		[Main(_ShadowBiasGroup)] _URPShadowBias("URP Shadow Bias (Default Off)", Float) = 0 //_URP_SHADOW_CAST_BIAS
		[Sub(_ShadowBiasGroup)] _TargetCastShadowHeight("Target Height When Casting Shadow", Float) = 20
		[Sub(_ShadowBiasGroup)] _HeightBias("HeightBias", Float) = 0
		
		// Flatten
		[Main(_FlattenGroup, _ENABLE_FLATTEN)] _EnableFlatten("Flatten (Default Off)", Float) = 0
		[Sub(_FlattenGroup)] _FlattenWorldOriginPos("Flatten World Origin Pos", Vector) = (0, 0, 0, 0)
		[Sub(_FlattenGroup)] _FlattenMaxHeight("Flatten Object Origin Max Height", Float) = 1
		[Sub(_FlattenGroup)] _FlattenPlaneOffset("Flatten Plane Offset", Float) = 0
		[Sub(_FlattenGroup)] _FlattenFactor("Flatten Factor", Float) = 0.1
		[Sub(_FlattenGroup)] _CameraPos("Camera Position", Vector) = (0, 150, 0, 0)
		
		//Custom Fov
		[Main(_FovGroup, _CUSTOM_FOV)] _Enable_Custom_Fov("Enable Custom Fov (Default Off)", Float) = 0
		[Sub(_FovGroup)] _BiasPos("Bias Possition", Vector) = (0, 0, 0)
		
        // 菲尼尔
        [Main(_FresnelGroup)] _EnableFresnel("Fresnel (Default Off)", Float) = 0
		[SubToggle(_FresnelGroup)] _FresnelLockViewDir("Fresnel Lock View Direction(Upward)", Float) = 0
        [Sub(_FresnelGroup)][HDR] _FresnelOriginalColor("Fresnel Color", Color) = (0,0,0,1) 
        [Sub(_FresnelGroup)] _FresnelOriginalPower("Fresnel Power", Float) = 0
        [Sub(_FresnelGroup)] _FresnelOriginalScale("Fresnel Scale", Float) = 0

		// 自阴影设置
		[Main(_SelfShadowMappingSettingGroup)]_EnableSelfShadowMapping("Can Receive Self Shadow? (Default On)", Float) = 1
		[Sub(_SelfShadowMappingSettingGroup)]_SelfShadowIntensityCtr("_SelfShadowIntensity(Default 1)", Range(0,1)) = 1
		//[Sub(_SelfShadowMappingSettingGroup)]_SelfShadowMappingDepthBias("_SelfShadowMappingDepthBias(Default 0)", Range(0,0.2)) = 0
		[Main(_AddiSelfShadowMappingSettingGroup)]_EnableAddiSelfShadowMapping("Can Receive Additional Self Shadow? (Default On)", Float) = 1
		[Sub(_AddiSelfShadowMappingSettingGroup)]_AddiShadowIntensity("_AdditionalShadowIntensity(Default 1)", Range(0,1)) = 1
		//[Main(_URPShadowMappingSettingGroup)]_EnableURPShadowMapping("Can Receive URP Shadow? (Default On)", Float) = 1
		//[Sub(_URPShadowMappingSettingGroup)]_URPShadowIntensity("_URPShadowIntensity(Default 1)", Range(0,1)) = 1	
		[PassSwitch(SelfShadowCaster)] [Main(_SelfShadowCasterGroup, _SELFSHADOW_ON)] _EnableSelfShadowCaster ("Can Cast Self Shadow? (Default Off)", Float) = 0
		// 边缘光
		[HideInInspector]_ActorRimWidth("Rim Width", Float) = 0
        [HideInInspector]_ActorRimSmoothness("Rim Smoothness", Float) = 0
		[HideInInspector][HDR]_ActorRimColor("Rim Color", color) = (1,1,1,1)
		[HideInInspector]_ActorRimIntensity("Rim Intensity", Float) = 0
		[HideInInspector]_ActorRimBlend("Rim Blend", Float) = 0
	
		// Wate Wave
		[Main(_AddiWaterWaveSettingGroup, _GERSTNERWAVE_ON)]_EnableAddGerstnerWave("Can Use Gerstner Wave? (Default Off)", Float) = 0
		[SubIntRange(_AddiWaterWaveSettingGroup)]_WaveCount("Wave Count(Default 1)", Range(0,20)) = 16
		[Sub(_AddiWaterWaveSettingGroup)] _WavelengthAndWavesteepness("X: Wavelength Max, Y: Wavelength Min, Z: Wavesteepness Max, W: Wavesteepness Min", Vector) = (5.0, 0.0, 3.0, 0.0)
		[Sub(_AddiWaterWaveSettingGroup)] _DirRandDirAndWaveSpeed("XY: Direction, Z: Random Direction, W: Wave Speed", Vector) = (1.0, 1.0, 1.0, 1.0)
	}

	HLSLINCLUDE
	#define CHARACTER_AMBIENT_COLOR 1
	//#define _DITHER_FADEOUT 1
	ENDHLSL

	SubShader
	{
		// SRP的shader里subshader有一个新的"RenderPipeline" tag , 允许创建匹配多个render pipeline的shader
		// 如果没有设定这个tag, subshader会匹配任何render pipeline, 假如只想让其在URP运行，就把tag设定为
		// "UniversalRenderPipeline"
		Tags {
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
			//"IgnoreProjector" = "True"
		}
		LOD 50

		// FORWARD pass, Shades GI, emission, fog和所有灯光都在一个pass里
		// 和内置的forward renderer比较的话，URP forward renderer渲染一个带有很多灯光的场景drawcall 和 overdraw数量会更少

		Pass
		{
			Name "Character"
			Tags { "LightMode" = "Character" }

			Blend [_SrcBlend] [_DstBlend]
			ZTest [_ZTest]
			ZWrite [_ZWrite]
			Cull [_Cull]
			Stencil
			{
				Ref [_stencilRef]
				ReadMask  [_stencilReadMask]
				WriteMask [_stencilWriteMask]
				Comp [_StencilComp]
				Pass [_StencilOp]
			}

			HLSLPROGRAM

			// 需要standard SRP library编译gles 2.0
			// 所有shader必须用HLSLcc编译，默认情况下只有gles不使用HLSLcc
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// --------------------------------------
			// Material Keywords
			// unused shader_feature variants are stripped from build automatically
			//#pragma shader_feature _NORMALMAP
			#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _EMISSION
			//#pragma shader_feature _METALLICSPECGLOSSMAP
			//#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			//#pragma shader_feature _OCCLUSIONMAP

			//#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
			//#pragma shader_feature _GLOSSYREFLECTIONS_OFF
			//#pragma shader_feature _SPECULAR_SETUP
			//#pragma shader_feature _RECEIVE_SHADOWS_OFF

			// --------------------------------------
			// URP keywords
			// When doing custom shaders you most often want to copy and past these #pragmas
			// These multi_compile variants are stripped from the build depending on:
			// 1) Settings in the URP Asset assigned in the GraphicsSettings at build time
			// e.g If you disable AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
			// will be stripped from build
			// 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
			// but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS
			//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile_fragment _ _CUSTOM_SCREEN_SPACE_OCCLUSION
			//#pragma multi_compile _ _SHADOWS_SOFT
			//#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			//#pragma multi_compile _ _FORWARD_PLUS

			//#pragma multi_compile_fragment _ _LIGHT_COOKIES
			//#pragma multi_compile _ _LIGHT_LAYERS

			// -------------------------------------
			// Unity defined keywords
			//#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			//#pragma multi_compile _ LIGHTMAP_ON
			//#pragma multi_compile_fog

			// --------------------------------------
			// 角色特效
			#pragma multi_compile_local _ _DISSOLVE_ON _DITHER_FADEOUT
			// #pragma multi_compile_local _ _DISSOLVE_ON _EVOLVE2_ON _HOLOGRAM2_ON
			#pragma shader_feature_local _ _IRIDESCENCE
			// #pragma multi_compile_local _ _ACTOR_USE_REFLECTION_PROBE
			
			#pragma multi_compile_local _ _ENABLE_FLATTEN
			#pragma shader_feature_local _ _CUSTOM_FOV

			//#pragma multi_compile_local _ _SELFSHADOW_ON
			#pragma shader_feature_local _ _SELFSHADOW_ON

            #pragma multi_compile _ _RECEIVE_MAIN_SELF_SHADOW
            #pragma multi_compile _ _RECEIVE_ADDITIONAL_SELF_SHADOW
			// @KeywordRule: REQUIRED _RECEIVE_MAIN_SELF_SHADOW => _SELFSHADOW_ON
			// @KeywordRule: REQUIRED _RECEIVE_ADDITIONAL_SELF_SHADOW => _SELFSHADOW_ON
			
			// #pragma shader_feature _ _FORWARD_PLUS_Z_BINING

			// Gerstner Wave
			#pragma shader_feature_local _ _GERSTNERWAVE_ON
			
			//编辑器下变体
			#pragma shader_feature _ _EDITOR_HIGH_SOFT_SHADOW
            #pragma shader_feature _ DEBUG_DISPLAY

			// @KeywordRule: REQUIRED _CUSTOM_FOV => _ENABLE_FLATTEN

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "FishStandard-Input.hlsl"
			#include "FishStandard-Lib.hlsl"
			ENDHLSL
		}


        Pass
        {
            Name "Object Motion Vectors"

            // Lightmode tag required setup motion vector parameters by C++ (legacy Unity)
            Tags
            {
                "LightMode" = "MotionVectors"
            }

            HLSLPROGRAM
            
			#include "FishStandard-Input.hlsl"
            #include "../Lib/FishMotionVectors.hlsl"
            
            ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			//--------------------------------------
			// GPU Instancing
			//#pragma multi_compile_instancing

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma multi_compile_local _ _DITHER_FADEOUT _DISSOLVE_ON
			//#pragma multi_compile_local_vertex _ _URP_SHADOW_CAST_BIAS
			//#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#include "FishStandard-Input.hlsl"
			#include "Packages/com.ut.rendering/Shaders/URP/FALib/FAShadowCasterPass.hlsl"
			//#include "FishShadowCasterPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			//--------------------------------------
			// GPU Instancing
			//#pragma multi_compile_instancing

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			//#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			#include "FishStandard-Input.hlsl"
			#include "Packages/com.ut.rendering/Shaders/URP/FALib/FADepthOnlyPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"
			Tags{"LightMode" = "DepthNormals"}

			ZWrite On
			Cull[_Cull]

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			// -------------------------------------
			// Material Keywords
			//#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			//#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			//#pragma multi_compile_instancing

			#include "FishStandard-Input.hlsl"
			#include "Packages/com.ut.rendering/Shaders/URP/FALib/FADepthNormalsPass.hlsl"
			ENDHLSL
		}
		
		Pass
		{
		    Name "SelfShadowCaster"
		    Tags{"LightMode" = "SelfShadowCaster"}
		    
		    ZWrite [_ZWrite]
		    ZTest LEqual
		    ColorMask 0
		    Cull [_Cull]
		    
		    HLSLPROGRAM
			
			#pragma multi_compile_local _ _ALPHATEST_ON
			#pragma multi_compile_local _ _DITHER_FADEOUT _DISSOLVE_ON _EVOLVE2_ON 
		    #pragma vertex LitPassVertex
            #pragma fragment SelfShadowPassFragment // we only need to do Clip(), no need color shading
            
            #define CharSelfShadowCasterPass 1
            #include "FishStandard-Input.hlsl"
            #include "Packages/com.ut.rendering/Shaders/URP/FALib/FAPBRLib.hlsl"

		    ENDHLSL
        }
	}
	
	// Uses a custom shader GUI to display settings. Re-use the same from Lit shader as they have the same properties.
	CustomEditor "LWGUI.LWGUI"
}