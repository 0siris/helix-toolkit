#define WPF_PREMULTIPLIED_OUTPUT
#include "psScreenDup.hlsl"

float4 mainPremultiplied(ScreenDupVS_INPUT input) : SV_Target {
    float4 color = main(input);
    // Only the productive desktop profile binds b0 and identifies undefined desktop alpha.
    if (IsPremultipliedOutput() && (OITWeightMode & 0x20000000) != 0) color.a = 1;
    return color;
}
