#ifndef FASTANDARDLIB_INCLUDE
#define FASTANDARDLIB_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.ut.rendering/Shaders/URP/Lib/UT_Effect_Lib.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/FALib/FALighting.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/FALib/FACustomFogLib.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/FALib/FaCustomSSAO.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/FALib/FADebug.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/FALib/DepthTextureUtil.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/Lib/SelfShadow-lib.hlsl"
#include "Packages/com.ut.rendering/Shaders/URP/Character/CharacterInput.hlsl"
#include "../Lib/FishEffect.hlsl"

// Water Wave
#ifdef _GERSTNERWAVE_ON
#include "Assets/Art/Shader/URP/Fish/Wave-Lib.hlsl"
#endif

struct Attributes
{
	float4 positionOS   : POSITION;
	half3 normalOS     : NORMAL;
	half4 tangentOS    : TANGENT;
	float4 uv           : TEXCOORD0;
	float4 uvLM         : TEXCOORD1;
	//UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS               : SV_POSITION;
	float2 uv                       : TEXCOORD0;
	float2 uvLM                     : TEXCOORD1;
	float3 positionWS               : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
	half3  normalWS                 : TEXCOORD3;
	// #ifdef _NORMALMAP
	half3 tangentWS				: TEXCOORD4;
	half3 bitangentWS			: TEXCOORD5;
	half4 fogColor               : TEXCOORD6;
	// #endif
	// #ifdef _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
	#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
	float4 shadowCoord			: TEXCOORD7; // compute shadow coord per-vertex for the main light
	#endif
	#if defined(_ENABLE_FLATTEN)// || defined(_CUSTOM_FOV)
	float3 originPosWS          : TEXCOORD11;
	#endif
	//#ifdef _EVOLVE_ON
	//	EVOLVE_PARAMS(7)
	//#endif
	#ifdef _EVOLVE2_ON
	float4 texcoord   : TEXCOORD8;
	half3 normalOS   : NORMAL;
	#endif
	#ifdef _IRIDESCENCE
	float2 matcapUV  : TEXCOORD9;
	#endif
	#ifdef _HOLOGRAM2_ON
	float4 positionOSAndDirect   :   TEXCOORD10; 
	#endif
	float3 color : TEXCOORD12;
		//为了防止出现不必要的计算，只能和上面的分开写
#if defined(DEBUG_DISPLAY)
	float4 tangent   : TANGENT;
	half3 NormalOS   : TEXCOORD13;
#endif
};


