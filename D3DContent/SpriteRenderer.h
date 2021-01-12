#pragma once
#include <DirectXMath.h>

struct Article;
struct Line;
struct Tilemap;

class SpriteRenderer
{
public:
  void Init();
  void PrepareDraw();
  void Render(Article* articles, int art_count, Line* lines, int line_count, Tilemap* tilemaps, int tilemap_count);
private:
  void RenderArticle(Article* article);
  void RenderLine(Line* line);
  void RenderTilemap(Tilemap* tilemap);

private:
  ID3D11Device*         m_device;
  ID3D11DeviceContext*  m_deviceContext;
  IDXGISwapChain*       m_swapChain;
  ID3D11Buffer*         m_vertexBuffer;

  ID3D11VertexShader* m_vertexShader;
  ID3D11InputLayout*  m_spriteLayout;
  ID3D11PixelShader*  m_spriteShader;
  ID3D11PixelShader*  m_tilemapShader;
  ID3D11Buffer*       m_VSConstantBuffer;
  ID3D11Buffer*       m_PSTilemapConstantBuffer;

  ID3D11SamplerState*       m_samplerState;
  ID3D11Texture2D*          m_tilemapTex;
  ID3D11ShaderResourceView* m_textureView;
  ID3D11ShaderResourceView* m_tilemapTexView;

  DirectX::XMMATRIX   m_cameraTrans;
};