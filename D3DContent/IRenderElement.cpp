#include "stdafx.h"
#include "IRenderElement.h"
#include <d3dcompiler.h>
#include <fstream>

struct CUSTOMVERTEX
{
  FLOAT x, y, z;
  DWORD color;
  FLOAT u, v;
};

ID3D11PixelShader* g_TilemapShader = NULL;

namespace RenderElement
{
  void RenderArticle(ID3D11Device* device, Article *art)
  {
    device->SetTexture(0, art->texture->tex);
    RECT bounds;
    bounds.top = 0;
    bounds.left = 0;
    bounds.right = art->texture->width / art->texture->frames;
    bounds.bottom = art->texture->height;
    D3DXMATRIX transmat, scalemat, transform;
    D3DXMatrixTranslation(&transmat, (float)art->translate.x, (float)art->translate.y, 1);
    D3DXMatrixScaling(&scalemat, art->scale.x * bounds.right, art->scale.y * bounds.bottom, -1);
    transform = scalemat * transmat;
    device->SetTransform(D3DTS_WORLD, &transform); 
    CUSTOMVERTEX quad_strip[] = {
     {0,0,1, art->color, 0,0}, {1,0,1, art->color, 1.0f / art->texture->frames,0},{0,1,1, art->color, 0,1},
     {1,1,1, art->color, 1.0f / art->texture->frames,1}
    };
    device->DrawPrimitiveUP(D3DPT_TRIANGLESTRIP, 2, quad_strip, sizeof(quad_strip[0]));
  }

  void RenderLine(ID3D11Device* device, Line* line)
  {
    Vector2 dir = { line->end.x - line->start.x, line->end.y - line->start.y };
    double len = sqrt(dir.x * dir.x + dir.y * dir.y);
    Vector2 nrm = { -dir.y / len, dir.x / len };
    device->SetTexture(0, NULL);
    D3DXMATRIX transform;
    D3DXMatrixIdentity(&transform);
    device->SetTransform(D3DTS_WORLD, &transform);
    float width = line->width;
    CUSTOMVERTEX line_strip[] = {
     {line->start.x + nrm.x * width,line->start.y + nrm.y * width,1, line->color, 0,0}, {line->end.x + nrm.x * width,line->end.y + nrm.y * width,1, line->color,1,0},
     {line->start.x - nrm.x * width,line->start.y - nrm.y * width,1, line->color, 0,0}, {line->end.x - nrm.x * width,line->end.y - nrm.y * width,1, line->color,1,0},
    };
    device->DrawPrimitiveUP(D3DPT_TRIANGLESTRIP, 2, line_strip, sizeof(line_strip[0]));
  }

  void RenderTilemap(ID3D11Device* device, CRenderer* renderer, Tilemap *tilemap)
  {
    device->SetTexture(0, tilemap->texture->tex);
    device->SetTexture(1, tilemap_tex);

    device->Draw(D3DPT_TRIANGLESTRIP, 2, quad_strip, sizeof(quad_strip[0]));

    device->SetPixelShader(NULL);
  }
}