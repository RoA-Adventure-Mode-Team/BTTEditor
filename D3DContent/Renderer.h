#pragma once
#include <vector>

class CRenderer
{
  friend class CRendererManager;
public:
  virtual ~CRenderer();

  HRESULT CheckDeviceState();
  HRESULT CreateSurface(UINT uWidth, UINT uHeight, bool fUseAlpha, UINT m_uNumSamples);

  virtual HRESULT Render() = 0;

  IDirect3DSurface9* GetSurfaceNoRef() { return m_pd3dRTS; }
  ID3DXSprite *GetSprite() { return m_renderSprite; }
  ID3DXLine *GetLine() { return m_renderLine; }
  void PushLine(D3DXVECTOR2 start, D3DXVECTOR2 end, float width, int color) { lines.push_back(Line(start, end, width, color)); }
  void SetTrans(D3DXMATRIX mat) { m_cameraTrans = mat; }
  const D3DXMATRIX &GetTrans() { return m_cameraTrans; }

protected:
  CRenderer();

  virtual HRESULT Init(IDirect3D9* pD3D, IDirect3D9Ex* pD3DEx, HWND hwnd, UINT uAdapter);

  IDirect3DDevice9* m_pd3dDevice;
  IDirect3DDevice9Ex* m_pd3dDeviceEx;

  IDirect3DSurface9* m_pd3dRTS;
  D3DXMATRIX m_cameraTrans;
  ID3DXSprite *m_renderSprite;
  ID3DXLine *m_renderLine;

  std::vector<Line> lines;
};