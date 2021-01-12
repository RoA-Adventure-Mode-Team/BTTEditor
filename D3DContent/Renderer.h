#pragma once
#include <vector>
#include "IRenderElement.h"

class CRenderer
{
  friend class CRendererManager;
public:
  virtual ~CRenderer();

  HRESULT CheckDeviceState();
  HRESULT CreateSurface(UINT uWidth, UINT uHeight, bool fUseAlpha, UINT m_uNumSamples);

  virtual HRESULT Render() = 0;

  IDirect3DSurface9* GetSurfaceNoRef() { return m_pd3dRTS; }
  void SetTrans(D3DXMATRIX mat) { m_cameraTrans = mat; }
  const D3DXMATRIX &GetTrans() { return m_cameraTrans; }

  void SetArticles(Article* articles, int count) { m_articles = articles; m_article_count = count; }
  void SetLines(Line* lines, int count) { m_lines = lines; m_line_count = count; }
  void SetTilemaps(Tilemap* tilemaps, int count) { m_tilemaps = tilemaps; m_tilemap_count = count; }

  void InitTexture(int width, int height);
  ID3D11Texture2D* GetTilemapTexture();

protected:
  CRenderer();

  virtual HRESULT Init(IDirect3D9* pD3D, IDirect3D9Ex* pD3DEx, HWND hwnd, UINT uAdapter);

  ID3D11Device* m_pd3dDevice;
  ID3D11DeviceEx* m_pd3dDeviceEx;

  IDirect3DSurface9* m_pd3dRTS;
  D3DXMATRIX m_cameraTrans;

  
  ID3D11Texture2D* m_tilemapTexture;

  Article* m_articles;
  int m_article_count;
  Line* m_lines;
  int m_line_count;
  Tilemap* m_tilemaps;
  int m_tilemap_count;
};