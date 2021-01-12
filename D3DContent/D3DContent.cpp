// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "IRenderElement.h"

BOOL APIENTRY DllMain(HMODULE hModule,
  DWORD  ul_reason_for_call,
  LPVOID lpReserved
)
{
  switch (ul_reason_for_call)
  {
  case DLL_PROCESS_ATTACH:
  case DLL_THREAD_ATTACH:
  case DLL_THREAD_DETACH:
  case DLL_PROCESS_DETACH:
    break;
  }
  return TRUE;
}

static CRendererManager* pManager = NULL;

static HRESULT EnsureRendererManager()
{
  return pManager ? S_OK : CRendererManager::Create(&pManager);
}

extern "C" HRESULT WINAPI SetSize(UINT uWidth, UINT uHeight)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  pManager->SetSize(uWidth, uHeight);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetAlpha(BOOL fUseAlpha)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  pManager->SetAlpha(!!fUseAlpha);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetNumDesiredSamples(UINT uNumSamples)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  pManager->SetNumDesiredSamples(uNumSamples);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetAdapter(POINT screenSpacePoint)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  pManager->SetAdapter(screenSpacePoint);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI GetBackBufferNoRef(IDirect3DSurface9 **ppSurface)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  IFC(pManager->GetBackBufferNoRef(ppSurface));

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI RegisterTexture(LPSTR key, LPWSTR fname, int frames, TextureData **texture)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  hr = pManager->RegisterTexture(key, fname, frames, texture);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI StartDraw()
{
  HRESULT hr = S_OK;
  IFC(EnsureRendererManager());
Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetCameraTransform(Vector2 pos, float zoom)
{
  D3DSURFACE_DESC desc;
  IDirect3DSurface9* surf;
  pManager->GetBackBufferNoRef(&surf);
  surf->GetDesc(&desc);

  // Transform camera from world coordinates into this stupid rendering coords
  D3DXMATRIX transmat;
  D3DXMatrixTranslation(&transmat, pos.x * (2 * zoom / desc.Width) - 1, pos.y * -(2 * zoom / desc.Height) + 1, 0);
  D3DXMATRIX scalemat;
  D3DXMatrixScaling(&scalemat, 2* zoom / desc.Width, -2 *zoom / desc.Height, 1);

  pManager->SetCameraTransform(scalemat * transmat);
  return S_OK;
}

extern "C" HRESULT WINAPI Render(Article * articles, int count, Line * lines, int line_count, Tilemap * tilemaps, int tilemap_count)
{
  assert(pManager);

  pManager->PushArticles(articles, count);
  pManager->PushLines(lines, line_count);
  pManager->PushTilemaps(tilemaps, tilemap_count);
  return pManager->Render();
}

extern "C" void WINAPI Destroy()
{
  delete pManager;
  pManager = NULL;
}