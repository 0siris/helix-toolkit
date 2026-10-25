#ifndef MESHMIXER_PHONG_HLSLI
#define MESHMIXER_PHONG_HLSLI

// Ordinary buffered Meshmixer PHONG and Inspector GeometryBuffer lighting. Source/context provenance:
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
    float4 mmProfile;                    // byte 224: X=ordinary selection; Y=Inspector GeometryBuffer
    float4 mmInspectorLight3;            // byte 240: region light3; ordinary draws retain zero padding
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
    float4 inspectorVertexColor : TEXCOORD10;
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

// Original frag_util standard_lightmodel88-109, parameterized for the existing fragment draw
// and the Inspector's compatibility vertex draw. Normal/light normalization belongs to the caller.
float4 meshMixerEvaluateLight(float3 normal, float3 light, float3 halfVector,
    float4 diffuse, float4 ambient, float4 ambientGlobal,
    float4 materialSpecular, float4 lightSpecular, float3 attenuationTerms,
    float distance, float shininess, bool includeSpecular, float alpha)
{
    float4 color = ambientGlobal;
    float normalDotLight = dot(normal, light);
    if (normalDotLight <= 0.0f)
    {
        color.a = alpha;
        return color;
    }
    float attenuation = 1.0f / (attenuationTerms.x + attenuationTerms.y * distance
        + attenuationTerms.z * distance * distance);
    color += attenuation * (diffuse * normalDotLight + ambient);
    float normalDotHalf = max(dot(normal, halfVector), 0.0f);
    if (includeSpecular)
        color += attenuation * materialSpecular * lightSpecular * pow(normalDotHalf, shininess);
    color.a = alpha;
    return color;
}

// 559700 -> 55eec0(1,true), then 55ad60: FRONT/AMBIENT_AND_DIFFUSE follows glColor.
// All four lights are enabled: diffuse .75/.3/.3/.3, specular .75 each.
// Compatibility lighting is evaluated and clamped per vertex; TWO_SIDE is false here.
float4 meshMixerInspectorVertexColor(float3 normal, float4 eyePosition, float4 sourceColor)
{
    float4 positions[4] = {
        mmLightPositionEye, mmNormalMatrix[3], mmHalfVectorEye, mmInspectorLight3
    };
    float4 color = mmLightModelAmbient * sourceColor;
    [unroll]
    for (int index = 0; index < 4; index++)
    {
        float3 auxiliary = (positions[index] - eyePosition).xyz;
        float3 light = normalize(auxiliary);
        float3 halfVector = normalize(light + float3(0, 0, 1));
        float diffuse = index == 0 ? 0.75f : 0.3f;
        color += meshMixerEvaluateLight(normal, light, halfVector,
            sourceColor * float4(diffuse, diffuse, diffuse, 1),
            sourceColor * mmLightAmbient, float4(0, 0, 0, 0),
            float4(0.25f, 0.2f, 0.2f, 1), float4(0.75f, 0.75f, 0.75f, 1),
            mmAttenuation, length(auxiliary), 30, true, 0);
    }
    return float4(saturate(color.rgb), sourceColor.a);
}

#endif
