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
    float CrownOnly;

    float3 IncisalColor;
    float OpalescenceStrength;

    float PearlStrength;
    float SheenStrength;
    float SubsurfaceStrength;
    float IncisalHaloStrength;

    float CrownCervical;
    float CrownTip;
    float CompositeVertexColorBlending;
    float CrownSpanPadding;
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

// Height along the crown from cervical (0) to the incisal edge or cusp tip (1). The CPU measures the crown span along
// the tooth axis from the mesh vertices; without it the original root-to-crown height stands in.
float CrownHeight01(float3 posOS, float height01)
{
    float3 axis = ToothCrown - ToothRoot;
    float axisLength = length(axis);
    if (CrownTip <= CrownCervical + 0.0001 || axisLength <= 0.0001)
        return saturate((height01 - 0.40) / 0.60);

    float along = dot(posOS - ToothRoot, axis / axisLength);
    return saturate((along - CrownCervical) / (CrownTip - CrownCervical));
}

float ToothHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

// Smooth value noise in object space millimetres for low-frequency enamel mottling.
float ToothValueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float x00 = lerp(ToothHash(cell), ToothHash(cell + float3(1, 0, 0)), f.x);
    float x10 = lerp(ToothHash(cell + float3(0, 1, 0)), ToothHash(cell + float3(1, 1, 0)), f.x);
    float x01 = lerp(ToothHash(cell + float3(0, 0, 1)), ToothHash(cell + float3(1, 0, 1)), f.x);
    float x11 = lerp(ToothHash(cell + float3(0, 1, 1)), ToothHash(cell + float3(1, 1, 1)), f.x);
    return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
}

// Angle-dependent pearl tint: a soft thin-film-like hue drift that stays white when PearlStrength is zero.
float3 PearlTint(float ndotv)
{
    float3 hue = 0.5 + 0.5 * cos(6.2831853 * (ndotv * 1.15 + float3(0.02, 0.36, 0.68)));
    return lerp(1.0.xxx, 0.72 + 0.56 * hue, saturate(PearlStrength));
}

// Keeps mid tones linear and rolls highlights off smoothly instead of clipping them to flat white.
float3 SoftClipToothColor(float3 color)
{
    const float knee = 0.78;
    float3 over = max(color - knee, 0.0);
    return min(color, knee) + (1.0 - knee) * (1.0 - exp(-over / (1.0 - knee)));
}

float3 ShadeToothLight(float3 posWS, float3 n, float3 v, float3 baseColor, float thinMask, float opalMask,
    out float3 dentinScatter, out float3 edgeTranslucency, out float3 clearcoat, out float3 sheen)
{
    float3 color = 0;
    dentinScatter = 0;
    edgeTranslucency = 0;
    clearcoat = 0;
    sheen = 0;
    // Red light travels farther in dentin than blue, so the diffuse terminator wraps wider in red.
    float3 channelWrap = SubsurfaceStrength * float3(0.55, 0.36, 0.24) + 0.08;
    float ndotvShared = saturate(dot(n, v));
    float3 pearl = PearlTint(ndotvShared);
    // Opalescent enamel scatters short wavelengths back and lets long wavelengths through.
    float3 opalTransmission = float3(1.0, 0.62, 0.30);

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
        float3 diffuseWrap = saturate((ndotlRaw + channelWrap) / (1.0 + channelWrap));

        color += baseColor * diffuseWrap * lightColor * attenuation;

        float sheenLobe = pow(ndoth, 18.0) * 0.55 + pow(ndoth, 4.0) * 0.12;
        float3 localSheen = SheenStrength * sheenLobe * ndotl * pearl;
        float3 localOpal = opalTransmission * opalMask * OpalescenceStrength * TranslucencyStrength *
                           (0.65 * backLight + 0.35 * forwardScatter * rim);

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
        edgeTranslucency += (localEdge + localOpal) * lightColor * attenuation;
        clearcoat += localClearcoat * lightColor * attenuation;
        sheen += localSheen * lightColor * attenuation;
        color += (localBaseSpec + localDentinScatter + localEdge + localOpal + localClearcoat + localSheen) *
                 lightColor * attenuation;
    }

    color += baseColor * vLightAmbient.rgb * 0.18;
    float3 pearlRim = SheenStrength * 0.35 * pow(1.0 - ndotvShared, 3.0) * pearl * vLightAmbient.rgb;
    sheen += pearlRim;
    color += pearlRim;
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

