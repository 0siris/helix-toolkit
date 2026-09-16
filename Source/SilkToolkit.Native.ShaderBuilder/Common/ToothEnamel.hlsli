#ifndef TOOTH_ENAMEL_HLSLI
#define TOOTH_ENAMEL_HLSLI

cbuffer cbToothEnamel : register(b2)
{
    float3 DentinColor;
    float Alpha;

    float3 EnamelTint;
    int DebugView;

    float3 StainColor;
    float ExposureCompensation;

    float3 ToothRoot;
    float EnamelStrength;

    float3 ToothCrown;
    float DentinStrength;

    float TranslucencyStrength;
    float BackscatterStrength;
    float EdgeTranslucency;
    float SalivaClearcoat;

    float ClearcoatRoughness;
    float BaseRoughness;
    float StainStrength;
    float GumStainStrength;

    float3 ToothBoundsMin;
    float MicrostructureStrength;

    float3 ToothBoundsMax;
    float ToothEnamelPadding;
};

struct PS_TOOTH_ENAMEL_IN
{
    float4 p : SV_POSITION;
    float4 vEye : POSITION0;
    float4 wp : POSITION1;
    float3 posOS : TEXCOORD2;
    float3 posWS : TEXCOORD3;
    float3 n : NORMAL;
    float2 uv : TEXCOORD0;
    float4 curv : COLOR0;
    float4 hints : COLOR1;
};

#if defined(TOOTH_ENAMEL_PIXEL)

float ToothHeight01(float3 posOS)
{
    float3 axis = ToothCrown - ToothRoot;
    float lengthSquared = dot(axis, axis);
    float featureHeight = saturate(dot(posOS - ToothRoot, axis) / max(lengthSquared, 0.0001));

    float3 boundsSize = ToothBoundsMax - ToothBoundsMin;
    float largestBoundsAxis = max(boundsSize.x, max(boundsSize.y, boundsSize.z));
    float boundsHeight = saturate((posOS.y - ToothBoundsMin.y) / max(boundsSize.y, 0.0001));
    float3 boundsMargin = largestBoundsAxis.xxx * 0.5;
    float3 expandedMin = ToothBoundsMin - boundsMargin;
    float3 expandedMax = ToothBoundsMax + boundsMargin;
    bool rootInBounds = all(ToothRoot >= expandedMin) && all(ToothRoot <= expandedMax);
    bool crownInBounds = all(ToothCrown >= expandedMin) && all(ToothCrown <= expandedMax);

    return lengthSquared > largestBoundsAxis * largestBoundsAxis * 0.0025 && rootInBounds && crownInBounds
        ? featureHeight
        : boundsHeight;
}

float3 ShadeToothLight(float3 posWS, float3 n, float3 v, float3 baseColor, float thinMask,
    out float3 dentinScatter, out float3 edgeTranslucency, out float3 clearcoat)
{
    float3 color = 0;
    dentinScatter = 0;
    edgeTranslucency = 0;
    clearcoat = 0;

    [loop]
    for (int i = 0; i < NumLights; ++i)
    {
        float3 l = 0;
        float3 lightColor = Lights[i].vLightColor.rgb;
        float attenuation = 1.0;

        if (Lights[i].iLightType == 1)
        {
            l = normalize(Lights[i].vLightDir.xyz);
        }
        else
        {
            l = Lights[i].vLightPos.xyz - posWS;
            float distanceToLight = length(l);
            if (Lights[i].vLightAtt.w < distanceToLight)
                continue;

            l /= max(distanceToLight, 0.0001);
            attenuation = 1.0 / max(Lights[i].vLightAtt.x + Lights[i].vLightAtt.y * distanceToLight +
                                    Lights[i].vLightAtt.z * distanceToLight * distanceToLight, 0.0001);

            if (Lights[i].iLightType == 3)
            {
                float3 spotDir = normalize(Lights[i].vLightDir.xyz);
                float rho = dot(-l, spotDir);
                attenuation *= pow(saturate((rho - Lights[i].vLightSpot.x) /
                                             max(Lights[i].vLightSpot.y - Lights[i].vLightSpot.x, 0.0001)),
                                   Lights[i].vLightSpot.z);
            }
        }

        float ndotlRaw = dot(n, l);
        float ndotl = saturate(ndotlRaw);
        float ndotv = saturate(dot(n, v));
        float3 h = normalize(l + v);
        float ldoth = max(saturate(dot(l, h)), 0.01);
        float ndoth = saturate(dot(n, h));
        float wrap = saturate((ndotlRaw + 0.35) / 1.35);
        float rim = pow(saturate(1.0 - ndotv), 2.0);
        float backLight = pow(saturate(dot(-l, v)), 2.0);
        float forwardScatter = pow(saturate(dot(l, v)), 2.0);
        float opticalMask = saturate(max(thinMask, 0.18));
        float scatter = opticalMask * TranslucencyStrength * saturate(0.30 * wrap + 0.35 * rim + 0.20 * backLight + 0.15 * forwardScatter);
        float edge = opticalMask * TranslucencyStrength * saturate(0.55 * rim + 0.30 * backLight + 0.15 * wrap);

        color += baseColor * wrap * lightColor * attenuation;

        float baseLinearRoughness = max(BaseRoughness * BaseRoughness, 0.0001);
        float clearcoatRoughness = lerp(0.089, 0.6, saturate(ClearcoatRoughness));
        float clearcoatLinearRoughness = clearcoatRoughness * clearcoatRoughness;
        float3 localDentinScatter = DentinColor * scatter * BackscatterStrength;
        float3 localEdge = lerp(DentinColor, EnamelTint, 0.65) * edge * EdgeTranslucency;
        float3 localBaseSpec = Specular_BRDF(baseLinearRoughness, 0.04.xxx, ndotv, ndotl, ldoth, ndoth, n, h) * 0.08 * ndotl;
        float dc = Filament_D_GGX(clearcoatLinearRoughness, ndoth, n, h);
        float vc = V_Kelemen(ldoth);
        float fc = Filament_F_Schlick(0.04, ldoth) * SalivaClearcoat;
        float3 localClearcoat = (dc * vc * fc) * ndotl;

        dentinScatter += localDentinScatter * lightColor * attenuation;
        edgeTranslucency += localEdge * lightColor * attenuation;
        clearcoat += localClearcoat * lightColor * attenuation;
        color += (localBaseSpec + localDentinScatter + localEdge + localClearcoat) * lightColor * attenuation;
    }

    color += baseColor * vLightAmbient.rgb * 0.18;
    float clearcoatRoughness = lerp(0.089, 0.6, saturate(ClearcoatRoughness));
    float ambientClearcoatFresnel = Filament_F_Schlick(0.04, saturate(dot(n, v))) * SalivaClearcoat;
    float3 ambientClearcoat = vLightAmbient.rgb * ambientClearcoatFresnel * 0.12;
    if (bHasCubeMap)
        ambientClearcoat = Specular_IBL(n, v, clearcoatRoughness) * ambientClearcoatFresnel;

    clearcoat += ambientClearcoat;
    color += ambientClearcoat;
    return color;
}

