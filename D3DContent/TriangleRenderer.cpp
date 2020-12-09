//+-----------------------------------------------------------------------------
//
//  CTriangleRenderer
//
//      Subclass of CRenderer that renders a single, spinning triangle
//
//------------------------------------------------------------------------------

#include "stdafx.h"

struct CUSTOMVERTEX
{
  FLOAT x, y, z;
  DWORD color;
};

#define D3DFVF_CUSTOMVERTEX (D3DFVF_XYZ | D3DFVF_DIFFUSE)

//+-----------------------------------------------------------------------------
//
//  Member:
//      CTriangleRenderer ctor
//
//------------------------------------------------------------------------------
CTriangleRenderer::CTriangleRenderer() : CRenderer()
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CTriangleRenderer dtor
//
//------------------------------------------------------------------------------
CTriangleRenderer::~CTriangleRenderer()
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CTriangleRenderer::Create
//
//  Synopsis:
//      Creates the renderer
//
//------------------------------------------------------------------------------
HRESULT
CTriangleRenderer::Create(IDirect3D9* pD3D, IDirect3D9Ex* pD3DEx, HWND hwnd, UINT uAdapter, CRenderer** ppRenderer)
{
  HRESULT hr = S_OK;

  CTriangleRenderer* pRenderer = new CTriangleRenderer();
  IFCOOM(pRenderer);

  IFC(pRenderer->Init(pD3D, pD3DEx, hwnd, uAdapter));

  *ppRenderer = pRenderer;
  pRenderer = NULL;

Cleanup:
  delete pRenderer;

  return hr;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CTriangleRenderer::Init
//
//  Synopsis:
//      Override of CRenderer::Init that calls base to create the device and 
//      then creates the CTriangleRenderer-specific resources
//
//------------------------------------------------------------------------------
HRESULT
CTriangleRenderer::Init(IDirect3D9* pD3D, IDirect3D9Ex* pD3DEx, HWND hwnd, UINT uAdapter)
{
  HRESULT hr = S_OK;
  {
    D3DXMATRIXA16 matView, matProj;
    D3DXVECTOR3 vEyePt(0.0f, 0.0f, -5.0f);
    D3DXVECTOR3 vLookatPt(0.0f, 0.0f, 0.0f);
    D3DXVECTOR3 vUpVec(0.0f, 1.0f, 0.0f);

    // Call base to create the device and render target
    IFC(CRenderer::Init(pD3D, pD3DEx, hwnd, uAdapter));


    // Set up the camera
    D3DXMatrixLookAtLH(&matView, &vEyePt, &vLookatPt, &vUpVec);
    IFC(m_pd3dDevice->SetTransform(D3DTS_VIEW, &matView));

    // Set up the global state
    IFC(m_pd3dDevice->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_LIGHTING, FALSE));
    IFC(m_pd3dDevice->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_POINT));
    IFC(D3DXCreateSprite(m_pd3dDevice, &m_renderSprite));
    IFC(D3DXCreateLine(m_pd3dDevice, &m_renderLine));
  }
Cleanup:
  return hr;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CTriangleRenderer::Render
//
//  Synopsis:
//      Renders the rotating triangle
//
//------------------------------------------------------------------------------
HRESULT
CTriangleRenderer::Render()
{
  HRESULT state = CheckDeviceState();
  HRESULT hr = S_OK;
    {
    D3DXMATRIXA16 matWorld;

    IFC(m_pd3dDevice->BeginScene());
    IFC(m_pd3dDevice->Clear(
      0,
      NULL,
      D3DCLEAR_TARGET,
      D3DCOLOR_ARGB(128, 128, 128, 128),  // NOTE: Premultiplied alpha!
      1.0f,
      0
    ));

    D3DXMATRIX scalemat;
    D3DXMatrixScaling(&scalemat, 2, 2, 1);
    m_renderSprite->SetTransform(&scalemat);
    IFC(m_pd3dDevice->SetTransform(D3DTS_WORLD, &m_cameraTrans));
    IFC(m_renderSprite->End());
    IFC(m_renderLine->Begin());
    IFC(m_pd3dDevice->SetTransform(D3DTS_VIEW, &m_cameraTrans));
    for (int i = 0; i < lines.size(); i++)
    {
      Line &line = lines[i];
      m_renderLine->SetWidth(line.width);
      m_renderLine->Draw(line.line, 2, line.color);
    }
    IFC(m_renderLine->End());
    lines.clear();

    IFC(m_pd3dDevice->EndScene());
  }
    return hr;
Cleanup:
  return hr;
}