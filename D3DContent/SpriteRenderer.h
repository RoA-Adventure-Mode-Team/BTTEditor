#pragma once
#include <DirectXMath.h>
#include <map>
#include <string>
#include <d2d1_2.h>
#include <wincodec.h>

struct Article;
struct Line;
struct Tilemap;

struct TextureData
{
  int frames;
  int height;
  int width;
  ID2D1Bitmap* tex;
};

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
  ID2D1DeviceContext* GetDeviceContext() { return m_deviceContext; }
  void PrepareForRender();

  HRESULT CreateBrush(int color, ID2D1SolidColorBrush** brush);
  HRESULT RegisterTexture(LPSTR key, LPWSTR fname, int frames, TextureData** texture);

private:
  void RenderArticle(Article* article);
  void RenderLine(Line* line);
  void RenderTilemap(Tilemap* tilemap);

private:
  UINT m_width;
  UINT m_height;

  ID2D1Factory2*            m_factory;
  ID2D1Effect*              m_cropEffect;
  ID2D1Effect*              m_colorEffect;
  ID2D1Device*              m_device;
  ID2D1DeviceContext*       m_deviceContext;
  IDXGISwapChain*           m_swapChain;
  ID2D1Bitmap1*             m_targetBitmap;
  ID3D11DeviceContext*      m_3dDeviceContext;
  ID2D1StrokeStyle*         m_dashedStroke;

  IWICImagingFactory2* m_imageFactory;
  std::map<std::string, TextureData*> m_textureAtlas;

  D2D1_MATRIX_3X2_F       m_cameraTrans;
  GUID m_guid;
};