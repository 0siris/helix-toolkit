#ifndef PS_GOOCH_SHADER_HLSL
#define PS_GOOCH_SHADER_HLSL

#define GOOCH
#include "../Commons.hlsli"

float4 main(PS_GOOCH_IN input) : SV_TARGET
{
	//make areas around negative min curvature darker
	shadeCurvature(input.c, input.curv);

    if (GoochSmoothShading)    {
        static const float n = 32; //Shininess
        float3 V = -normalize(float3(mView._13_23_33));
        float3 R = reflect(-LightVector, input.n);
        float3 specular = float3(0.5, 0.5, 0.5) * (n + 2) / (2 * 3.14159265359) * pow(abs(dot(R, V)), n);
        return float4(saturate(input.c + specular), 1);
    }
     
    return float4(input.c, 1);    
}

#endif