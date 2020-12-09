#pragma once
#include <map>
#include <functional>

class CRenderer;

struct TextureData
{
  LPDIRECT3DTEXTURE9 tex;
  int frames;
  int width;
  int height;
};

struct Vector2
{
  double x;
  double y;
};

struct Article
{
  LPSTR sprite;
  Vector2 translate;
  Vector2 scale;
  float depth;
  int color;
};

struct Line
{
  Line(D3DXVECTOR2 start, D3DXVECTOR2 end, float _width, int _color) :
    line{ start, end },
    width(_width),
    color(_color) { }
  D3DXVECTOR2 line[2];
  float width;
  int color;
};

struct strequal : std::binary_function <LPWSTR, LPWSTR, bool> {
  bool operator() (LPWSTR x, LPWSTR y) const { return lstrcmpW(x, y) > 0; }
};

class CRendererManager
{
public:
  static HRESULT Create(CRendererManager** ppManager);
  ~CRendererManager();

  HRESULT EnsureDevices();

  void SetSize(UINT uWidth, UINT uHeight);
  void SetAlpha(bool fUseAlpha);
  void SetNumDesiredSamples(UINT uNumSamples);
  void SetAdapter(POINT screenSpacePoint);
  void SetCameraTransform(D3DXMATRIX mat);
  void StartDraw();
  void DrawArticle(Article art);
  void DrawLine(Vector2 start, Vector2 end, int color, float width);
  HRESULT RegisterTexture(const LPSTR key, const LPWSTR fname, int frames);

  HRESULT GetBackBufferNoRef(IDirect3DSurface9** ppSurface);

  HRESULT Render();

private:
  CRendererManager();

  void CleanupInvalidDevices();
  HRESULT EnsureRenderers();
  HRESULT EnsureHWND();
  HRESULT EnsureD3DObjects();
  HRESULT TestSurfaceSettings();
  void DestroyResources();

  IDirect3D9* m_pD3D;
  IDirect3D9Ex* m_pD3DEx;

  UINT m_cAdapters;
  CRenderer** m_rgRenderers;
  CRenderer* m_pCurrentRenderer;
  std::map<std::string, TextureData> m_textureAtlas;

  HWND m_hwnd;

  UINT m_uWidth;
  UINT m_uHeight;
  UINT m_uNumSamples;
  bool m_fUseAlpha;
  bool m_fSurfaceSettingsChanged;
  bool m_repeat;
};