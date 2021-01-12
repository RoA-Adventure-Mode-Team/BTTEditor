Texture2D tex0;
SamplerState tex_sampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};
Texture2D tex1;
SamplerState tmap_sampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
}; 

cbuffer cbPerObject : register(b0)
{
	// Size of each square in the tilemap, in UV units (0-1)
	float2 tmap_size;
	// Number of tiles in one row of the tileset
	int tileset_span;
	// Size of each tile in the tileset, in UV units
	float2 tileset_size;
};



float4 main(float2 tex : TEXCOORD0 ) : SV_TARGET
{
	int tilemap_val = tex1.Sample(tmap_sampler, tex);
	// Find the X and Y position of the current tile in the tileset, as a conceptual 2D grid
	int2 tileset_pos = int2(tilemap_val % tileset_span, tilemap_val / tileset_span);
	// Find the UV position of the top left of this tile
	float2 tileset_start = float2(tileset_pos.x * tileset_size.x, tileset_pos.y * tileset_size.y);
	// Get the offset of this pixel from the top left of this tile in the range [0-1)
	float2 tileset_offset = float2((tex.x / tmap_size.x) - tileset_pos.x, (tex.y / tmap_size.y) - tileset_pos.y);
	return tex0.Sample(tex_sampler, tileset_start + tileset_offset);
}