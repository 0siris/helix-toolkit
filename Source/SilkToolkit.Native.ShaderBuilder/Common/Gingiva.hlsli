#ifndef GINGIVA_HLSLI
#define GINGIVA_HLSLI

cbuffer cbGingiva : register(b2)
{
    float3 HealthyColor;
    float Alpha;

    float3 BloodColor;
    int GingivaDebugView;

    float3 PaleColor;
    float BloodAmount;

    float3 PurpleColor;
    float Inflammation;

    float Paleness;
    float PurpleAmount;
    float SssStrength;
    float SssRadius;

    float Thickness;
    float Moisture;
    float WetSpecular;
    float WetRoughness;

    float BaseRoughness;
    float Metallic;
    float Reflectance;
    float MicroBump;

    float CavityDarkness;
    float ExposureCompensation;
    int HasBloodMap;
    int HasMoistureMap;

    int HasRoughnessMap;
    int HasCavityMap;
    int HasThicknessMap;
    float GingivaPadding;

    float3 GypsumBaseColor;
    float GypsumRoughness;

    float3 GypsumChalkColor;
    float GypsumRoughnessVariation;

    float3 GypsumMineralColor;
    float GypsumReflectance;

    float GypsumCoarseNoiseScale;
    float GypsumFineNoiseScale;
    float GypsumRoughnessNoiseScale;
    float GypsumChalkAmount;

    float GypsumMineralAmount;
    float GypsumCavityDarkness;
    float GypsumCavityInfluence;
    float GingivaGypsumPadding;
};

Texture2D<float> texGingivaBloodMap : register(t6);
Texture2D<float> texGingivaMoistureMap : register(t7);
Texture2D<float> texGingivaRoughnessMap : register(t8);
Texture2D<float> texGingivaCavityMap : register(t9);
Texture2D<float> texGingivaThicknessMap : register(t14);

struct PS_GINGIVA_IN
{
    float4 p : SV_POSITION;
    float4 vEye : POSITION0;
    float4 wp : POSITION1;
    float3 posWS : TEXCOORD3;
    float3 n : NORMAL;
    float3 t1 : TANGENT;
    float3 t2 : BINORMAL;
    float2 uv : TEXCOORD0;
    float4 curv : COLOR0;
    float4 hints : COLOR1;
};

#if defined(GINGIVA_PIXEL)

float SafeFinite01(float value, float fallback)
{
    return isfinite(value) ? saturate(value) : fallback;
}

float3 ResolveFaceNormal(PS_GINGIVA_IN input, bool isFrontFace)
{
    float3 normal = normalize(cross(ddy(input.posWS), ddx(input.posWS)));
    if (!isFrontFace)
        normal = -normal;
    return normal;
}

float ComputeMeshConcavity(float4 curvature, float4 hints)
{
    float meanCurvature = isfinite(curvature.x) ? curvature.x : 0.0;
    float kMin = isfinite(curvature.z) ? curvature.z : 0.0;
    float kMax = isfinite(curvature.w) ? curvature.w : 0.0;
    float shapeIndex01 = SafeFinite01(hints.y, 0.5);
    float curvedness01 = SafeFinite01(hints.z, 0.0);
    float signedMean = saturate((-meanCurvature / max(abs(kMin) + abs(kMax), 0.0001)) * 2.0);
    float shapeConcavity = saturate((0.5 - shapeIndex01) * 1.75);
    return saturate(max(signedMean, shapeConcavity) * curvedness01);
}

float ComputeMeshCavity(float4 curvature, float4 hints)
{
    float curvedness01 = SafeFinite01(hints.z, 0.0);
    float concavity = ComputeMeshConcavity(curvature, hints);
    return saturate(curvedness01 * 0.32 + concavity * 0.68);
}

float ComputeMeshBloodMask(float4 curvature, float4 hints)
{
    float curvedness01 = SafeFinite01(hints.z, 0.0);
    float concavity = ComputeMeshConcavity(curvature, hints);
    return saturate(0.45 + concavity * 0.28 + curvedness01 * 0.10);
}

float ComputeMeshThickness(float4 curvature, float4 hints)
{
    float border = SafeFinite01(hints.x, 0.0);
    float cavity = ComputeMeshCavity(curvature, hints);
    return saturate(0.62 - cavity * 0.22 - border * 0.18);
}