// Appliance composite (attachments, bite supports): a tinted, slightly translucent resin in the same saliva film as
// the teeth. Constant buffer roles: DentinColor = body color, EnamelTint = tooth color seen through thin edges,
// ToothBoundsMin/Max.z = the appliance height range in its own space (Z points away from the tooth),
// EdgeTranslucency = how far the tooth color shows through thin edges,
// BaseRoughness = soft sheen roughness, SalivaClearcoat and ClearcoatRoughness = wet film, SheenStrength and
// SubsurfaceStrength as for enamel.
float4 ShadeComposite(PS_TOOTH_ENAMEL_IN input, bool isFrontFace)
{
    float3 v = normalize(input.vEye.xyz);

    // gsComposite already replaced crease normals per triangle; the interpolated normal is valid as it is.
    float3 n = normalize(isFrontFace ? input.n : -input.n);

    // Where the resin runs out thin toward the tooth, the tooth color shows through.
    float heightRange = max(ToothBoundsMax.z - ToothBoundsMin.z, 0.0001);
    float thin = 1.0 - saturate((input.posOS.z - ToothBoundsMin.z) / heightRange);
    // Thick resin looks deeper and more saturated, thin resin lighter.
    float3 body = DentinColor * lerp(1.06, 0.90, 1.0 - thin);
    float3 albedo = lerp(body, lerp(body, EnamelTint, 0.7),
                         smoothstep(0.70, 1.0, thin) * 0.75 * saturate(EdgeTranslucency));
    // Analysis views such as the undercut display color appliances per vertex; the vertex shader passes it in curv.
    albedo = lerp(albedo, input.curv.rgb, saturate(CompositeVertexColorBlending));

    // Facet changes across a pixel widen the highlights instead of producing sparkles.
    float3 dndx = ddx(n);
    float3 dndy = ddy(n);
    float variance = min(4.0 * (dot(dndx, dndx) + dot(dndy, dndy)), 0.30);
    float coatBase = lerp(0.089, 0.6, saturate(ClearcoatRoughness));
    float coatRoughness = sqrt(saturate(coatBase * coatBase + variance));
    float sheenRoughness = sqrt(saturate(BaseRoughness * BaseRoughness + variance));
    float3 channelWrap = SubsurfaceStrength * float3(0.34, 0.28, 0.22);
    float ndv = saturate(dot(n, v));
    float3 color = 0;

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
            l = Lights[i].vLightPos.xyz - input.posWS;
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
        float3 h = normalize(l + v);
        float ndoth = saturate(dot(n, h));
        float ldoth = max(saturate(dot(l, h)), 0.01);
        float3 diffuse = albedo * saturate((ndotlRaw + channelWrap) / (1.0 + channelWrap));
        float coat = Filament_D_GGX(coatRoughness * coatRoughness, ndoth, n, h) * V_Kelemen(ldoth) *
                     Filament_F_Schlick(0.04, ldoth) * SalivaClearcoat * ndotl;
        float sheen = Filament_D_GGX(sheenRoughness * sheenRoughness, ndoth, n, h) * SheenStrength * 0.25 * ndotl;
        color += (diffuse + coat.xxx + albedo * sheen) * lightColor * attenuation;
    }

    color += albedo * vLightAmbient.rgb * 0.18;
    color += lerp(albedo, 1.0.xxx, 0.45) * pow(1.0 - ndv, 3.0) * 0.20 * vLightAmbient.rgb;
    return float4(SoftClipToothColor(color * ExposureCompensation), Alpha);
}

