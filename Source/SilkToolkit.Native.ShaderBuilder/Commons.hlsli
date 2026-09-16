#ifndef COMMONS_HLSI
#define COMMONS_HLSI


//--------------------------------------------------------------------------------------
// Perframe Buffers
//--------------------------------------------------------------------------------------

#if defined(MESH)
//Per model
cbuffer cbMeshModel2 : register(b10)
{
	float4x4 mWorldView;
	float4x4 mWorldViewProjection;
};
#endif


#if defined(GOOCH)
//Size: 3*16byte = 48byte
cbuffer cbGooch : register(b2)
{
    float3 LightVector = float3(0, -1, 0);
    
    float3 KCool = float3(0.14, 0.14, 0.54);
    
    float3 KWarm = float3(0.92,0.92,0.42);
    bool   GoochSmoothShading = true;
};
#endif


#include "Common/CkTools.hlsl" //includes Common.hlsl->CommonBuffers.hlsl->DataStructs.hlsl
#include "Common/OLFunctions.hlsl"
#include "Common/OLDataStructs.hlsli"


#endif