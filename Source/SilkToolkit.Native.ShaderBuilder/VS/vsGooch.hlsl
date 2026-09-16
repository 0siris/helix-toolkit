#ifndef VSGOOCH_SHADER_HLSL
#define VSGOOCH_SHADER_HLSL

#define MESH
#define GOOCH

#include "../Commons.hlsli"
#pragma pack_matrix( row_major )


GS_GOOCH_IN main(VSEInput input){
    GS_GOOCH_IN output = (GS_GOOCH_IN) 0;

    float4 position = input.p;
    float3 normal = input.n;

    invertNormalIfNeeded(normal); 
    matrix mInstance = { input.mr0, input.mr1, input.mr2, input.mr3 };
    instantiate(position, normal, input.t1, input.t2, mInstance);
	
    output.p = mul(position, mWorld); //set position into camera clip space	
	output.wp = output.p.xyz;
    output.p = mul(output.p, mViewProjection);
	output.n = normalize(mul(normal,(float3x3)mWorld));
	output.curv = input.curv;

	return output;
}


#endif