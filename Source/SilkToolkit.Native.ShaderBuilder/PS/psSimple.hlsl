#ifndef PSSIMPLE_SHADER_HLSL
#define PSSIMPLE_SHADER_HLSL
#define MESH
#include "../Commons.hlsli"

float4 main(PSInput input) : SV_TARGET
{
	return shadeSimple(input.n);
}

#endif