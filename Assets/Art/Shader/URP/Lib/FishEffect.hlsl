#ifndef FISHEFFECT_INCLUDED
#define FISHEFFECT_INCLUDED

#include "Packages/com.ut.rendering/Shaders/URP/Lib/UTShaderUtils.hlsl"

// @KeywordRule: REQUIRED _CUSTOM_FOV => _ENABLE_FLATTEN

#ifdef _ENABLE_FLATTEN
// 压扁
float3 FlattenWorldPos(float3 positionWS)
{
    // 1.计算顶点沿着相机朝向需要前进的距离，然后获得该类似正交视角的顶点。
    // 2.计算顶点沿着相机视线方向需要前进的距离，并根据正交顶点和原顶点求出透视视角下顶点的期望位置。
    float3 flattenDirection = normalize(_CameraForward);
    float3 camViewDirection = normalize(positionWS - _CameraPos);
	float3 originPos = _CameraPos + flattenDirection * (dot(-_CameraPos, flattenDirection) - _FlattenPlaneOffset);
    float flattenProjectionDistance = dot(positionWS - originPos, flattenDirection);
    float safeNormViewProjFlatten = abs(dot(flattenDirection, camViewDirection));
    float viewProjectionDistance = flattenProjectionDistance / safeNormViewProjFlatten;
    float3 viewProjection = viewProjectionDistance * camViewDirection;
    float diffProjFromPos2OriginInObjSpace = dot(flattenDirection, _FlattenWorldOriginPos - positionWS);
    float invViewProjDis = diffProjFromPos2OriginInObjSpace / safeNormViewProjFlatten / _FlattenMaxHeight;
    float3 flattenedVertex = positionWS - viewProjection;
    flattenedVertex -= camViewDirection * invViewProjDis * _FlattenFactor;
	flattenedVertex.xz -= (_CameraPos.xz - _RealCameraPos.xz) * ((_FlattenPlaneOffset - _FlattenWorldOriginPos.y) / (_CameraPos.y - _FlattenWorldOriginPos.y));
	// 通用，非仅Y轴压扁使用
	// float3 projectWorldOriginPos = _CameraPos + flattenDirection * dot(_FlattenWorldOriginPos - _CameraPos, flattenDirection);
	// flattenedVertex -= (_CameraPos - _RealCameraPos) * (length(originPos - projectWorldOriginPos) / length(_CameraPos - projectWorldOriginPos));
    positionWS = flattenedVertex;
    return positionWS;
}
#endif

#ifdef _CUSTOM_FOV
// 自定义FOV，依赖压扁
float4 CustomFOV(float3 positionWS)
{
	positionWS.xz += _BiasPos.xz;
#ifndef SHADER_API_GLES3
	_JitterMatrix[1][3] *= -1.0;
#endif
	float4 fovPositionCS = mul(mul(_JitterMatrix, _CustomVPMatrix), float4(positionWS, 1));
	
	return fovPositionCS;
}

float4 CustomFOVForJitterMotionVector(float3 positionWS, float4x4 jitterMat)
{
	positionWS.xz += _BiasPos.xz;
	// GL(CustomP + jitter) * CustomV = GL(CustomP) * CustomV + GL(jitter) * CustomV
	//								  = CustomVP + GL(jitter) * CustomV
	// GL() means GL.GetGPUProjectionMatrix()
	float4x4 jitterCustomVP = _CustomVPMatrix + mul(jitterMat, _CustomViewMatrix);
	float4 fovPositionCS = mul(jitterCustomVP, float4(positionWS, 1));
	
	return fovPositionCS;
}

float4 CustomFOVForNoJitterMotionVector(float4 positionWS, float4x4 noJitterVPMat)
{
	positionWS.xz += _BiasPos.xz;
	float4 fovPositionCS = mul(noJitterVPMat, positionWS);

	return fovPositionCS;
}
#endif

#if defined(ENABLE_LIGHT_COOKIES)
half _GlobalEnableLightCookies;

