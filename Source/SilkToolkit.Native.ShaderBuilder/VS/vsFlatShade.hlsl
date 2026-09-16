#ifndef VSFLATSHADE_HLSL
#define VSFLATSHADE_HLSL

#define MESH

#include "../Commons.hlsli"
#pragma pack_matrix( row_major )


GS_SIMPLE_IN main(VSEInput input){
	GS_SIMPLE_IN output = (GS_SIMPLE_IN)0;

    float4 position = input.p;
    float3 normal = input.n;

    invertNormalIfNeeded(normal);    
    matrix mInstance = { input.mr0, input.mr1, input.mr2, input.mr3 };
    instantiate(position, normal, input.t1, input.t2, mInstance);

	
	//set position into camera clip space	
    output.p = mul(position, mWorld);
	output.wp = output.p.xyz;
	output.p = mul(output.p, mViewProjection);
    output.n = normalize(mul(normal, (float3x3) mWorld));
	output.curv = input.curv;
	return output;
}

#endif