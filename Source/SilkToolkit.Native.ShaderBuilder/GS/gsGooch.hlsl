#ifndef GSGOOCH_SHADER_HLSL
#define GSGOOCH_SHADER_HLSL

#define GOOCH
#include "../Commons.hlsli"


float3 calcGoochIllumination(float3 normal) {
	float s = (1 + dot(LightVector, normal)) / 2.0;
	float3 diffuse = KCool * s + (1 - s) * KWarm;
    
    if (GoochSmoothShading){
        return diffuse; //smoothing will be done by pixel shader
    }
    else{ // calculate the specular part by primitive
        static const float n = 32;
        float3 lookDir = -normalize(float3(mView._13_23_33));
        float3 R = reflect(-LightVector, normal);
        float3 V = lookDir;
        float3 specular = float3(0.5, 0.5, 0.5) * (n + 2) / (2 * 3.14159265359) * pow(abs(dot(R, V)), n);

        return saturate(diffuse + specular);
    }
}

[maxvertexcount(3)]
void main(
	triangle GS_GOOCH_IN input[3], 
	inout TriangleStream<PS_GOOCH_IN> outStream
)
{
	PS_GOOCH_IN output = (PS_GOOCH_IN)0;

    float3 normal;
    if (GoochSmoothShading)    {
        normal = normalize(input[0].n + input[1].n + input[2].n);
    }
    else{
        float3 posA = input[0].wp;
        float3 posB = input[1].wp;
        float3 posC = input[2].wp;
        normal = normalize(cross(normalize(posB - posA), normalize(posC - posA)));
    }

	output.c = calcGoochIllumination(normal);
	for (int i = 0; i < 3; i++) {
		output.p = input[i].p;
		output.n = input[i].n;
		output.curv = input[i].curv;
		outStream.Append(output);
	}
	outStream.RestartStrip();
}


#endif