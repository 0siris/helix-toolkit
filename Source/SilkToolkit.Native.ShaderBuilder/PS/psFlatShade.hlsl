#ifndef PSFLATSHADE_HLSL
#define PSFLATSHADE_HLSL

#include "../Commons.hlsli"

float4 main(PS_SIMPLE_IN input) : SV_TARGET
{
	shadeCurvature(input.c.rgb, input.curv,0.30f);
	return input.c;
}

#endif