real3 SampleMainLightCookieForFish(float3 samplePositionWS, half mainLightCookieEnabled)
{
	if(step(mainLightCookieEnabled, 0.5))
		return real3(1,1,1);

	float2 uv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, samplePositionWS, float4(1, 1, 0, 0), URP_TEXTURE_WRAP_MODE_NONE);
	real4 color = SampleMainLightCookieTexture(uv + fmod(_Time.y, 10) * _LKWaveSpeed.xy);

	return IsMainLightCookieTextureRGBFormat() ? color.rgb
			 : IsMainLightCookieTextureAlphaFormat() ? color.aaa
			 : color.rrr;
}
#endif

half3 GetMainLightCookie(float3 positionWS)
{
#if defined(ENABLE_LIGHT_COOKIES)
	const half mainLightCookieEnabled = IsMainLightCookieEnabled() * _EnableLightCookies * _GlobalEnableLightCookies;
	half3 cookie = SampleMainLightCookieForFish(positionWS, mainLightCookieEnabled) * _LKLightStrength;
	cookie = max(cookie, half3(_LKMinLightColor, _LKMinLightColor, _LKMinLightColor));
#else
	half3 cookie = half3(1, 1, 1);
#endif
	return cookie;
}



#ifdef _HOLOGRAM_ON
		float3 GetGlitchPos(float3 sourcePos)
		{
			half4 glitchOffset = _Hologram_GlitchOffset;
			half glitchSpeed = _Hologram_GlitchSpeed;
			half glitchTiling = _Hologram_GlitchTiling;
			half glitchInterval = _Hologram_GlitchInterval;
			half glitchConstant = _Hologram_GlitchConstant;
			float3 objOffset = mul(UNITY_MATRIX_T_MV, glitchOffset).xyz;
			float3 objScale = 1.0 / float3(length(unity_WorldToObject[0].xyz), length(unity_WorldToObject[1].xyz), length(unity_WorldToObject[2].xyz));

			float mulTime1 = _Time.y * glitchSpeed;
			float mulTime2 = _Time.y * glitchSpeed + 0.2;
			float2 tmpPos1 = float2((sourcePos.x*glitchTiling + mulTime1), mulTime2);
			float s1 = perlin_noise(tmpPos1) * 0.5 + 0.5;

			float2 tmpPos2 = float2(mulTime1, mulTime2);
			float s2 = (perlin_noise(tmpPos2) * 0.5 + 0.5) * cos(mulTime1 * (1 / glitchInterval));
			s2 = clamp(((s2 + glitchConstant) * 2 - 1), 0.0, 1.0);

			float s0 = (s1 * 2 - 1) * s2;

			float3 staticSwitch = (objOffset / objScale) * s0; // * _Hologram_GlitchAmount;
			float3 glitch_pos = sourcePos + staticSwitch;

			return glitch_pos;
		}

		half3 GetHologramColor(half3 sourceColor, float3 posWS, half rim)
		{

			float t = fmod(_Time.y, 10);
			half frequency = _Hologram_SLine1Frequency;
			half speed = _Hologram_SLine1Speed;
			half4 hologramColor = _Hologram_Color;
			half sLine1alpha = _Hologram_SLine1Alpha;
			half y = posWS.y * frequency + t * speed;
			half line_scale = lerp(0, 1, abs((y - floor(y)) - 0.5) /** _Hologram_SLine1Hardness*/);
			half4 line1_color = half4(hologramColor.rgb * line_scale, sLine1alpha * line_scale);

			//half splash = (cos(t * _Hologram_SplashFrequency) + 2) / 3;
			//half splash_alpha = lerp(1 - _Hologram_SplashLevel, 1, splash);

			half3 color = sourceColor.rgb * _Intensity + (hologramColor.rgb *hologramColor.a + line1_color.rgb * line1_color.a) * max(0, (1 - rim));
			// float final_alpha = sourceColor.a; // * clamp((hologramColor.a + line1_color.a), 0.0, 1.0) * splash_alpha * _Hologram_Alpha;

			return color;
		}
#endif

