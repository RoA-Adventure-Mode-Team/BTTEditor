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

extern "C" HRESULT WINAPI InitRenderer(HWND hwnd, SpriteRenderer **outRenderer)
{
  try
  {
    (*outRenderer) = new SpriteRenderer(hwnd);
    return S_OK;
  }
  catch (HRESULT hr)
  {
    return hr;
  }
}

extern "C" HRESULT WINAPI DestroyRenderer(SpriteRenderer* pRenderer)
{
  delete pRenderer;
  return S_OK;
}

extern "C" HRESULT WINAPI CreateBrush(SpriteRenderer * pRenderer, int color, ID2D1SolidColorBrush** brush)
{
  HRESULT hr = S_OK;

  hr = pRenderer->CreateBrush(color, brush);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI RegisterTexture(SpriteRenderer * pRenderer, LPSTR key, LPWSTR fname, int frames, TextureData * *texture)
{
  HRESULT hr = S_OK;

  hr = pRenderer->RegisterTexture(key, fname, frames, texture);

Cleanup:
  return hr;
}

extern "C" HRESULT WINAPI SetSize(SpriteRenderer* pRenderer, int width, int height)
{
  return pRenderer->Resize(width, height);
}

extern "C" HRESULT WINAPI SetCameraTransform(SpriteRenderer* pRenderer, Vector2 pos, float zoom)
{
  // Transform camera from world coordinates into this stupid rendering coords
  D2D1_MATRIX_3X2_F transmat = D2D1::Matrix3x2F::Translation(pos.x, pos.y);
  D2D1_MATRIX_3X2_F scalemat = D2D1::Matrix3x2F::Scale(zoom, zoom);

  pRenderer->SetCameraTrans(transmat * scalemat);
  return S_OK;
}

extern "C" HRESULT WINAPI PrepareForRender(SpriteRenderer* pRenderer)
{
  pRenderer->PrepareForRender();
  return S_OK;
}

extern "C" HRESULT WINAPI Render(SpriteRenderer* pRenderer, Article * articles, int count, Line * lines, int line_count, Tilemap * tilemaps, int tilemap_count)
{
  pRenderer->Render(articles, count, lines, line_count, tilemaps, tilemap_count);
  return S_OK;
}