float3 ResolveGingivaNormal(PS_GINGIVA_IN input, bool isFrontFace, float moisture)
{
    float3 normal = bRenderFlat ? normalize(cross(ddy(input.posWS), ddx(input.posWS))) : normalize(input.n);
    if (!isFrontFace)
        normal = -normal;

    if (bHasNormalMap)
    {
        float3 localNormal = BiasX2(texNormalMap.Sample(samplerSurface, input.uv).xyz);
        localNormal.xy *= MicroBump * lerp(1.0, 0.65, saturate(moisture));
        localNormal = normalize(localNormal);

        if (bAutoTengent)
        {
            normal = PeturbNormal(localNormal, input.posWS, normal, input.uv);
        }
        else
        {
            float3 tangent = normalize(input.t1);
            float3 bitangent = normalize(input.t2);
            normal = normalize(normal + localNormal.x * tangent + localNormal.y * bitangent);
        }
    }

    return normal;
}

float3 ComputeGingivaColor(float3 albedo, float bloodMask, float cavity)
{
    float bloodInfluence = saturate(bloodMask + Inflammation * 0.45);
    float3 color = lerp(albedo, HealthyColor, 0.35);
    color = lerp(color, BloodColor, bloodInfluence);
    color = lerp(color, PaleColor, Paleness);
    color = lerp(color, PurpleColor, PurpleAmount);
    color *= lerp(1.0, 1.0 - CavityDarkness, cavity);
    return color;
}

float ComputeGingivaRoughness(float roughnessMap, float moisture)
{
    float baseRoughness = saturate(BaseRoughness * roughnessMap);
    return saturate(lerp(baseRoughness, WetRoughness, saturate(moisture)));
}

float3 ComputeGingivaSSS(float3 normalWS, float3 viewDirWS, float3 lightDirWS, float thickness, float bloodMask)
{
    float ndotv = saturate(dot(normalWS, viewDirWS));
    float rim = pow(1.0 - ndotv, max(1.2, SssRadius));
    float backScatter = pow(saturate(dot(-lightDirWS, viewDirWS)), 2.5);
    float scatter = (rim * 0.45 + backScatter * 0.55) * saturate(thickness) * SssStrength;
    float3 softTissueSss = float3(1.0, 0.35, 0.22);
    float3 bloodSss = float3(1.0, 0.08, 0.04);
    return lerp(softTissueSss, bloodSss, bloodMask) * scatter;
}

float3 ComputeWetClearcoat(float3 normalWS, float3 viewDirWS, float3 lightDirWS, float moisture, float lightNdotL)
{
    float3 h = normalize(viewDirWS + lightDirWS);
    float ndoth = saturate(dot(normalWS, h));
    float vdoth = saturate(dot(viewDirWS, h));
    float rough = saturate(WetRoughness);
    float clearcoatRoughness = lerp(0.089, 0.6, rough);
    float linearRoughness = clearcoatRoughness * clearcoatRoughness;
    float d = Filament_D_GGX(linearRoughness, ndoth, normalWS, h);
    float v = V_Kelemen(max(vdoth, 0.01));
    float f = Filament_F_Schlick(0.02, vdoth);
    return (d * v * f) * lightNdotL * saturate(moisture) * WetSpecular;
}

float3 ShadeGingivaLight(float3 posWS, float3 n, float3 viewDir, float3 baseColor, float roughness, float blood, float moisture,
    float thickness, out float3 sssAccum, out float3 wetAccum)
{
    float ndotv = saturate(dot(n, viewDir));
    float alphaRoughness = max(roughness * roughness, 0.0001);
    float3 diffuseColor = lerp(baseColor, float3(0, 0, 0), Metallic);
    float3 specularColor = 0.16 * Reflectance * Reflectance * (1.0 - Metallic) + baseColor * Metallic;
    float3 color = 0;
    sssAccum = 0;
    wetAccum = 0;

    [loop]
    for (int i = 0; i < NumLights; ++i)
    {
        float3 l = 0;
        float attenuation = 1.0;
        float3 lightColor = Lights[i].vLightColor.rgb;

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

        float ndotl = saturate(dot(n, l));
        float3 h = normalize(l + viewDir);
        float ldoth = max(saturate(dot(l, h)), 0.01);
        float ndoth = saturate(dot(n, h));
        float diffuseFactor = Diffuse_Burley(ndotl, ndotv, ldoth, roughness);
        float3 diffuse = diffuseColor * diffuseFactor;
        float3 specular = Specular_BRDF(alphaRoughness, specularColor, ndotv, ndotl, ldoth, ndoth, n, h);
        float3 sss = ComputeGingivaSSS(n, viewDir, l, thickness, blood);
        float3 wet = ComputeWetClearcoat(n, viewDir, l, moisture, ndotl);

        sssAccum += sss * lightColor * attenuation;
        wetAccum += wet * lightColor * attenuation;
        color += (diffuse + specular) * ndotl * lightColor * attenuation + (sss + wet) * lightColor * attenuation;
    }

    color += baseColor * vLightAmbient.rgb * 0.16;

    if (bHasCubeMap)
    {
        float3 specularEnv = Specular_IBL(n, viewDir, roughness);
        color += specularColor * specularEnv * 0.45;
        float wetFresnel = Filament_F_Schlick(0.02, ndotv) * saturate(moisture) * WetSpecular;
        float3 wetEnv = Specular_IBL(n, viewDir, WetRoughness) * wetFresnel;
        wetAccum += wetEnv;
        color += wetEnv;
    }

    return color;
}

