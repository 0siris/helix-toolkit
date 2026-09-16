#ifndef GSFLATSHADE_HLSL
#define GSFLATSHADE_HLSL

#define MESH
#include "../Commons.hlsli"


[maxvertexcount(3)]
void main(
	triangle GS_SIMPLE_IN input[3], 
	inout TriangleStream<PS_SIMPLE_IN> outStream
)
{
	PS_SIMPLE_IN output = (PS_SIMPLE_IN)0;

	float3 a = input[1].wp - input[0].wp;
	float3 b = input[2].wp - input[0].wp;
	float3 normal = normalize(cross(b, a));

	output.c = shadeSimple(normal);

	for (int i = 0; i < 3; i++) {
		output.p = input[i].p;
		output.curv = input[i].curv;
		outStream.Append(output);
	}
	outStream.RestartStrip();
}

#endif