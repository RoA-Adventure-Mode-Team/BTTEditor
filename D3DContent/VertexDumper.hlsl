cbuffer cbPerObject : register(b0)
{
	matrix		g_mWorldView;
	matrix		g_mWorld;
	float4		g_color;
};


struct VS_INPUT
{
	float4 Position : SV_POSITION;
	float2 TextureUV : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position : SV_POSITION;
	float4 Diffuse : COLOR0;
	float2 TextureUV : TEXCOORD0;
};


VS_OUTPUT main( VS_INPUT Input )
{
	VS_OUTPUT Output;
	Output.Position = mul(Input.Position, g_mWorld);
	Output.Diffuse = g_color;
	Output.TextureUV = Input.TextureUV;

	return Output;
}