float4 ShadeToothEnamel(PS_TOOTH_ENAMEL_IN input, bool isFrontFace)
{
    float3 v = normalize(input.vEye.xyz);
    float3 n = ResolveToothNormal(input, isFrontFace, v);
    bool crownOnly = CrownOnly > 0.5;

    float height01 = ToothHeight01(input.posOS);
    float crownHeight = CrownHeight01(input.posOS, height01);
    float incisal = smoothstep(0.60, 1.0, height01);
    float borderHint = saturate(input.hints.x);
    float shapeIndex01 = saturate(input.hints.y);
    float curvedness01 = saturate(input.hints.z);
    float concavity = smoothstep(0.58, 0.92, shapeIndex01);
    float curvatureDetail = concavity * curvedness01;
    float incisalThin = smoothstep(0.55, 1.0, crownHeight);
    float thinMask = saturate(max(crownOnly ? incisalThin : incisal, curvatureDetail * 0.35 + borderHint * 0.15));
    float enamelMask = crownOnly ? 1.0
        : saturate(0.52 + 0.48 * max(smoothstep(0.38, 1.0, height01), thinMask));

    // Layered shade: dentin chroma shows through the thin cervical enamel, the body is enamel over dentin and the
    // thin incisal third turns opalescent grey-blue with a brighter halo along the edge.
    float3 dentin = DentinColor;
    float3 body = lerp(dentin, EnamelTint, saturate(EnamelStrength * enamelMask));
    float cervical = (1.0 - smoothstep(0.0, 0.48, crownHeight)) * saturate(DentinStrength);
    float3 baseColor = lerp(body, dentin, cervical);
    float opalMask = incisalThin * saturate(0.55 + 0.45 * (1.0 - curvatureDetail));
    float3 incisalShade = lerp(baseColor, IncisalColor, opalMask * saturate(OpalescenceStrength));
    // The halo is a thin bright line where the incisal edge turns away from the viewer.
    float edgeFacing = pow(1.0 - saturate(dot(n, v)), 2.0);
    float halo = incisalThin * incisalThin * incisalThin * edgeFacing * IncisalHaloStrength * 1.6;
    baseColor = lerp(incisalShade, EnamelTint * 1.04, saturate(halo));

    float gumGradient = 1.0 - height01;
    float localStainMask = saturate(0.15 + curvatureDetail * 0.85 + curvedness01 * 0.15 + borderHint * 0.35);
    float localStain = StainStrength * localStainMask;
    float gumStain = crownOnly ? 0.0 : GumStainStrength * pow(gumGradient, 1.8);
    float stain = saturate(localStain * 0.78 + gumStain * 0.75);
    baseColor = lerp(baseColor, StainColor, stain);

    float mottle = ToothValueNoise(input.posOS * 0.85) * 0.65 + ToothValueNoise(input.posOS * 2.6) * 0.35 - 0.5;
    baseColor *= 1.0 + mottle * MicrostructureStrength * 0.45;
    baseColor = lerp(baseColor, dentin, saturate(mottle) * MicrostructureStrength * 0.6);

    float3 dentinScatter = 0;
    float3 edgeTranslucency = 0;
    float3 clearcoat = 0;
    float3 sheen = 0;
    float3 color = ShadeToothLight(input.posWS, n, v, baseColor, thinMask, opalMask, dentinScatter,
                                   edgeTranslucency, clearcoat, sheen);
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
        return float4(SoftClipToothColor(withoutClearcoat * ExposureCompensation), Alpha);
    if (DebugView == 8)
        return float4(stain.xxx, Alpha);
    if (DebugView == 9)
        return float4(shapeIndex01.xxx, Alpha);
    if (DebugView == 10)
        return float4(curvedness01.xxx, Alpha);
    if (DebugView == 11)
        return float4(borderHint.xxx, Alpha);
    if (DebugView == 12)
        return float4(crownHeight.xxx, Alpha);
    if (DebugView == 13)
        return float4(opalMask.xxx, Alpha);
    if (DebugView == 14)
        return float4(saturate(sheen * ExposureCompensation * 2.0), Alpha);
    if (DebugView == 15)
        return float4(saturate(edgeTranslucency * ExposureCompensation * 2.0), Alpha);

    return float4(SoftClipToothColor(color), Alpha);
}

#endif

#endif