#if defined(_PERCENTEFFECT_ON)
		half GetPercentEffectFactor(float3 posWorld)
		{
			// 过渡动画
			half4 modelFootPos = half4(0, 0, 0, 1);
			half4 evolveParams = _EvolveParams;
			float modelWorldPosY = mul(unity_ObjectToWorld, modelFootPos).y + evolveParams.w;;
			half factor = posWorld.y - modelWorldPosY;
			factor /= _ModelHeight;

			return factor;
		}

		half GetPercentEffectSerration(float3 posWorld)
		{
			half range = 0;
			half r = frac(sin((posWorld.z) * 100));
			if (r > 0.5)
			{
				half ice_sin1 = _Ice_Sin1;
				half ice_sin2 = _Ice_Sin2;
				half ice_degree1 = _Ice_Degree1;
				half ice_degree2 = _Ice_Degree2;
				range = max(0, (sin(posWorld.z*ice_sin1)*ice_degree1*0.01)) + max(0, (sin(posWorld.z*ice_sin2)*ice_degree2*0.01));
			}

			return range;
		}

		void DoPercentEffectClip(float3 posWorld)
		{
			half factor = GetPercentEffectFactor(posWorld) - GetPercentEffectSerration(posWorld);
			clip(factor - _DissolvePercent);
		}
        
        /*
		half GetInvisibilityClipVal(half2 uv)
		{
			half invisibilityColorLength = _InvisibilityColorLength;
			half clipVal = SAMPLE_TEXTURE2D(_InvisibilityMask, sampler_InvisibilityMask, TRANSFORM_TEX(uv, _InvisibilityMask)).r;
			return 1 - _DissolvePercent - clipVal - invisibilityColorLength;
		}
		*/
#endif

#ifdef _DITHER_FADEOUT_NOISE
		void DoDitherFadeoutClipNoise(float2 uv, half ditherOpacity)
		{
			half noise = SAMPLE_TEXTURE2D(_DitheringTex, sampler_DitheringTex, uv).r;
			half clipSign = ditherOpacity - noise;
			clip(clipSign);
		}
#endif