Varyings LitPassVertex(Attributes input)
{
	Varyings output;

	// VertexPositionInputs包括了很多空间(world, view, homogeneous clip space)的 position
	// 编译的时候会剔除未使用到的references(比如说未使用view space)
	// 这种结构具有更大的灵活性，也没有额外的消耗
	//input.positionOS = float4(input.uv.xy, 0.0, 1.0);
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

	// 和VertexPositionInputs差不多，包含了world space中的normal, tangent and bitangent
	// 如果没使用到会被剔除
	VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

#if defined(DEBUG_DISPLAY)
	output.tangent = input.tangentOS;
    output.NormalOS = input.normalOS;
	output.uv = input.uv;
	output.uvLM = input.uvLM.xy;
#else
	// TRANSFORM_TEX is the same as the old shader library.
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif


	output.positionWS = vertexInput.positionWS;
	// 目前就使用vertex input中的clip space position
	output.positionCS = vertexInput.positionCS;
	 //Computes fog factor per-vertex.
	//float fogFactor = ComputeFogFactorLinear(vertexInput.positionCS.z);

	output.normalWS = vertexNormalInput.normalWS;

	// 上面所说的新的Input结构灵活性在这里可以体现出来
	// 当一个没有定义normal map的变种存在时，tangentWS和bitangentWS不会被引用
	// 而GetVertexNormalInputs只是把normal从object转换到world space中
// #ifdef _NORMALMAP
	output.tangentWS = vertexNormalInput.tangentWS;
	output.bitangentWS = vertexNormalInput.bitangentWS;
// #endif

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
	// main light的shadow coord在vertex里计算
	// 如果应用了cascades, URP会在screen space里重构
	// 其他情况下URP会在light space(没有 depth pre-pass and shadow collect pass)里重构shadow
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif

#if defined(_GERSTNERWAVE_ON)
    Gerstner gerstner = GerstnerWave(_DirRandDirAndWaveSpeed.xy, output.positionWS, _WaveCount, _WavelengthAndWavesteepness.x, _WavelengthAndWavesteepness.y, _WavelengthAndWavesteepness.z, _WavelengthAndWavesteepness.w, _DirRandDirAndWaveSpeed.z);
    output.positionWS = gerstner.positionWS;
	float3 binormal = gerstner.binormal;
    float3 tangent = gerstner.tangent;
	output.normalWS = normalize(cross(binormal,tangent));
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.color = gerstner.positionWS.xyz;
#endif

#if defined(_ENABLE_FLATTEN)
	output.originPosWS = output.positionWS;
	output.positionWS = FlattenWorldPos(output.positionWS);
	#ifdef _CUSTOM_FOV
		output.positionCS = CustomFOV(output.positionWS);
	#else
		output.positionCS = TransformWorldToHClip(output.positionWS);
	#endif
#endif

	
//#ifdef _EVOLVE2_ON
//    float4 posWorld = float4(output.positionWS.xyz, 1);
//    float3 vertexOffset = Evolove2GetVertexOffset(posWorld);
//    float3 newPosOS = input.positionOS.xyz + vertexOffset;
    
//    VertexPositionInputs vertexOffsetInput = GetVertexPositionInputs(newPosOS);
//    output.positionCS = vertexOffsetInput.positionCS;
//    output.positionWS = vertexOffsetInput.positionWS.xyz;

//    output.texcoord = input.positionOS;
//    output.normalOS = input.normalOS;
//#endif
#ifdef _IRIDESCENCE
    output.matcapUV.x = dot(normalize(UNITY_MATRIX_IT_MV[0].xyz), normalize(input.normalOS));
    output.matcapUV.y = dot(normalize(UNITY_MATRIX_IT_MV[1].xyz), normalize(input.normalOS));
    output.matcapUV = output.matcapUV * 0.5 + 0.5;
#endif

//#ifdef _HOLOGRAM2_ON
//    float4 posOSAndDirect = Hologram2Vertex(input.positionOS.xyz, vertexInput.positionWS.xyz);
//    float3 posOS = posOSAndDirect.xyz;
//    output.positionCS = TransformObjectToHClip(posOS);
//    output.positionOSAndDirect.xyz = input.positionOS.xyz;
//    output.positionOSAndDirect.w = posOSAndDirect.w;
//#endif
	CustomMixFogColor(vertexInput.positionWS, output.fogColor.xyz, output.fogColor.w);
	return output;
}

half3 CustomFAGlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
	#if !defined(_ENVIRONMENTREFLECTIONS_OFF)
	half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

	half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(_ReflectionProbeMap, sampler_ReflectionProbeMap, reflectVector, mip);
	#if _IRIDESCENCE
	encodedIrradiance *= _IridescenceReflectionPower;
	#endif

	#if defined(UNITY_USE_NATIVE_HDR) || defined(UNITY_DOTS_INSTANCING_ENABLED)
	half3 irradiance = encodedIrradiance.rgb;
	#else
	half3 irradiance = DecodeHDREnvironment(encodedIrradiance, _ReflectionProbeMap_HDR);
	#endif
	return irradiance * occlusion;
	#endif
    
	return _GlossyEnvironmentColor.rgb * occlusion;
}

