#include "stdafx.h"
#include <d2d1.h>
#include <d2d1helper.h>
#include <d2d1effects_2.h>
#include <dxgi1_2.h>
#include <d3d11.h>
#include <fstream>
#include <set>
#include "SpriteRenderer.h"
#include "IRenderElement.h"

const int C_TILEMAP_SIZE = 128;

struct CUSTOMVERTEX
{
  FLOAT x, y, z;
  FLOAT u, v;
};

CUSTOMVERTEX quad_list[] = {
 {0,0,0, 0,0}, {1,0,0, 1,0}, {0,1,0, 0,1},
 {1,0,0, 1,0}, {1,1,0, 1,1}, {0,1,0, 0,1}
};

float dashes[] = { 2.0f, 1.0f };

D3D11_INPUT_ELEMENT_DESC layout[] =
{
  {"SV_POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0,
    D3D11_INPUT_PER_VERTEX_DATA, 0},
  {"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12,
    D3D11_INPUT_PER_VERTEX_DATA, 0}
};

struct VS_CONSTANT_BUFFER
{
  DirectX::XMMATRIX   mWorldView;
  DirectX::XMMATRIX   mWorld;
  DirectX::XMFLOAT4   fColor;
};

struct PS_TILEMAP_CONSTANT_BUFFER
{
  DirectX::XMINT2   tilemap_size;
  DirectX::XMFLOAT2 tileset_size;
  int               tileset_span;
  // Unused
  DirectX::XMINT3   padding;
};

SpriteRenderer::SpriteRenderer(HWND hwnd)
{
  Init(hwnd);
}

void
SpriteRenderer::Init(HWND hwnd)
{
  HRESULT hr;
  {
    IFC(CoCreateGuid(&m_guid));

    IFC(D2D1CreateFactory(D2D1_FACTORY_TYPE::D2D1_FACTORY_TYPE_SINGLE_THREADED, &m_factory));

    // Create D3D device
    ID3D11Device* device;

    DXGI_SWAP_CHAIN_DESC swapChainDesc = { 0 };

    swapChainDesc.SampleDesc.Count = 1; // Don't use multi-sampling.
    swapChainDesc.SampleDesc.Quality = 0;
    swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    swapChainDesc.BufferCount = 2; // Use double-buffering to minimize latency.
    swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL; // All Windows Runtime apps must use this SwapEffect.
    swapChainDesc.Flags = 0;
    swapChainDesc.OutputWindow = hwnd;
    swapChainDesc.Windowed = true;
    swapChainDesc.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    swapChainDesc.BufferDesc.RefreshRate = { 1, 60 };

    UINT creationFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#if _DEBUG
    creationFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
    IFC(D3D11CreateDeviceAndSwapChain(
      nullptr,                    // specify null to use the default adapter
      D3D_DRIVER_TYPE_HARDWARE,
      0,
      creationFlags,              // optionally set debug and Direct2D compatibility flags
      NULL,              // list of feature levels this app can support
      0,   // number of possible feature levels
      D3D11_SDK_VERSION,
      &swapChainDesc,
      &m_swapChain,
      &device,                    // returns the Direct3D device created
      NULL,            // returns feature level of device created
      &m_3dDeviceContext                    // returns the device immediate context
    ));

    DXGI_SWAP_CHAIN_DESC sDesc;
    m_swapChain->GetDesc(&sDesc);
    m_width = sDesc.BufferDesc.Width;
    m_height = sDesc.BufferDesc.Height;

    IDXGIDevice* dxgiDevice;
    IFC(device->QueryInterface(&dxgiDevice));
    IFC(m_factory->CreateDevice(dxgiDevice, &m_device));
    IFC(m_device->CreateDeviceContext(
      D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
      &m_deviceContext
    )); 
    
    D2D1_BITMAP_PROPERTIES1 bitmapProperties =
      D2D1::BitmapProperties1(
        D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
        D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_IGNORE)
      );

    IDXGISurface* dxgiBackBuffer;
    IFC(m_swapChain->GetBuffer(0, IID_PPV_ARGS(&dxgiBackBuffer)));
    IFC(m_deviceContext->CreateBitmapFromDxgiSurface(dxgiBackBuffer, NULL, &m_targetBitmap));
    m_deviceContext->SetTarget(m_targetBitmap);
    int refs = dxgiBackBuffer->Release();

    // INIT: Initialize shaders
    IFC(m_deviceContext->CreateEffect(CLSID_D2D1Crop, &m_cropEffect));
    IFC(m_deviceContext->CreateEffect(CLSID_D2D1Tint, &m_colorEffect));

    m_colorEffect->SetInputEffect(0, m_cropEffect);

    D3D11_SHADER_RESOURCE_VIEW_DESC rs_desc;;
    rs_desc.Format = DXGI_FORMAT_R8_SINT;
    rs_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
    rs_desc.Texture2D.MipLevels = 1;
    rs_desc.Texture2D.MostDetailedMip = 0;

    IFC(CoCreateInstance(CLSID_WICImagingFactory2, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&m_imageFactory)));

    IFC(m_factory->CreateStrokeStyle(
      D2D1::StrokeStyleProperties(
        D2D1_CAP_STYLE_FLAT,
        D2D1_CAP_STYLE_FLAT,
        D2D1_CAP_STYLE_FLAT,
        D2D1_LINE_JOIN_MITER,
        1.0f,
        D2D1_DASH_STYLE_CUSTOM,
        0.0f),
      dashes,
      ARRAYSIZE(dashes),
      &m_dashedStroke
    ));
  }
  return;
