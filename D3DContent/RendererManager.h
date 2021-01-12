#pragma once
#include <map>
#include <functional>
#include "IRenderElement.h"

struct Article;
class CRenderer;

struct TextureData
{
  ID3D11Texture2D* tex;
  int frames;
  int width;
  int height;
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
  void PushArticles(Article* articles, int count);
  void PushLines(Line* lines, int count);
  void PushTilemaps(Tilemap* tilemaps, int count);
  HRESULT RegisterTexture(const LPSTR key, const LPWSTR fname, int frames, TextureData** texture);

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
  std::map<std::string, TextureData*> m_textureAtlas;

  HWND m_hwnd;

  UINT m_uWidth;
  UINT m_uHeight;
  UINT m_uNumSamples;
  bool m_fUseAlpha;
  bool m_fSurfaceSettingsChanged;
  bool m_repeat;
};