float GypsumHash(float3 p)
{
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
}

float GypsumValueNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = GypsumHash(i + float3(0, 0, 0));
    float n100 = GypsumHash(i + float3(1, 0, 0));
    float n010 = GypsumHash(i + float3(0, 1, 0));
    float n110 = GypsumHash(i + float3(1, 1, 0));
    float n001 = GypsumHash(i + float3(0, 0, 1));
    float n101 = GypsumHash(i + float3(1, 0, 1));
    float n011 = GypsumHash(i + float3(0, 1, 1));
    float n111 = GypsumHash(i + float3(1, 1, 1));

    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);
    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);
    return lerp(nxy0, nxy1, f.z);
}

float3 ComputeGypsumColor(float3 posWS, float cavity)
{
    float coarse = GypsumValueNoise(posWS * max(GypsumCoarseNoiseScale, 0.0001));
    float fine = GypsumValueNoise(posWS * max(GypsumFineNoiseScale, 0.0001));
    float powder = fine * 0.65 + coarse * 0.35;

    float3 color = lerp(GypsumBaseColor, GypsumChalkColor, powder * saturate(GypsumChalkAmount));
    color = lerp(color, GypsumMineralColor, saturate((1.0 - powder) * GypsumMineralAmount));
    color *= lerp(1.0, 1.0 - saturate(GypsumCavityDarkness), saturate(cavity) * saturate(GypsumCavityInfluence));
    return color;
}

float ComputeGypsumRoughness(float3 posWS)
{
    float fine = GypsumValueNoise(posWS * max(GypsumRoughnessNoiseScale, 0.0001));
    return saturate(GypsumRoughness + (fine - 0.5) * GypsumRoughnessVariation);
}

float3 ShadeGypsumLight(float3 posWS, float3 n, float3 viewDir, float3 baseColor, float roughness)
{
    float ndotv = saturate(dot(n, viewDir));
    float alphaRoughness = max(roughness * roughness, 0.0001);
    float3 diffuseColor = baseColor;
    float3 specularColor = saturate(GypsumReflectance).xxx;
    float3 color = 0;

    [loop]
    for (int i = 0; i < NumLights; ++i)
    {
        float3 l = 0;
        float attenuation = 1.0;
        float3 lightColor = Lights[i].vLightColor.rgb;

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

        float ndotl = saturate(dot(n, l));
        float3 h = normalize(l + viewDir);
        float ldoth = max(saturate(dot(l, h)), 0.01);
        float ndoth = saturate(dot(n, h));
        float diffuseFactor = Diffuse_Burley(ndotl, ndotv, ldoth, roughness);
        float3 diffuse = diffuseColor * diffuseFactor;
        float3 specular = Specular_BRDF(alphaRoughness, specularColor, ndotv, ndotl, ldoth, ndoth, n, h);
        color += (diffuse + specular * 0.55) * ndotl * lightColor * attenuation;
    }

    color += baseColor * vLightAmbient.rgb * 0.24;

    if (bHasCubeMap)
    {
        float3 specularEnv = Specular_IBL(n, viewDir, roughness);
        color += specularColor * specularEnv * 0.16;
    }

    return color;
}