half3 CustomFAEnvironmentBRDF(FABRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm, half3 iridescenceTerm)
{
	half3 c = indirectDiffuse * brdfData.diffuse;
	float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
	#if _IRIDESCENCE
	c += indirectSpecular * surfaceReduction * lerp(brdfData.specular * iridescenceTerm, brdfData.grazingTerm, iridescenceTerm);
	#else
	c += indirectSpecular * surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
	#endif
	return c;
}

half3 CustomFAGlobalIllumination(FABRDFData brdfData, half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS, half atten, half EnvironmentReflectionIntensity, half3 iridescenceTerm)
{
	half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    
	half3 indirectDiffuse = bakedGI * occlusion;
	half3 indirectSpecular = CustomFAGlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion) * EnvironmentReflectionIntensity;
    
	half atten_power = clamp(atten, 0.35, 1);
	indirectSpecular = indirectSpecular * pow(atten_power, 1.5);
	
	half NoV = saturate(dot(normalWS, viewDirectionWS));
	half fresnelTerm = Pow4(1.0 - NoV);
	
	return CustomFAEnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm, iridescenceTerm);
}

half3 CustomDirectBRDF(FABRDFData brdfData, half3 lightDirectionWS, half3 normalWS, half3 viewDirectionWS, half3 fresnelIridescenceLight)
{
	half3 halfDir = SafeNormalize(lightDirectionWS + viewDirectionWS);
	half LoH = saturate(dot(lightDirectionWS, halfDir));
	float cosTheta1 = dot(halfDir, float3(lightDirectionWS));
	half3 specularTerm = FADirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
	
	#if _IRIDESCENCE
	specularTerm *= LoH;
	specularTerm *= fresnelIridescenceLight;
	#endif
	//限制高光不超过7，防止开了bloom的情况下，金属度和光滑度非常高的物体闪烁的情况
	half3 color = clamp(specularTerm * brdfData.specular,0,7) + brdfData.diffuse;
	return color;
}

half3 CustomFALightingPhysicallyBased(FABRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, half3 fresnelIridescenceLight = half3(1,1,1))
{
	//提高精度，规避移动平台的衰减硬边缘
	half NdotL = saturate(dot(normalWS, light.direction));
	half3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation * NdotL);
	return CustomDirectBRDF(brdfData, light.direction, normalWS, viewDirectionWS, fresnelIridescenceLight) * radiance;
}

half4 OutputStandardColor(Varyings input, FASurfaceData surfaceData, half3 cookie, half facing = 1)
{
#if defined(_ALPHATEST_ON)
	clip(surfaceData.alpha - _Cutoff);
#endif

	float3 normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));

	normalWS = normalize(normalWS) * facing;

	float3 positionWS = input.positionWS.xyz;
	half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
	

	// BRDFData保存了经过能量转换过的diffuse, specular material reflections 和 roughness
	// 如果想要自定义着色模型，替换下面的就可以了
	FABRDFData brdfData;
	InitializeFABRDFData(surfaceData, brdfData);

#if _ENABLE_SHADING_DEBUG
		return OutputDebugColor(surfaceData, brdfData);
#endif

#ifdef LIGHTMAP_ON
	// Normal is required in case Directional lightmaps are baked
	half3 bakedGI = FASampleLightmap(input.uvLM, normalWS);
#else
	#ifdef CHARACTER_AMBIENT_COLOR
		half3 bakedGI = max(0, _CharacterAmbientColor.rgb);
		//bakedGI = half3(1, 1, 1);
	#else
		// 球谐SampleSH是per-pixel的，还有SampleSHVertex and SampleSHPixel可供选择
		half3 bakedGI = SampleSH(normalWS);
	#endif
#endif

	// URP提供的light struct可以抽象shader变量
	// 其中包括了light direction, color, distanceAttenuation 和 shadowAttenuation
	// URP根据灯光和平台采用不同的着色方法
	// 禁止在shader里引用light变量，使用GetLight函数填充light struct
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
	// Main light是directional light, 有一组特定的variables和shading path
	// 假如只有一盏directional light就像下面这样
	// 当传入一个shadowcoord(per-vertex)的时候，shadowAttenuation会被计算
