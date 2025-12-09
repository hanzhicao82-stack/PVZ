#ifndef GERSTNER_WAVES_INCLUDED
#define GERSTNER_WAVES_INCLUDED




float Random(int seed)
{
    return frac(sin(dot(float2(seed,2), float2(12.9898, 78.233))) ) * 2 - 1;
}

struct Gerstner
{
    float3 positionWS;
    float3 binormal;
    float3 tangent;
};

Gerstner GerstnerWave(float2 direction, float3 positionWS, int waveCount, float wavelengthMax, float wavelengthMin, float steepnessMax, float steepnessMin, float randomdirection)
{
    Gerstner gerstner;

    float3 P;
    float3 B;
    float3 T;


    for (int i = 0; i < waveCount; i++)
    {
        float step = (float) i / (float) waveCount;

        float2 d = float2(Random(i),Random(2*i));
        d = normalize(lerp(normalize(direction), d, randomdirection));

        float wavelength = lerp(wavelengthMax, wavelengthMin, step);
        float steepness = lerp(steepnessMax, steepnessMin, step) / waveCount;

        float k = 2 * PI / wavelength;
        float g = 9.81f;
        float w = sqrt(g * k);
        float a = steepness / k;
        float2 wavevector = k * d;
        float value = dot(wavevector, positionWS.xz) - w * _Time.y * _DirRandDirAndWaveSpeed.w;

        P.x += d.x * a * cos(value);
        P.z += d.y * a * cos(value);
        P.y += a * sin(value);

        T.x += d.x * d.x * k * a * -sin(value);
        T.y += d.x * k * a * cos(value);
        T.z += d.x * d.y * k * a * -sin(value);

        B.x += d.x * d.y * k * a * -sin(value);
        B.y += d.y * k * a * cos(value);
        B.z += d.y * d.y * k * a * -sin(value);
    }
    
    gerstner.positionWS.x = positionWS.x + P.x * 10.0;
    gerstner.positionWS.y = positionWS.y + P.y * 10.0;
    gerstner.positionWS.z = positionWS.z + P.z * 10.0;
    gerstner.tangent = float3(1 + T.x, T.y, T.z);
    gerstner.binormal = float3(B.x,B.y,1 + B.z);

    return gerstner;
}

#endif // GERSTNER_WAVES_INCLUDED