#ifndef GSFLATSHADESMOOTH_HLSL
#define GSFLATSHADESMOOTH_HLSL

#define MESH
#include "../Commons.hlsli"

struct GSOutput
{
	float4 pos : SV_POSITION;
};

[maxvertexcount(3)]
void main(
	triangle GS_SIMPLE_IN input[3], 
	inout TriangleStream<PS_SIMPLE_IN> outStream
)
{
	PS_SIMPLE_IN output = (PS_SIMPLE_IN)0;

	float3 normal = normalize(input[0].n + input[1].n + input[2].n);

	output.c = shadeSimple(normal);
	for (int i = 0; i < 3; i++) {
		output.p = input[i].p;
		output.curv = input[i].curv;
		outStream.Append(output);
	}

	outStream.RestartStrip();
}

#endif