#if defined(_ENABLE_FLATTEN)
	Light mainLight = GetMainLight(input.shadowCoord, input.originPosWS, CalculateShadowMask(input.shadowCoord));
#else
	Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, CalculateShadowMask(input.shadowCoord));
#endif
#else
	Light mainLight = GetMainLight();
#endif
	mainLight.color *= _MainLightStrength;

	// real3 cookie = SampleMainLightCookie(input.positionWS.xyz);
	mainLight.color *= cookie;

	// 不用时编译器会优化
	float2 uvScreen = GetNormalizedScreenSpaceUV(input.positionCS);

#if defined(_CUSTOM_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor aoFactor = CustomGetScreenSpaceAmbientOcclusion(uvScreen);
    mainLight.color *= aoFactor.directAmbientOcclusion;
    surfaceData.occlusion = min(surfaceData.occlusion, aoFactor.indirectAmbientOcclusion);
#endif



	#if defined(_IRIDESCENCE) && !defined(_LOW_DETAIL)
	half3 fresnelIridescence = SAMPLE_TEXTURE2D(_IridescenceMatCap, sampler_IridescenceMatCap, input.matcapUV).rgb;
	#else
	half3 fresnelIridescence = half3(1,1,1);
	#endif

	half lightFixSign = UNITY_MATRIX_M._m23 <= 0 ? 1.0f : -1.0f;
	mainLight.direction.z *= lerp(1, lightFixSign, _EnableFixLighting);
	
	half3 fresnelTerm = half3(1, 1, 1);
#if _IRIDESCENCE
	fresnelTerm = fresnelIridescence;
#endif

	half3 color = 0;
	uint meshRenderingLayers = GetMeshRenderingLayer();

	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
	{
		#if defined (_RECEIVE_MAIN_SELF_SHADOW) && defined(_SELFSHADOW_ON) 
		#if defined(_ENABLE_FLATTEN)
		half attenuation = CalculateShadowAttenuation(_Main_SelfShadowMapRT,sampler_Main_SelfShadowMapRT,_Main_SelfShadowWorldToClip,_Main_SelfShadowParam,_Main_SelfShadowLightDirection
			,input.originPosWS, input.normalWS,_Main_SelfShadowIntensity,_Main_SelfShadowBias,half4(1,1,0,0),_Main_ShadowParam);
		#else
		half attenuation = CalculateShadowAttenuation(_Main_SelfShadowMapRT,sampler_Main_SelfShadowMapRT,_Main_SelfShadowWorldToClip,_Main_SelfShadowParam,_Main_SelfShadowLightDirection
			,input.positionWS, input.normalWS,_Main_SelfShadowIntensity,_Main_SelfShadowBias,half4(1,1,0,0),_Main_ShadowParam);
		#endif
			mainLight.shadowAttenuation = lerp(1, attenuation, _SelfShadowIntensityCtr);
			//atten = lerp(1, attenuation, _SelfShadowIntensityCtr);
		#endif
	
		// #if _IRIDESCENCE
		//     color += FALightingPhysicallyBasedIridescence(brdfData, mainLight, normalWS, viewDirectionWS, fresnelIridescence);
		// #else
		//     color += CustomFALightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);
		// #endif
		// half3 fresnelTerm = half3(1, 1, 1);
		// #if _IRIDESCENCE
		// fresnelTerm = fresnelIridescence;
		// #endif

		color += CustomFALightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS, fresnelTerm) *
			_CharacterMainLightIntensity * _CharacterMainLightColor;
	}

	// if(!_EnableURPShadowMapping)
    //     mainLight.shadowAttenuation = 1;
    
	// additionalLights的阴影具有限制
	// 如果additionalLightsRenderingMode是LightRenderingMode.PerPixel并且lightType是LightType.Spot并且可见光存在并且阴影存在，additionalLightsCastShadows才为true
	// 暂时使用主光源的阴影衰减进行计算
	half atten = clamp(mainLight.shadowAttenuation, _CombinedScaledParams.w, 1);
	//临时添加
    
	// _IgnoreMainShadowAtten 1 atten 0 
	// _IgnoreMainShadowAtten 0 atten atten
	atten = _IgnoreMainShadowAtten + atten * (-1 * _IgnoreMainShadowAtten + 1);
	// Mix diffuse GI with environment reflections.
	// Add giColor
	color += CustomFAGlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS, atten, _EnvironmentReflectionIntensity, fresnelIridescence);

	