Cleanup:
  throw(hr);
}
HRESULT
SpriteRenderer::Resize(int width, int height)
{
  HRESULT hr = S_OK;
  {
    m_deviceContext->SetTarget(NULL);
    int refs = m_targetBitmap->Release();

    IFC(m_swapChain->ResizeBuffers(2, width, height, DXGI_FORMAT_R8G8B8A8_UNORM, 0));
    m_width = width;
    m_height = height;

    IDXGISurface* dxgiBackBuffer;
    IFC(m_swapChain->GetBuffer(0, IID_PPV_ARGS(&dxgiBackBuffer)));
    IFC(m_deviceContext->CreateBitmapFromDxgiSurface(dxgiBackBuffer, NULL, &m_targetBitmap));
    m_deviceContext->SetTarget(m_targetBitmap);
    refs = dxgiBackBuffer->Release();
  }
Cleanup:
  return hr;
}

void
SpriteRenderer::PrepareForRender()
{
  m_deviceContext->BeginDraw();

  static D2D1_COLOR_F ClearColor = { 0.5f, 0.5f, 0.5f, 1.0f };
  m_deviceContext->Clear(ClearColor);
}

void 
SpriteRenderer::Render(Article* articles, int art_count, Line* lines, int line_count, Tilemap* tilemaps, int tilemap_count)
{
  // Declare an inline comparison function
  auto depth_less = [](IRenderElement* a, IRenderElement* b) { return a->depth > b->depth; };
  // Create a sorted multiset using this comparison function
  std::multiset < IRenderElement*, decltype(depth_less)> elements_sorted(depth_less);

  for (int i = 0; i < art_count; i++)
  {
    elements_sorted.insert(articles + i);
  }
  for (int i = 0; i < line_count; i++)
  {
    elements_sorted.insert(lines + i);
  }
  for (int i = 0; i < tilemap_count; i++)
  {
    elements_sorted.insert(tilemaps + i);
  }

  for (IRenderElement* elem : elements_sorted)
  {
    switch (elem->type)
    {
    case RT_Article:
      RenderArticle(static_cast<Article*>(elem));
      break;
    case RT_Line:
      RenderLine(static_cast<Line*>(elem));
      break;
    case RT_Tilemap:
      RenderTilemap(static_cast<Tilemap*>(elem));
      break;
    default:
      break;
    }
  }

  D2D1_TAG tag1, tag2;
  HRESULT hr = m_deviceContext->EndDraw(&tag1, &tag2);

  hr = m_swapChain->Present(1, 0);
}

void
SpriteRenderer::RenderArticle(Article* article)
{
  HRESULT hr;
  {
    D2D1_MATRIX_3X2_F transmat, scalemat, transform;
    transmat = D2D1::Matrix3x2F::Translation(article->translate.x, article->translate.y);
    scalemat = D2D1::Matrix3x2F::Scale(article->scale.x, article->scale.y);
    transform = scalemat * transmat;
    m_deviceContext->SetTransform(transform * m_cameraTrans);

    m_cropEffect->SetInput(0, article->texture->tex);
    IFC(m_cropEffect->SetValue(D2D1_CROP_PROP_RECT, D2D1::RectF(article->cropStart.x * article->texture->width, article->cropStart.y * article->texture->height, article->cropEnd.x * article->texture->width, article->cropEnd.y * article->texture->height)));
    IFC(m_colorEffect->SetValue(D2D1_TINT_PROP_COLOR, D2D1_VECTOR_4F{ ((article->color & 0x00FF0000) >> 16) / 255.0f, ((article->color & 0x0000FF00) >> 8) / 255.0f, (article->color & 0x000000FF) / 255.0f, ((article->color & 0xFF000000) >> 24) / 255.0f }));
    m_deviceContext->DrawImage(m_colorEffect, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR);
  }
Cleanup:
  hr = hr;
}

void 
SpriteRenderer::RenderLine(Line* line)
{
  HRESULT hr = S_OK;
  {
    m_deviceContext->SetTransform(m_cameraTrans);
    m_deviceContext->DrawLine(D2D1_POINT_2F{ (float)line->start.x, (float)line->start.y }, D2D1_POINT_2F{ (float)line->end.x, (float)line->end.y }, line->color, line->width, NULL);
  }
Cleanup:
  hr = hr;
}

