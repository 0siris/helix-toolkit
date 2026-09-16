#ifndef ORCALIGN_DATA_STRUCTS_HLSI
#define ORCALIGN_DATA_STRUCTS_HLSI


//extended input for orcalign models
struct VSEInput {
    float4 p    : POSITION;
    float3 n    : NORMAL;
    float3 t1   : TANGENT;
    float3 t2   : BINORMAL;
    float2 t    : TEXCOORD;
    float4 c    : COLOR;

	float4 mr0  : TEXCOORD1;
	float4 mr1  : TEXCOORD2;
	float4 mr2  : TEXCOORD3;
	float4 mr3  : TEXCOORD4;

	float4 curv : COLOR1; //curvature
	float4 hints: COLOR2; //shading hints
};



struct GS_GOOCH_IN {
	float4 p	: SV_Position;
	float3 wp	: POSITION0;
	float3 n	: NORMAL;
	float4 curv :COLOR;
};

struct GS_SIMPLE_IN {
	float4 p	: SV_Position;
	float3 wp	: POSITION0;
	float3 n	: NORMAL;
	float4 curv : COLOR;
};



struct PS_GOOCH_IN {
	float4 p	: SV_Position;
	float3 c	: COLOR;
	float3 n	: NORMAL;
	float4 curv : COLOR1;
};

struct PS_SIMPLE_IN {
	float4 p	: SV_Position;
	float4 c	: COLOR;
	float4 curv	: COLOR1;
};


#endif 