#ifdef _FORWARD_PLUS_Z_BINING
    //方向光单独处理，因为方向光肯定会计算，不受culling影响，没必要走tile、zbin那套判断。
	bool isMatch;
	for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_LIGHTS); lightIndex++)
	{
		Light light = ForwardPlusGetAdditionalLight(lightIndex, positionWS, isMatch);
		light.color *= _AdditionalLightStrength;
		light.shadowAttenuation = 1;
		if (isMatch)
		{
	#if defined(_CUSTOM_SCREEN_SPACE_OCCLUSION)
			light.color *= aoFactor.directAmbientOcclusion;
	#endif
	#if defined (_RECEIVE_MAIN_SELF_SHADOW) && defined(_SELFSHADOW_ON) 
			if (_EnableSelfShadowMapping && _CustomShadowUseAdditionalLight)
			{
				if(lightIndex == _CustomShadowAdditionalLightIndex)
					light.shadowAttenuation = lerp(1, attenuation, _SelfShadowIntensityCtr);
			}
	#endif
			color += CustomFALightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, fresnelTerm);
		}
	}
	ClusteredLightLoop cll = ClusteredLightLoopInit(uvScreen, positionWS);
	while (ClusteredLightLoopNextWord(cll)) {
		while (ClusteredLightLoopNextLight(cll)) { 
			uint lightIndex = ClusteredLightLoopGetLightIndex(cll);
			Light light = ForwardPlusGetAdditionalLight(lightIndex, positionWS, isMatch);
			light.color *= _AdditionalLightStrength;
            light.shadowAttenuation = 1;
			if (isMatch)
			{
	#if defined(_CUSTOM_SCREEN_SPACE_OCCLUSION)
				light.color *= aoFactor.directAmbientOcclusion;
	#endif
	#if defined (_RECEIVE_MAIN_SELF_SHADOW) && defined(_SELFSHADOW_ON) 
				if (_EnableSelfShadowMapping && _CustomShadowUseAdditionalLight)
				{
					if(lightIndex == _CustomShadowAdditionalLightIndex)
						light.shadowAttenuation = lerp(1, attenuation * light.shadowAttenuation, _SelfShadowIntensityCtr);
				}
	#endif
				color += CustomFALightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, fresnelTerm);
			}
		}
	}