#if defined(_EVOLVE2_ON) || defined(_HOLOGRAM2_ON)
	    float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
	    float2 mod2D289( float2 x ) {  return    x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
	    float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
		#define MOD2 float2(.1031,.11369)
		float hash21(float2 p)
		{
			float2 p2 = frac(p * MOD2);
			p2 += dot(p2, p2.yx + 19.19);
			return -1.0 + 2.0 * frac((p2.x + p2.y) * p2.y);
			//return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
		}
        float snoise( float2 p )
        {
		    float2 pi = floor(p);
            float2 pf = p - pi;
    
            float2 w = pf * pf * (3.0 - 2.0 * pf);
    
            return 
        		lerp(
        			lerp(hash21(pi + float2(0, 0)), hash21(pi + float2(1, 0)), w.x),
        			lerp(hash21(pi + float2(0, 1)), hash21(pi + float2(1, 1)), w.x), 
                    w.y);

            //const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            //float2 i = floor( v + dot( v, C.yy ) );
            //float2 x0 = v - i + dot( i, C.xx );
            //float2 i1;
            //i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            //float4 x12 = x0.xyxy + C.xxzz;
            //x12.xy -= i1;
            //i = mod2D289( i );
            //float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            //float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            //m = m * m;
            //m = m * m;
            //float3 x = 2.0 * frac( p * C.www ) - 1.0;
            //float3 h = abs( x ) - 0.5;
            //float3 ox = floor( x + 0.5 );
            //float3 a0 = x - ox;
            //m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            //float3 g;
            //g.x = a0.x * x0.x + h.x * x0.y;
            //g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            //return 130.0 * dot( m, g );
        }
#endif

#ifdef _EVOLVE2_ON
	    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        float4x4 Inverse4x4(float4x4 input)
        {
            #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
            float4x4 cofactors = float4x4(
            minor( _22_23_24, _32_33_34, _42_43_44 ),
            -minor( _21_23_24, _31_33_34, _41_43_44 ),
            minor( _21_22_24, _31_32_34, _41_42_44 ),
            -minor( _21_22_23, _31_32_33, _41_42_43 ),
        
            -minor( _12_13_14, _32_33_34, _42_43_44 ),
            minor( _11_13_14, _31_33_34, _41_43_44 ),
            -minor( _11_12_14, _31_32_34, _41_42_44 ),
            minor( _11_12_13, _31_32_33, _41_42_43 ),
        
            minor( _12_13_14, _22_23_24, _42_43_44 ),
            -minor( _11_13_14, _21_23_24, _41_43_44 ),
            minor( _11_12_14, _21_22_24, _41_42_44 ),
            -minor( _11_12_13, _21_22_23, _41_42_43 ),
        
            -minor( _12_13_14, _22_23_24, _32_33_34 ),
            minor( _11_13_14, _21_23_24, _31_33_34 ),
            -minor( _11_12_14, _21_22_24, _31_32_34 ),
            minor( _11_12_13, _21_22_23, _31_32_33 ));
            #undef minor
            return transpose( cofactors ) / determinant( input );
        }
        
        inline half4 TriplanarSampling4( float3 worldPos, half3 worldNormal, float falloff, float2 tiling )
        {
            float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
            projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
            half3 nsign = sign( worldNormal );
            half4 xNorm; half4 yNorm; half4 zNorm;
            xNorm = SAMPLE_TEXTURE2D( _HexPattern, sampler_HexPattern, (tiling * worldPos.zy * half2(  nsign.x, 1.0 )) );
            yNorm = SAMPLE_TEXTURE2D( _HexPattern, sampler_HexPattern, (tiling * worldPos.xz * half2(  nsign.y, 1.0 )) );
            zNorm = SAMPLE_TEXTURE2D( _HexPattern, sampler_HexPattern, (tiling * worldPos.xy * half2( -nsign.z, 1.0 )) );
            return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
        }
        
        // https://blog.csdn.net/MQLCSDN/article/details/96352876 
        float4x4 GetClipMatrix()
        {
            half scaleY = 1 / length(unity_WorldToObject[1].xyz);
			// 找到模型的脚底坐标。因为skinMeshRender会根据骨骼绑定会修改模型原点的坐标问题，
			// 这里专门模型的原点+一个世界空间的偏移数值来确定模型的脚底坐标
			float4 modelFootPos = float4(0, 0, 0, 1);
			float3 modelWorldPos = mul(GetObjectToWorldMatrix(), modelFootPos).xyz;
			float offset = lerp(-3, _ModelHeight, _DissolvePercent);  // 需要根据模型高度来调整范围
			modelWorldPos.y += offset * scaleY;
			//modelWorldPos.y += _HeightOffset * scaleY;

            // worldPoss生成一个worldToLocalMatrix
            float4x4 trans = float4x4(float4(1, 0, 0, -modelWorldPos.x), float4(0, 1, 0, -modelWorldPos.y), float4(0, 0, 1, -modelWorldPos.z), float4(0, 0, 0, 1));
            float4x4 rotate = float4x4(float4(-1, 0, 0, 0), float4(0, 0, 1, 0), float4(0, 1, 0, 0), float4(0, 0, 0, 1)); // x旋转180，z旋转-90的旋转矩阵
            float4x4 clipMatrix = mul(rotate, trans);
            return clipMatrix;
        }
        
        // vertex
        float3 Evolove2GetVertexOffset(float4 positionWS)
        {
        	float4x4 clipMatrix = GetClipMatrix();
            //float4x4 _ClipQuadMatrix = worldToLocalMatrix; //_ClipQuadMatrix
            float4x4 inverseClipQuadMatrix = Inverse4x4(clipMatrix);
            //float3 positionWS = mul(GetObjectToWorldMatrix(), positionOS).xyz;
            //float4 tempMatrix = mul(_ClipQuadMatrix, float4(positionWS, 1.0));
            float4 tempMatrix = mul(clipMatrix, positionWS);
            float3 objectMatrix = mul(GetWorldToObjectMatrix(), float4(mul(inverseClipQuadMatrix, float4(float3(tempMatrix.x, tempMatrix.y, 0.0), 0.0)).xyz, 0.0)).xyz;
            
            //float noise = snoise(float2(tempMatrix.x, tempMatrix.y) * _NoiseScale);
            //noise = noise * 0.5 + 0.5;
            float tempNoise = tempMatrix.z;// + (noise * _NoiseInfluence);
            
            float distance = saturate(smoothstep( _DistanceMin , _DistanceMax , tempNoise));
            
            float3 vertexOffset = normalize(objectMatrix) * _VertexOffset * distance;
            return vertexOffset;
        }
        
        // fragment     
        half4 Evolove2GetEmissionAndOpacity(float3 positionWS, half3 positionOS, half3 normalOS, half3 normalWS, half3 viewDir)
        {             
            float4x4 clipMatrix = GetClipMatrix();
            // noise
            float4 tempMatrix = mul(clipMatrix, float4(positionWS, 1.0));
            //float noise = snoise(float2(tempMatrix.x, tempMatrix.y) * _NoiseScale);
            //noise = noise * 0.5 + 0.5;
            float tempNoise = tempMatrix.z;// + (noise * _NoiseInfluence);
            // uv offset
            half4 uvHexPattern = TriplanarSampling4(positionOS.xyz, normalOS, _Falloff, (_Tiling).xx);
            float hexOffset = (0.0 + (uvHexPattern.y - 0.0) * (-1.0 - 0.0) / (1.0 - 0.0));
            float tempHexOffset = hexOffset * _HexMaxOffset;
            
            float uvParam =  tempNoise + tempHexOffset;
            
            // hex Color
            float distance = saturate(smoothstep(_DistanceMin, _DistanceMax, uvParam));
            float4 level = lerp(_LevelsEnd, _LevelsStart, distance);
            half tempOutput = saturate(level.z + (uvHexPattern.r - level.x) * (level.w - level.z) / (level.y - level.x)) * (1.0 - distance);
            half3 hsvHexColor = RgbToHsv( _HexColor.rgb );
            half3 rgbHexColor = HsvToRgb( half3(( hsvHexColor.x + _HueOffset ), hsvHexColor.y, hsvHexColor.z) );
            half3 hexColor = rgbHexColor * distance * tempOutput;
            
            // hex Color2
            float distance2 = saturate(smoothstep(_Distance2Min, _Distance2Max, uvParam));           
            half tempOutput2 = saturate( 0.0 + ((1.0 - uvHexPattern.r) - 0.95) * (1.0 - 0.0) / (1.0 - 0.95)) * (1.0 - distance2);
            half3 hsvHexColor2 = RgbToHsv( _HexColor2.rgb );
            half3 rgbHexColor2 = HsvToRgb(half3((hsvHexColor2.x + _HueOffset), hsvHexColor2.y, hsvHexColor2.z));
            half3 hexColor2 = rgbHexColor2 * distance2 * tempOutput2;
            
            // fresnel color
            float fresnel = 0.0 + _FresnelScale_E * pow(1.0 - dot( normalWS, viewDir ), _FresnelPower_E);         
            float distance3 = saturate(smoothstep(_Distance3Min, _Distance3Max, tempNoise));
            half3 hsvFresnelColor = RgbToHsv( _FresnelColor_E.rgb );
            half3 rgbFresnelColor = HsvToRgb(half3((hsvFresnelColor.x + _HueOffset), hsvFresnelColor.y, hsvFresnelColor.z));
            half3 fresnelColor = rgbFresnelColor * distance3 * fresnel;

            // emission
            half3 emission = clamp(hexColor + hexColor2 + fresnelColor, half3( 0,0,0 ) , half3(999,999,999));
            
            // opacity
            half colorTempOutput = tempOutput + tempOutput2;
            half opacity = saturate(lerp(1.0, colorTempOutput, sign(distance)));
            
            return half4(emission, opacity);
        }
#endif

//全息2
#ifdef _HOLOGRAM2_ON
        // vertex
        float4 Hologram2Vertex(float3 positionOS, float3 positionWS)
        {
            float3 lineGlitchM2V = mul(UNITY_MATRIX_T_MV, half4(_Hologram2_LineGlitchOffset, 0)).xyz;  // 从模型空间变换到观察空间
            float3 objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
            float time = _Time.y;//fmod(_Time.x, 10);
            
            half axisWS = positionWS.x;
            half axisOS = positionOS.x;
            if(_Hologram2_PositionFeature == 1)
            {
                axisWS = positionWS.y;
                axisOS = positionOS.y;
            }
            else if(_Hologram2_PositionFeature == 2)
            {
                axisWS = positionWS.z;
                axisOS = positionOS.z;
            }
            
            half axis = axisWS;
            if(_Hologram2_PositionSpaceFeature == 1)
            {
                axis = axisOS;
            }
            
            half direct = axis * _Hologram2_PositionDirection;
            
            // line glitch
            float lineGlitchDist = time * _Hologram2_LineGlitchSpeed;
            float2  lineGlitchUV = (direct * _Hologram2_LineGlitchFrequency + (lineGlitchDist + _Hologram2_RandomOffset)).xx;
            float lineGlitchClamp = clamp(((SAMPLE_TEXTURE2D_X_LOD(_Hologram2_LineGlitch, sampler_Hologram2_LineGlitch, float4(lineGlitchUV, 0, 0), 0).r - _Hologram2_LineGlitchInvertedThickness) * _Hologram2_LineGlitchHardness), 0.0, 1.0);
            float3 lineGlithOffset = (lineGlitchM2V / objectScale) * lineGlitchClamp;
            
            // Random Glitch
            float3 randomGlitchM2V = mul(UNITY_MATRIX_T_MV, half4(_Hologram2_RandomGlitchOffset, 0)).xyz;
            float2 randomGlithUV = float2((direct * _Hologram2_RandomGlitchTiling + (time * -2.3 + _Hologram2_RandomOffset)), (_Hologram2_RandomOffset + time * -2.05));
            float simplePerlin = snoise(randomGlithUV);
            simplePerlin = simplePerlin * 0.5 + 0.5;
            
            float4 matrixToPos = float4( float4x4( 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1 )[0][3],float4x4( 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1 )[1][3],float4x4( 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1 )[2][3],float4x4( 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1 )[3][3]);  
            float offset = matrixToPos.x + matrixToPos.y + matrixToPos.z;
            if(_Hologram2_PositionSpaceFeature == 1)
            {
                offset = 0;
            }
           float2 randomGlithUVOffset = float2((offset * 223 + (time * -5.74 + _Hologram2_RandomOffset)), (_Hologram2_RandomOffset + time * -0.83));
           float simplePerlinRandomGlith = snoise(randomGlithUVOffset);
           simplePerlinRandomGlith = simplePerlinRandomGlith * 0.5 + 0.5;
           float randomGlithOffsetClamp = clamp((-1 + (simplePerlinRandomGlith + _Hologram2_RandomGlitchConstant) * 2), 0, 1);
           float randomGlithFinal = (-1 + simplePerlin * 2) * randomGlithOffsetClamp;
           
           float2 tempUV = ((20 * randomGlithUV.x) , randomGlithUV.y);
           float simplePerlinTemp = snoise(tempUV);
           simplePerlinTemp = simplePerlinTemp * 0.5 + 0.5;
           float tempClamp = clamp((-1 + simplePerlinTemp * 2), 0 ,1);
           float tempLerp = lerp(0, tempClamp, 2);
           float3 randomGlithOffset = (randomGlitchM2V / objectScale) * (randomGlithFinal + randomGlithFinal * tempLerp) * _Hologram2_RandomGlitchAmount;
           
           
           float3 vertexOffset = lineGlithOffset + randomGlithOffset;
           return float4(positionOS + vertexOffset, direct);
        }
        
        // fragment
        float4 Hologram2Fragment(float4 sourceColor, half2 uv, float3 positionOS, float3 positionWS, float3 normalWS, float3 tangentWS, float3 bitangentWS, float direct)
        {
			//全息2效果 不改变原始alpha
		    float originalA = sourceColor.a;
            float time = _Time.y;//fmod(_Time.x, 10);
            float3 worldViewDir = normalize(_WorldSpaceCameraPos.xyz - positionWS);
            float3 normalWorld = normalize(normalWS);

            
            half3 m2W = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
            
            half temp = m2W.x + m2W.y + m2W.z;
            if(_Hologram2_PositionSpaceFeature == 1)
            {
                temp = 0;
            }

            // color glith
            float2 randomOffsetUV = float2(temp * 223 + (_Hologram2_RandomOffset + time * -15), (_Hologram2_RandomOffset + time * -0.5));
            float simplePerlinTemp = snoise(randomOffsetUV);
            simplePerlinTemp = simplePerlinTemp * 0.5 + 0.5;
            float tempClamp = clamp((-0.61 + simplePerlinTemp * 2), 0, 1);
            float tempLerp = lerp(1, tempClamp, _Hologram2_ColorGlitchAffect);
            float colorGlithParam = tempLerp;
            
            half4 mainColor = _Hologram2_Color * sourceColor;// * SAMPLE_TEXTURE2D(_Hologram2_MainTex, sampler_Hologram2_MainTex, TRANSFORM_TEX(uv, _Hologram2_MainTex));

            // Holo2的部分属性会导致某些小米机型崩溃，故删除掉
            // float3 tanToWorld0 = float3(tangentWS.x, bitangentWS.x, normalWorld.x);
            // float3 tanToWorld1 = float3(tangentWS.y, bitangentWS.y, normalWorld.y);
            // float3 tanToWorld2 = float3(tangentWS.z, bitangentWS.z, normalWorld.z);
            // float3 tanViewDir = normalize(tanToWorld0 * worldViewDir.z + tanToWorld1 * worldViewDir.y + tanToWorld2 * worldViewDir.z);
            // float NdotV = dot(UnpackNormalScale(SAMPLE_TEXTURE2D(_Hologram2_NormalMap, sampler_Hologram2_NormalMap, TRANSFORM_TEX(uv, _Hologram2_NormalMap)), _Hologram2_NormalScale), tanViewDir);
            // float normalAffect = lerp(1, (NdotV + 1) / 2, _Hologram2_NormalAffect);
            // float normal = 1 - normalAffect;
            float normal = 0;

            // fresnel
            float fresnelColorParam = _Hologram2_FresnelRGBScale * pow(1 - dot(normalWorld, worldViewDir), _Hologram2_FresnelRGBPower);
			float4 fresnelColor = fresnelColorParam * mainColor;
            // float fresnelColorNormal = saturate(fresnelColorParam + normal);
            // float fresnelAlpha = _Hologram2_FresnelAlphaScale * pow(1 - dot(normalWorld, worldViewDir), _Hologram2_FresnelAlphaPower);
            // float fresnelAlphaNormal = clamp((fresnelAlpha + normal), 0, 1);
            // float4 fresnelColor = fresnelColorNormal * mainColor * fresnelAlphaNormal;

            // line 1
            float2 line1UV = (direct * _Hologram2_Line1Frequency + (time * _Hologram2_Line1Speed + _Hologram2_RandomOffset)).xx;
            float line1Param = clamp(((SAMPLE_TEXTURE2D(_Hologram2_Line1, sampler_Hologram2_Line1, line1UV).r - _Hologram2_Line1InvertedThickness) * _Hologram2_Line1Hardness), 0, 1);
            float3 line1Color = (mainColor * line1Param).rgb;
            float line1Alpha = line1Param * _Hologram2_Line1Alpha;
            float4 line1 = float4(line1Color, line1Alpha);

            // Holo2的部分属性会导致某些小米机型崩溃，故删除掉
            // grain
            // float simplePerlinGrain = snoise(positionWS * _Hologram2_GrainScale + time * 100);
            // simplePerlinGrain = simplePerlinGrain * 0.5 + 0.5;
            // float grainAffect = lerp(_Hologram2_GrainValues.x, _Hologram2_GrainValues.y, simplePerlinGrain);
            // float grain = lerp(0, grainAffect, _Hologram2_GrainAffect);

            // float tempAlpha = clamp((mainColor.a + fresnelAlphaNormal + line1.w), 0, 1);
            // Dissolve
            // float3 position = positionWS;
            // if(_Hologram2_PositionSpaceFeature == 1)
            // {
            //     position = positionOS;
            // }
            // float simplePerlinDissolve = snoise(position * _Hologram2_DissolveScale);
            // simplePerlinDissolve = simplePerlinDissolve * 0.5 + 0.5;
            // float dissolve = clamp((simplePerlinDissolve - _Hologram2_DissolveHide), 0, 1);

            // float3 color = float4(colorGlithParam * (mainColor + fresnelColor + float4(line1.xyz, 0) + grain)).rgb;
            // float alpha = tempAlpha * dissolve * _Hologram2_Alpha;
            // float3 color = float4(colorGlithParam * (mainColor + fresnelColor + float4(line1.xyz, 0))).rgb;
            // float alpha = tempAlpha * _Hologram2_Alpha;
            // return saturate(float4(color, alpha));
			float4 finalColor = colorGlithParam * (mainColor + fresnelColor + float4(line1.xyz, 0));
			finalColor.a = originalA;
			return max(finalColor, 0) ;
        }   
#endif

#endif
