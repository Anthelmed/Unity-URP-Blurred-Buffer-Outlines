#ifndef BLUR_UTILS_INCLUDED
#define BLUR_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

half3 GaussianBlur(Texture2D blitTexture, SamplerState samplerState, half2 uv, half2 pixelOffset)
{
    half3 color = 0;

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] ={
        0.44908,
        0.05092
     };
    const half gOffsets[stepCount] ={
        0.53805,
        2.06278
     };

    UNITY_UNROLL
    for( int i = 0; i < stepCount; i++ )
    {
        half2 texCoordOffset = gOffsets[i] * pixelOffset;
        half4 p1 = SAMPLE_TEXTURE2D(blitTexture, samplerState, uv + texCoordOffset);
        half4 p2 = SAMPLE_TEXTURE2D(blitTexture, samplerState, uv - texCoordOffset);
        half4 col = p1 + p2;
        color += gWeights[i] * col;
    }

    return color;
}

void HorizontalGaussianBlur_half(Texture2D blitTexture, SamplerState samplerState, half2 uv, half2 texelSize, out half3 colOut)
{
    half2 delta = half2(texelSize.x, 0);

    colOut = GaussianBlur(blitTexture, samplerState, uv, delta);
}

void VerticalGaussianBlur_half(Texture2D blitTexture, SamplerState samplerState, half2 uv, half2 texelSize, out half3 colOut)
{
    half2 delta = half2(0, texelSize.y);

    colOut = GaussianBlur(blitTexture, samplerState, uv, delta);
}

#endif
