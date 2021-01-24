#pragma once
#include <DirectXMath.h>
#include <map>
#include <string>
#include <d2d1_2.h>
#include <wincodec.h>

struct TextureData
{
  ID2D1Bitmap* tex;
  int frames;
  int width;
  int height;
};

struct Article;
struct Line;
struct Tilemap;

class SpriteRenderer
{
public:
  SpriteRenderer(HWND hwnd);
  void Init(HWND hwnd);
  void Render(Article* articles, int art_count, Line* lines, int line_count, Tilemap* tilemaps, int tilemap_count);
  void SetCameraTrans(D2D1_MATRIX_3X2_F transform) { m_cameraTrans = transform; }

  int GetWidth() const { return m_width; }
  int GetHeight() const { return m_height; }
  HRESULT Resize(int width, int height);

  HRESULT RegisterTexture(const LPSTR key, const LPWSTR fname, int frames, TextureData** texture);
private:
  void RenderArticle(Article* article);
  void RenderLine(Line* line);
  void RenderTilemap(Tilemap* tilemap);

private:
  UINT m_width;
  UINT m_height;

  ID2D1Factory2*            m_factory;
  IWICImagingFactory2*      m_imageFactory;
  ID2D1Effect*              m_cropEffect;
  ID2D1Effect*              m_colorEffect;
  ID2D1Device*              m_device;
  ID2D1DeviceContext*       m_deviceContext;
  IDXGISwapChain*           m_swapChain;
  ID2D1Bitmap1*             m_targetBitmap;
  ID3D11DeviceContext*      m_3dDeviceContext;

  D2D1_MATRIX_3X2_F       m_cameraTrans;

  std::map<std::string, TextureData*> m_textureAtlas;
  GUID m_guid;
};