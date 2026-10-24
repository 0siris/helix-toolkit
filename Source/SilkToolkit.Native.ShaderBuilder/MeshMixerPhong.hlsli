#ifndef MESHMIXER_PHONG_HLSLI
#define MESHMIXER_PHONG_HLSLI

// Ordinary buffered Meshmixer 3.5 PHONG only. Source and measured context provenance:
// reference-runtime/full-product-z1-color-material-contract.json: phong.vert, phong.frag, frag_util.frag.
// reference-runtime/full-product-z1-phong-light-state.json: selected/unselected profiles and buffered ambient.
// No generic Silk/WPF lights, Material02 values, textures, mirror, symmetry, cut planes or transparent passes.

cbuffer MeshMixerPhongFrame : register(b2)
{
    row_major float4x4 mmModelView;       // byte 0: row-vector scene -> eye
    row_major float4x4 mmProjection;      // byte 64: row-vector eye -> GL clip, z in [-w,w]
    row_major float4x4 mmNormalMatrix;    // byte 128: explicit scene -> eye normal transform
    float4 mmLightPositionEye;           // byte 192: explicit original positional light
    float4 mmHalfVectorEye;              // byte 208: explicit native driver halfvector, XYZ normalized in VS
    float4 mmProfile;                    // byte 224: X=selected membership (0 or 1), YZW=0; total 240 bytes
};

struct MeshMixerPhongVertex
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float4 frontColor : COLOR0;
    float4 backColor : COLOR1;           // existing slot 3, first float4; not dental curvature
};

struct MeshMixerPhongPixel
{
    float4 position : SV_POSITION;
    float3 normal : TEXCOORD0;
    float4 sourcePosition : TEXCOORD1;
    float4 diffuse : TEXCOORD2;
    float4 backDiffuse : TEXCOORD3;
    float4 ambient : TEXCOORD4;
    float4 ambientGlobal : TEXCOORD5;
    float3 lightDirection : TEXCOORD6;
    float3 halfVector : TEXCOORD7;
    float distance : TEXCOORD8;
    float frontAlpha : TEXCOORD9;
};

// Original55eec0 + PE immediates and genuine GL observations establish these TWO ordinary profiles.
// Frame reset/full selected: diffuse/specular .75, material specular (.25,.2,.2,1).
// Per-record enter(.5,false): diffuse .375, light specular RGB ZERO, material (.125,.1,.1,1).
// Leave(1,true), pop lighting attributes and restore renderer flag reset the native profile for the next record.
float4 meshMixerLightDiffuse()
{
    float value = mmProfile.x != 0.0f ? 0.75f : 0.375f;
    return float4(value, value, value, 1.0f);
}

float4 meshMixerLightSpecular()
{
    float value = mmProfile.x != 0.0f ? 0.75f : 0.0f;
    return float4(value, value, value, 1.0f);
}

float4 meshMixerMaterialSpecular()
{
    return mmProfile.x != 0.0f ? float4(0.25f, 0.2f, 0.2f, 1.0f) : float4(0.125f, 0.1f, 0.1f, 1.0f);
}

// Original559700/55eec0: light ambient (0,0,0,1), global ambient (.05,.05,.05,1), shininess 30.
// GL_COLOR_MATERIAL is FRONT/AMBIENT_AND_DIFFUSE; original103290 sets white before its buffered draw.
// The genuine driver probe DURING that draw observes WHITE front ambient, not the last vertex's post-draw color.
static const float4 mmLightAmbient = float4(0.0f, 0.0f, 0.0f, 1.0f);
static const float4 mmLightModelAmbient = float4(0.05f, 0.05f, 0.05f, 1.0f);
static const float4 mmBufferedFrontAmbient = float4(1.0f, 1.0f, 1.0f, 1.0f);
static const float mmShininess = 30.0f;
// Observed inherited attenuation in the genuine context and every reached light leaf, not PE-written defaults.
static const float3 mmAttenuation = float3(1.0f, 0.0f, 0.0f);

#endif
