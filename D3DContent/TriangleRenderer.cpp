//+-----------------------------------------------------------------------------
//
//  CTriangleRenderer
//
//      Subclass of CRenderer that renders a single, spinning triangle
//
//------------------------------------------------------------------------------

#include "stdafx.h"
#include "IRenderElement.h"
#include <set>

struct CUSTOMVERTEX
{
  FLOAT x, y, z;
  DWORD color;
  FLOAT u, v;
};
const CUSTOMVERTEX quad_strip[] = {
   {0,0,1, 0xFFFFFFFF, 0,0}, {1,0,1, 0xFFFFFFFF, 1,0},{0,1,1, 0xFFFFFFFF, 0,1},
   {1,1,1, 0xFFFFFFFF, 1,1}
};

#define D3DFVF_CUSTOMVERTEX (D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX0)

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

    // Call base to create the device and render target
    IFC(CRenderer::Init(pD3D, pD3DEx, hwnd, uAdapter));

    // Set up the global state
    IFC(m_pd3dDevice->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_LIGHTING, FALSE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_FOGENABLE, FALSE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_CLIPPING, FALSE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_ZENABLE, D3DZB_FALSE));

    IFC(m_pd3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA));
    IFC(m_pd3dDevice->SetRenderState(D3DRS_BLENDOP, D3DBLENDOP_ADD)); 
    IFC(m_pd3dDevice->SetRenderState(D3DRS_DIFFUSEMATERIALSOURCE, D3DMCS_COLOR1));

    IFC(m_pd3dDevice->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_POINT));

    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE));
    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE));
    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_COLORARG2, D3DTA_DIFFUSE));
    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_MODULATE));
    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE));
    IFC(m_pd3dDevice->SetTextureStageState(0, D3DTSS_ALPHAARG2, D3DTA_DIFFUSE));

    // Create a vertex buffer to hold the quad verticies
    void* pVerts;
    IFC(m_pd3dDevice->CreateVertexBuffer(sizeof(quad_strip) * sizeof(_TRIVERTEX), 0, D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX1, D3DPOOL_DEFAULT, &m_quad_buffer, NULL));
    IFC(m_quad_buffer->Lock(0, sizeof(quad_strip) * sizeof(_TRIVERTEX), &pVerts, 0));
    memcpy(pVerts, quad_strip, sizeof(quad_strip) * sizeof(_TRIVERTEX));
    m_quad_buffer->Unlock();

    m_pd3dDevice->CreateVertexDeclaration(g_VBDecl_Geometry, &m_pVertexDeclaration);
    m_pd3dDevice->SetVertexDeclaration(m_pVertexDeclaration);

    m_articles = nullptr;
    m_article_count = 0;
  }
Cleanup:
  return hr;
}

HRESULT
CTriangleRenderer::Render()
{
  HRESULT state = CheckDeviceState();
  HRESULT hr = S_OK;
    {

    IFC(m_pd3dDevice->Clear(
      0,
      NULL,
      D3DCLEAR_TARGET,
      D3DCOLOR_ARGB(128, 128, 128, 128),  // NOTE: Premultiplied alpha!
      1.0f,
      0
    ));
    IFC(m_pd3dDevice->BeginScene());
    IFC(m_pd3dDevice->SetTransform(D3DTS_VIEW, &m_cameraTrans));

    // Declare an inline comparison function
    auto depth_less = [](IRenderElement* a, IRenderElement* b) { return a->depth > b->depth; };
    // Create a sorted multiset using this comparison function
    std::multiset < IRenderElement*, decltype(depth_less)> elements_sorted(depth_less);

    for (int i = 0; i < m_article_count; i++)
    {
      elements_sorted.insert(m_articles + i);
    }
    for (int i = 0; i < m_line_count; i++)
    {
      elements_sorted.insert(m_lines + i);
    }
    for (int i = 0; i < m_tilemap_count; i++)
    {
      elements_sorted.insert(m_tilemaps + i);
    }

    for (IRenderElement* elem : elements_sorted)
    {
      switch (elem->type)
      {
      case RT_Article:
        RenderElement::RenderArticle(m_pd3dDevice, static_cast<Article *>(elem));
        break;
      case RT_Line:
        RenderElement::RenderLine(m_pd3dDevice, static_cast<Line*>(elem));
        break;
      case RT_Tilemap:
        RenderElement::RenderTilemap(m_pd3dDevice, this, static_cast<Tilemap*>(elem));
        break;
      default:
        break;
      }
    }

    IFC(m_pd3dDevice->EndScene());
  }
    return hr;
Cleanup:
  return hr;
}