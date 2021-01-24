// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "IRenderElement.h"
#include "SpriteRenderer.h"

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

static SpriteRenderer* pRenderer = NULL;

extern "C" HRESULT WINAPI Init(HWND hwnd)
{
  try {
    if(pRenderer == NULL)
      pRenderer = new SpriteRenderer(hwnd);
    return S_OK;
  }
  catch (HRESULT hr)
  {
    return hr;
  }
}

extern "C" HRESULT WINAPI RegisterTexture(LPSTR key, LPWSTR fname, int frames, TextureData **texture)
{
  HRESULT hr = S_OK;

  hr = pRenderer->RegisterTexture(key, fname, frames, texture);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetSize(int width, int height)
{
  if(pRenderer)
    return pRenderer->Resize(width, height);
  return S_FALSE;
}

extern "C" HRESULT WINAPI SetCameraTransform(Vector2 pos, float zoom)
{

  // Transform camera from world coordinates into this stupid rendering coords
  D2D1_MATRIX_3X2_F transmat = D2D1::Matrix3x2F::Translation(pos.x, pos.y);
  D2D1_MATRIX_3X2_F scalemat = D2D1::Matrix3x2F::Scale(zoom, zoom);

  pRenderer->SetCameraTrans(transmat * scalemat);
  return S_OK;
}

extern "C" HRESULT WINAPI Render(Article * articles, int count, Line * lines, int line_count, Tilemap * tilemaps, int tilemap_count)
{
  assert(pRenderer);
  pRenderer->Render(articles, count, lines, line_count, tilemaps, tilemap_count);
  return S_OK;
}

extern "C" void WINAPI Destroy()
{
  delete pRenderer;
  pRenderer = NULL;
}