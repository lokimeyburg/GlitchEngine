﻿struct PixelInput
{
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
    float2 texCoord : TEXCOORD0;
};

Texture2D shaderTexture;
SamplerState MeshTextureSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    MaxLOD = 0.0f;
    AddressU = Wrap;
    AddressV = Wrap;
};

float4 PS(PixelInput input) : SV_Target
{
    float4 color = shaderTexture.Sample(MeshTextureSampler, input.texCoord);
	return color;
}