#else //_FORWARD_PLUS_Z_BINING
	// URP Lighting
	uint pixelLightCount = GetAdditionalLightsCount(); // Max 8
	#if defined(UNITY_PLATFORM_WEBGL)
	pixelLightCount = 0;
	#endif

	half3 addiLightCol = half3(0,0,0);

	for (uint lightIndex = 0; lightIndex < pixelLightCount; lightIndex++)
	{
		#if defined(_ENABLE_FLATTEN)
		Light light = GetAdditionalLight(lightIndex, input.originPosWS);
		#else
		Light light = GetAdditionalLight(lightIndex, input.positionWS);
		#endif
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
		{
			#if defined (_RECEIVE_ADDITIONAL_SELF_SHADOW) && defined(_SELFSHADOW_ON)
			#if defined(_ENABLE_FLATTEN)
				Light light = GetAdditionalLight(lightIndex, input.originPosWS);
			#else
				Light light = GetAdditionalLight(lightIndex, input.positionWS);
			#endif
			
			light.direction.z *= lerp(1, lightFixSign, _EnableFixLighting);

			int sceneLightIndex = GetPerObjectLightIndex(lightIndex);
			if(sceneLightIndex == _Additional_SelfShadowLightIndex)
			{
			#if defined(_ENABLE_FLATTEN)
				half additionalAttenuation = CalculateShadowAttenuation(_Additional_SelfShadowMapRT,sampler_Additional_SelfShadowMapRT,_AdditionalLightWorldToClip,
						_AdditionalLightSelfShadowParam,light.direction,input.originPosWS,
						input.normalWS,_AdditionalLightShadowIntensity,_AdditionalLightShadowBias,_AdditionalLightShadowOffset,
						_AdditionalLightShadowParam);
			#else
				half additionalAttenuation = CalculateShadowAttenuation(_Additional_SelfShadowMapRT,sampler_Additional_SelfShadowMapRT,_AdditionalLightWorldToClip,
						_AdditionalLightSelfShadowParam,light.direction,input.positionWS,
						input.normalWS,_AdditionalLightShadowIntensity,_AdditionalLightShadowBias,_AdditionalLightShadowOffset,
						_AdditionalLightShadowParam);
			#endif
				light.shadowAttenuation = lerp(1, additionalAttenuation, _AddiShadowIntensity);
			}
			#else

			int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
			
			#if defined(_ENABLE_FLATTEN)
				Light light = GetAdditionalPerObjectLight(perObjectLightIndex, input.originPosWS);
			#else
				Light light = GetAdditionalPerObjectLight(perObjectLightIndex, positionWS);
			#endif
			light.direction.z *= lerp(1, lightFixSign, _EnableFixLighting);
			
			#endif
			
			light.color *= _AdditionalLightStrength;

			//real3 cookieColor = SampleAdditionalLightCookie(lightIndex, input.positionWS);
			//light.color *= cookieColor;	


			// if(!_EnableURPShadowMapping)
			//           light.shadowAttenuation = 1;

			#if defined(_CUSTOM_SCREEN_SPACE_OCCLUSION)
				light.color *= aoFactor.directAmbientOcclusion;
			#endif
			// #if _IRIDESCENCE
			// 		//half3 fresnelIridescenceLight = SAMPLE_TEXTURE2D(_IridescenceMainLightMatCap, sampler_IridescenceLightMatCap, input.matcapUV).rgb;
			// 	color += FALightingPhysicallyBasedIridescence(brdfData, light, normalWS, viewDirectionWS, fresnelIridescence);
			// #else
			// 	color += FALightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
			// #endif
			color += CustomFALightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, fresnelTerm);
		}
	}
#endif //_FORWARD_PLUS_Z_BINING

//#endif

//#ifdef _EVOLVE2_ON
//    half4 emissionAndOpacity = Evolove2GetEmissionAndOpacity(positionWS, input.texcoord.xyz,  input.normalOS.xyz, input.normalWS, viewDirectionWS);
//    // 确保进化后原有亮度不变
//    surfaceData.emission = max(surfaceData.emission, emissionAndOpacity.rgb);
//    clip(emissionAndOpacity.a - 0.01);
//    // 确保进化后原有透明度不变
//    surfaceData.alpha = min(surfaceData.alpha, emissionAndOpacity.a);
//#endif
	// Emission
	color += surfaceData.emission;
    //color += saturate(surfaceData.emission);
	//#if _SPECULAR_SETUP
	//	return half4(1,1,1,1);
	//#else
	//	return half4(0,0,0,1);
	//#endif
	
	if(_EnableFresnel)
    {   
	#if defined(_ENABLE_FLATTEN)
        float3 camViewDirection = lerp(viewDirectionWS, SafeNormalize(_CameraPos - input.originPosWS), _FresnelLockViewDir);
	#else
		float3 camViewDirection = viewDirectionWS;
	#endif
		//camViewDirection = viewDirectionWS;

        float NdotV = saturate(dot(normalWS , camViewDirection));
        //float NdotV = saturate(dot(normalWS , half3(0,1,0)));
        half power = max(0.0001, _FresnelOriginalPower); // 防止精度引起数值出现无限大
        half rim = pow(1 - NdotV, power) * _FresnelOriginalScale;
        half3 fresnel = _FresnelOriginalColor.rgb * rim;
        color = color + fresnel;
    }

	half alpha = 1.0;
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON) // defined(_EVOLVE2_ON)
	alpha = surfaceData.alpha;	
