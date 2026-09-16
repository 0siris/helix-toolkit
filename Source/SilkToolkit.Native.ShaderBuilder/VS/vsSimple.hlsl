#ifndef VSSIMPLE_SHADER_HLSL
#define VSSIMPLE_SHADER_HLSL

#define MESH
#include "../Commons.hlsli"
#pragma pack_matrix( row_major )


PSInput main(VSInput input)
{
	PSInput output = (PSInput)0;

	float4 position = input.p;
	float3 normal = input.n;

    invertNormalIfNeeded(normal);
    matrix mInstance = { input.mr0, input.mr1, input.mr2, input.mr3 };
    instantiate(position, normal, input.t1, input.t2, mInstance);

	//set position into camera clip space	
	output.p = mul(position, mWorld);
	output.wp = output.p;
	output.p = mul(output.p, mView);
	output.p = mul(output.p, mProjection);
	output.n = normalize(mul(normal.xyz, (float3x3) mWorld));

	//set texture coords and color
	output.t = input.t;
	output.c = input.c;

	return output;
}


#endif