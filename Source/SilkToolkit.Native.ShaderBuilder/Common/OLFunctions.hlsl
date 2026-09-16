#ifndef FUNCTIONS_FX
#define FUNCTIONS_FX


//make areas around negative curvature darker
void shadeCurvature(inout float3 color, float4 curvature, float strength = 1.0f) {
	if (curvature.z < 0) {
		color = color * (1 - strength) + strength * color * ((curvature.z + 1) * 0.2 + 0.8);
	}
		
}


#if defined(MESH)

//simple deffuse shading function 
//wich uses the view and normal direction as the ambient and diffuse color
//to calculate the shading of an pixel
float4 shadeSimple(in float3 normal){
	float3 lookDir = -normalize(float3(mView._13_23_33));
	float4 Ia = 0.1f * vMaterialAmbient;

	float s = (1 + dot(normal, lookDir)) / 2.0;
	float4 Id = vMaterialDiffuse * s + (1 - s) * vMaterialSpecular;

	return float4(saturate(Ia.xyz + Id.xyz), vMaterialDiffuse.a);
}


inline void instantiate(inout float4 position, inout float3 normal, inout float3 t1, inout float3 t2, matrix mInstance){
	// compose instance matrix									
	if (bHasInstances) {
		//matrix mInstance = { mr0,mr1,mr2,mr3 };
		position = mul(position, mInstance);
		normal = mul(normal, (float3x3) mInstance);

        if (bHasNormalMap)        {
            if (!bAutoTengent)            {
                t1 = mul(t1, (float3x3) mInstance);
                t2 = mul(t2, (float3x3) mInstance);
            }
        }
    }
}

inline void invertNormalIfNeeded(inout float3 normal){
    if (bInvertNormal){
        normal = -normal;
    }
}

#endif 

#endif