float3 ResolveToothNormal(PS_TOOTH_ENAMEL_IN input, bool isFrontFace, float3 viewDirection)
{
    float3 n = input.n;
    float normalLengthSquared = dot(n, n);
    float3 faceNormalRaw = cross(ddy(input.posWS), ddx(input.posWS));
    float faceNormalLengthSquared = dot(faceNormalRaw, faceNormalRaw);
    float3 faceNormal = faceNormalLengthSquared > 0.00000001 ? normalize(faceNormalRaw) : float3(0, 1, 0);

    n = normalLengthSquared > 0.000001 ? normalize(n) : faceNormal;
    if (normalLengthSquared <= 0.000001 && !isFrontFace)
        n = -n;

    if (normalLengthSquared <= 0.000001 && dot(faceNormal, viewDirection) > dot(n, viewDirection) + 0.25)
        n = normalize(lerp(n, faceNormal, 0.65));

    return n;
}

float4 ShadeToothEnamel(PS_TOOTH_ENAMEL_IN input, bool isFrontFace)
{
    float3 v = normalize(input.vEye.xyz);
    float3 n = ResolveToothNormal(input, isFrontFace, v);

    float height01 = ToothHeight01(input.posOS);
    float incisal = smoothstep(0.60, 1.0, height01);
    float borderHint = saturate(input.hints.x);
    float shapeIndex01 = saturate(input.hints.y);
    float curvedness01 = saturate(input.hints.z);
    float concavity = smoothstep(0.58, 0.92, shapeIndex01);
    float curvatureDetail = concavity * curvedness01;
    float thinMask = saturate(max(incisal, curvatureDetail * 0.35 + borderHint * 0.15));
    float enamelMask = saturate(0.52 + 0.48 * max(smoothstep(0.38, 1.0, height01), thinMask));

    float3 dentin = DentinColor * DentinStrength;
    float enamelAmount = saturate(EnamelStrength * enamelMask);
    float3 baseColor = lerp(dentin, EnamelTint, enamelAmount * 0.72);

    float gumGradient = 1.0 - height01;
    float localStainMask = saturate(0.15 + curvatureDetail * 0.85 + curvedness01 * 0.15 + borderHint * 0.35);
    float localStain = StainStrength * localStainMask;
    float gumStain = GumStainStrength * pow(gumGradient, 1.8);
    float stain = saturate(localStain * 0.78 + gumStain * 0.75);
    baseColor = lerp(baseColor, StainColor, stain);

    float micro = frac(sin(dot(input.posOS, float3(12.9898, 78.233, 37.719))) * 43758.5453) - 0.5;
    baseColor *= 1.0 + micro * MicrostructureStrength * 0.08;

    float3 dentinScatter = 0;
    float3 edgeTranslucency = 0;
    float3 clearcoat = 0;
    float3 color = ShadeToothLight(input.posWS, n, v, baseColor, thinMask, dentinScatter, edgeTranslucency, clearcoat);
    float3 withoutClearcoat = color - clearcoat;
    color *= ExposureCompensation;

    if (DebugView == 1)
        return float4(height01.xxx, Alpha);
    if (DebugView == 2)
        return float4(thinMask.xxx, Alpha);
    if (DebugView == 3)
        return float4(enamelMask.xxx, Alpha);
    if (DebugView == 4)
        return float4(saturate(dentin), Alpha);
    if (DebugView == 5)
        return float4(saturate(dentinScatter * ExposureCompensation * 2.0), Alpha);
    if (DebugView == 6)
        return float4(saturate(clearcoat * ExposureCompensation * 4.0), Alpha);
    if (DebugView == 7)
        return float4(saturate(withoutClearcoat * ExposureCompensation), Alpha);
    if (DebugView == 8)
        return float4(stain.xxx, Alpha);
    if (DebugView == 9)
        return float4(shapeIndex01.xxx, Alpha);
    if (DebugView == 10)
        return float4(curvedness01.xxx, Alpha);
    if (DebugView == 11)
        return float4(borderHint.xxx, Alpha);

    return float4(saturate(color), Alpha);
}

#endif

#endif