#endif
	
#ifdef _DISSOLVE_ON
	color = DissolveColor(color, input.uv);
#endif

    half4 finalColor = half4(color, alpha);
//#ifdef _HOLOGRAM2_ON
//    finalColor = Hologram2Fragment(finalColor, input.uv, input.positionOSAndDirect.xyz, positionWS, input.normalWS, input.tangentWS, input.bitangentWS, input.positionOSAndDirect.w);
//#endif

#ifdef _ALPHAPREMULTIPLY_ON
    finalColor.rgb *= finalColor.a;
#endif
#ifndef NO_TPA
    finalColor.a *= _TPA;
#endif
	//return half4(addiLightCol,1);
	return finalColor;
}

float2 RotateUV(float2 uv, float rotationDegrees)
{
    // 定义旋转中心
    float2 center = float2(0.5, 0.5);

    float2 uvCentered = uv - center;

    float angleRad = radians(rotationDegrees);

    float s = sin(angleRad);
    float c = cos(angleRad);

    float2 rotatedCenteredUV;
    rotatedCenteredUV.x = uvCentered.x * c - uvCentered.y * s;
    rotatedCenteredUV.y = uvCentered.x * s + uvCentered.y * c;

    float2 rotatedUV = rotatedCenteredUV + center;

    return rotatedUV;
}

//只是RenderingDebugger传数据用
#if defined(DEBUG_DISPLAY)
	void FAInitializeSurfaceData(FASurfaceData FAsurfaceData,out SurfaceData surfaceData)
	{
		 surfaceData.albedo = FAsurfaceData.albedo;
		 surfaceData.specular = 0;
		 surfaceData.metallic = FAsurfaceData.metallic;
		 surfaceData.smoothness = FAsurfaceData.smoothness;
		 surfaceData.normalTS = FAsurfaceData.normalTS;
		 surfaceData.emission = FAsurfaceData.emission;
		 surfaceData.occlusion = FAsurfaceData.occlusion;
		 surfaceData.alpha = FAsurfaceData.alpha;
		 surfaceData.clearCoatMask = 0;
		 surfaceData.clearCoatSmoothness = 0;
	}

	void FAInitializeSurfaceData2(SurfaceData surfaceData,out FASurfaceData FAsurfaceData)
	{
		 FAsurfaceData.albedo = surfaceData.albedo;
		 FAsurfaceData.metallic = surfaceData.metallic;
		 FAsurfaceData.smoothness = surfaceData.smoothness;
		 FAsurfaceData.normalTS = surfaceData.normalTS;
		 FAsurfaceData.emission = surfaceData.emission;
		 FAsurfaceData.occlusion = surfaceData.occlusion;
		 FAsurfaceData.alpha = surfaceData.alpha;
	}

	void FAInitializeInputData(Varyings input, half3 normalTS, half facing, out InputData inputData)
	{
	    inputData = (InputData)0;
	
	    inputData.positionWS = input.positionWS.xyz;
	    inputData.positionCS = input.positionCS;
	
	    inputData.tangentToWorld =  half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
	
		float3 normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
	
	
	    inputData.normalWS = normalWS *facing;
	
	    inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - inputData.positionWS);
	
		float4 shadowCoord;
		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
			shadowCoord = input.shadowCoord;
		#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
			shadowCoord = TransformWorldToShadowCoord(positionWS);
		#else
			shadowCoord = float4(0, 0, 0, 0);
		#endif
	
	    inputData.shadowCoord = shadowCoord;
	
	
	
	#if UT_RENDERING
	
	        inputData.bakedGI = 0;//SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
		
	#else
	        inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogColor.w);
	        inputData.bakedGI =0;// SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
	#endif
	
	    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

	}