float3 ShadeGypsumRegion(PS_GINGIVA_IN input, float3 flatNormal, float meshCavity, out float roughness)
{
    float cavity = HasCavityMap != 0
        ? saturate(meshCavity * 0.55 + texGingivaCavityMap.Sample(samplerSurface, input.uv).r * 0.45)
        : meshCavity;

    float3 gypsumColor = ComputeGypsumColor(input.posWS, cavity);
    roughness = ComputeGypsumRoughness(input.posWS);
    float3 viewDir = normalize(input.vEye.xyz);
    float3 color = ShadeGypsumLight(input.posWS, flatNormal, viewDir, gypsumColor, roughness);
    return saturate(color * ExposureCompensation);
}

float4 ShadeGingiva(PS_GINGIVA_IN input, bool isFrontFace)
{
    float2 uv = input.uv;
    float3 viewDir = normalize(input.vEye.xyz);
    // Organic mask convention: 0 = gypsum, 0.5 = transition/trim line, 1 = gum.
    float organicMask = SafeFinite01(input.hints.w, 1.0);
    float3 flatNormal = ResolveFaceNormal(input, isFrontFace);
    float3 albedo = HealthyColor;
    if (bHasDiffuseMap)
        albedo *= texDiffuseMap.Sample(samplerSurface, uv).rgb;

    float border = SafeFinite01(input.hints.x, 0.0);
    float meshBlood = ComputeMeshBloodMask(input.curv, input.hints);
    float meshCavity = ComputeMeshCavity(input.curv, input.hints);
    float meshThickness = ComputeMeshThickness(input.curv, input.hints);
    float edgeDamping = lerp(1.0, 0.72, border);

    float gypsumRoughness = 0.88;
    float3 gypsumColor = ShadeGypsumRegion(input, flatNormal, meshCavity, gypsumRoughness);

    if (GingivaDebugView == 10)
        return float4(organicMask.xxx, Alpha);
    if (GingivaDebugView == 11)
        return float4(gypsumColor, Alpha);

    float bloodMap = HasBloodMap != 0 ? lerp(meshBlood, texGingivaBloodMap.Sample(samplerSurface, uv).r, 0.85) : meshBlood;
    float moistureMap = HasMoistureMap != 0 ? texGingivaMoistureMap.Sample(samplerSurface, uv).r : 1.0;
    float roughnessMap = HasRoughnessMap != 0 ? texGingivaRoughnessMap.Sample(samplerSurface, uv).r : 1.0;
    float cavity = HasCavityMap != 0 ? saturate(meshCavity * 0.35 + texGingivaCavityMap.Sample(samplerSurface, uv).r * 0.85) : meshCavity;
    float thickness = HasThicknessMap != 0 ? lerp(meshThickness, texGingivaThicknessMap.Sample(samplerSurface, uv).r, 0.85) : meshThickness;

    float blood = saturate(BloodAmount * bloodMap);
    float moisture = saturate(Moisture * moistureMap * lerp(1.0, 0.82, border));
    float finalThickness = saturate(Thickness * thickness * edgeDamping);
    float roughness = ComputeGingivaRoughness(roughnessMap, moisture);
    float3 n = ResolveGingivaNormal(input, isFrontFace, moisture);
    float3 tissueColor = ComputeGingivaColor(albedo, blood, saturate(cavity));

    float3 sss = 0;
    float3 wet = 0;
    float3 color = ShadeGingivaLight(input.posWS, n, viewDir, tissueColor, roughness, blood, moisture, finalThickness, sss, wet);
    float3 withoutWet = color - wet;
    color *= lerp(1.0, 1.0 - CavityDarkness, saturate(cavity) * 0.35);
    color *= ExposureCompensation;

    if (GingivaDebugView == 1)
        return float4(saturate(tissueColor), Alpha);
    if (GingivaDebugView == 2)
        return float4(blood.xxx, Alpha);
    if (GingivaDebugView == 3)
        return float4(moisture.xxx, Alpha);
    if (GingivaDebugView == 4)
        return float4(roughness.xxx, Alpha);
    if (GingivaDebugView == 5)
        return float4(saturate(sss * ExposureCompensation * 2.0), Alpha);
    if (GingivaDebugView == 6)
        return float4(saturate(wet * ExposureCompensation * 4.0), Alpha);
    if (GingivaDebugView == 7)
        return float4(cavity.xxx, Alpha);
    if (GingivaDebugView == 8)
        return float4(finalThickness.xxx, Alpha);
    if (GingivaDebugView == 9)
        return float4(saturate(withoutWet * ExposureCompensation), Alpha);

    float organicBlend = smoothstep(0.0, 1.0, organicMask);
    return float4(saturate(lerp(gypsumColor, color, organicBlend)), Alpha);
}

#endif

#endif
