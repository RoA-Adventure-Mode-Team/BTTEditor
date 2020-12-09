// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

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

extern "C" HRESULT WINAPI GetBackBufferNoRef(IDirect3DSurface9 * *ppSurface)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  IFC(pManager->GetBackBufferNoRef(ppSurface));

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI RegisterTexture(LPSTR key, LPWSTR fname, int frames)
{
  HRESULT hr = S_OK;

  IFC(EnsureRendererManager());

  hr = pManager->RegisterTexture(key, fname, frames);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI StartDraw()
{
  HRESULT hr = S_OK;
  IFC(EnsureRendererManager());
  pManager->StartDraw();
Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI PushSprite(Article art)
{
  HRESULT hr = S_OK;
  IFC(EnsureRendererManager());
  pManager->DrawArticle(art);
Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI PushLine(Vector2 start, Vector2 end, int color, float width)
{
  HRESULT hr = S_OK;
  IFC(EnsureRendererManager());
  pManager->DrawLine(start, end, color, width);
Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetCameraTransform(Vector2 pos, float zoom)
{
  D3DXMATRIX transmat;
  D3DXMatrixTranslation(&transmat, pos.x, pos.y, 0);
  D3DXMATRIX scalemat;
  D3DXMatrixScaling(&scalemat, zoom, zoom, 1);
  pManager->SetCameraTransform(transmat * scalemat);
  return S_OK;
}

extern "C" HRESULT WINAPI Render()
{
  assert(pManager);

  return pManager->Render();
}

extern "C" void WINAPI Destroy()
{
  delete pManager;
  pManager = NULL;
}