#endif


half4 LitPassFragment(Varyings input, half facing : VFACE) : SV_Target
{
	#if defined(_DITHER_FADEOUT)
	// 边缘光
	half3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS.xyz);
	float rim = 1.0 - saturate(dot(viewDirWS, input.normalWS));
	rim = smoothstep(1 - _ActorRimWidth, 1, rim);
	rim = smoothstep(0, _ActorRimSmoothness, rim);

	half dither = lerp(_DitherOpacity, 1, rim);
	half4 rimColor = _ActorRimColor * _ActorRimIntensity;
	
	NiloDoDitherFadeoutClip(input.positionCS.xy, dither);
	#endif

	const float2 inputUV = input.uv;
	const half3 cookie = GetMainLightCookie(input.positionWS.xyz);

	// Surfacedata包含了albedo, metallic, specular, smoothness, occlusion, emission以及alpha
	// InitializeStandarLitSurfaceData初始化是基于standard shader规则,也可以自己写函数初始化Surfacedata
	FASurfaceData FAsurfaceData = InitializeFASurfaceData(inputUV);

#if defined(DEBUG_DISPLAY)

	switch (_DebugVertexAttributeMode)
	{
	     case DEBUGVERTEXATTRIBUTEMODE_TEXCOORD0:
	         return half4(input.uv.xy, 0, 1);
	     case DEBUGVERTEXATTRIBUTEMODE_TEXCOORD1:
	         return half4(input.uvLM.xy, 0, 1);
	     case DEBUGVERTEXATTRIBUTEMODE_TEXCOORD2:
	         return half4(0, 0, 0, 1);
	     case DEBUGVERTEXATTRIBUTEMODE_TEXCOORD3:
	         return half4(0, 0, 0, 1);
	     case DEBUGVERTEXATTRIBUTEMODE_COLOR:
	         return half4(1, 1, 1, 1);
	     case DEBUGVERTEXATTRIBUTEMODE_TANGENT:
	         return half4(input.tangent);
	     case DEBUGVERTEXATTRIBUTEMODE_NORMAL:
	         return half4(input.NormalOS,1);
	}
	 
	SurfaceData surfaceData;
	InputData inputData;
	BRDFData brdfData;
	FAInitializeSurfaceData(FAsurfaceData,surfaceData);
	FAInitializeInputData(input, surfaceData.normalTS, facing, inputData);
	InitializeBRDFData(surfaceData, brdfData);
	 
#if defined(DEBUG_DISPLAY)
    SetupDebugDataTexture(inputData, input.uv, _MainTex_TexelSize, _MainTex_MipInfo, GetMipCount(TEXTURE2D_ARGS(_MainTex, smp)));
#endif

	half4 debugColor;
    //SetupDebugDataTexture(inputData, input.uvAndUvLM, _BaseMap_TexelSize, _BaseMap_MipInfo, GetMipCount(TEXTURE2D_ARGS(_BaseMap, smp)));
	if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
	{
	    return debugColor;
	}

	FAInitializeSurfaceData2(surfaceData,FAsurfaceData);

#endif

	half4 color = OutputStandardColor(input, FAsurfaceData, cookie, facing);
	
	color.rgb = lerp(input.fogColor.xyz, color.rgb, input.fogColor.w);

	#if defined(_DITHER_FADEOUT)
	return lerp(color, lerp(color, /*color * */rimColor, _ActorRimBlend), rim);
	#endif

	//return half4(_CameraPos.x,0,_CameraPos.z,1);
	return color;
}
#endif