void
SpriteRenderer::RenderTilemap(Tilemap* tilemap)
{
  HRESULT hr = S_OK;
  {
    int x_start = (-m_cameraTrans.dx / m_cameraTrans.m11 - tilemap->translate.x) / (tilemap->tile_width * tilemap->scale.x);
    int y_start = (-m_cameraTrans.dy / m_cameraTrans.m22 - tilemap->translate.y) / (tilemap->tile_height * tilemap->scale.y);
    if (x_start < 0)
      x_start = 0;
    if (y_start < 0)
      y_start = 0;
    int x_end = ((-m_cameraTrans.dx + m_width) / m_cameraTrans.m11 - tilemap->translate.x + tilemap->tile_width * tilemap->scale.x) / (tilemap->tile_width * tilemap->scale.x);
    int y_end = ((-m_cameraTrans.dy + m_height) / m_cameraTrans.m22 - tilemap->translate.y + tilemap->tile_height * tilemap->scale.y) / (tilemap->tile_height * tilemap->scale.y);
    if (x_end > C_TILEMAP_SIZE)
      x_end = C_TILEMAP_SIZE;
    if (y_end > C_TILEMAP_SIZE)
      y_end = C_TILEMAP_SIZE;

    D2D1_MATRIX_3X2_F scalemat = D2D1::Matrix3x2F::Scale(tilemap->scale.x, tilemap->scale.y);


    m_cropEffect->SetInput(0, tilemap->texture->tex);
    IFC(m_colorEffect->SetValue(D2D1_TINT_PROP_COLOR, D2D1_VECTOR_4F{ 1.0f, 1.0f, 1.0f, 1.0f }));

    for (int y = y_start; y < y_end; y++)
    {
      for (int x = x_start; x < x_end; x++)
      {
        int tile = tilemap->map[x + y * C_TILEMAP_SIZE];
        if (tile)
        {
          tile = tile - 1;
          int tileset_x = (tile % (tilemap->texture->width / tilemap->tile_width)) * tilemap->tile_width;
          int tileset_y = (tile / (tilemap->texture->width / tilemap->tile_width)) * tilemap->tile_height;

          D2D1_MATRIX_3X2_F transmat;
          transmat = D2D1::Matrix3x2F::Translation(tilemap->translate.x + (double)x * tilemap->tile_width * tilemap->scale.x - tileset_x * tilemap->scale.x, tilemap->translate.y + (double)y * tilemap->tile_height * tilemap->scale.y - tileset_y * tilemap->scale.y);
          m_deviceContext->SetTransform(scalemat * transmat * m_cameraTrans);
          IFC(m_cropEffect->SetValue(D2D1_CROP_PROP_RECT, D2D1::RectF(tileset_x, tileset_y, tileset_x + tilemap->tile_width, tileset_y + tilemap->tile_height)));

          m_deviceContext->DrawImage(m_colorEffect, D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR);
        }
      }
    }
  }
Cleanup:
  hr = hr;
}

HRESULT SpriteRenderer::CreateBrush(int color, ID2D1SolidColorBrush** brush)
{
  HRESULT hr;
  {
    IFC(m_deviceContext->CreateSolidColorBrush(
      D2D1::ColorF(((color & 0x00FF0000) >> 16) / 255.0f, ((color & 0x0000FF00) >> 8) / 255.0f, (color & 0x000000FF) / 255.0f, ((color & 0xFF000000) >> 24) / 255.0f),
      brush
    ));
  }
Cleanup:
  return hr;
}

HRESULT SpriteRenderer::RegisterTexture(const LPSTR key, const LPWSTR fname, int frames, TextureData** texture)
{
  HRESULT hr = S_OK;
  {
    IWICBitmapDecoder* pDecoder = NULL;
    IWICBitmapFrameDecode* pSource = NULL;
    IWICFormatConverter* pConverter = NULL;
    ID2D1Bitmap* pBitmap;

    HANDLE handle = CreateFileW(fname,
      GENERIC_READ,
      FILE_SHARE_READ,
      NULL,
      OPEN_EXISTING,
      FILE_ATTRIBUTE_NORMAL,
      NULL);
    IFC(m_imageFactory->CreateDecoderFromFileHandle(reinterpret_cast<ULONG_PTR>(handle),
      NULL,
      WICDecodeMetadataCacheOnLoad,
      &pDecoder));

    IFC(pDecoder->GetFrame(0, &pSource));
    IFC(m_imageFactory->CreateFormatConverter(&pConverter));
    IFC(pConverter->Initialize(
      pSource,
      GUID_WICPixelFormat32bppPBGRA,
      WICBitmapDitherTypeNone,
      NULL,
      0.f,
      WICBitmapPaletteTypeMedianCut
    ));
    IFC(m_deviceContext->CreateBitmapFromWicBitmap(
      pConverter,
      NULL,
      &pBitmap
    ));

    D2D1_SIZE_U size = pBitmap->GetPixelSize();
    TextureData* data = new TextureData();
    data->frames = frames;
    data->tex = pBitmap;
    data->width = size.width / frames;
    data->height = size.height;
    if (m_textureAtlas.find(key) == m_textureAtlas.end())
      m_textureAtlas.insert(std::pair<std::string, TextureData*>(key, data));
    else
    {
      delete  m_textureAtlas[key];
      m_textureAtlas[key] = data;
    }
    (*texture) = data;


    pDecoder->Release();
    pSource->Release();
    pConverter->Release();
    CloseHandle(handle);
  }

Cleanup:

  return hr;
}