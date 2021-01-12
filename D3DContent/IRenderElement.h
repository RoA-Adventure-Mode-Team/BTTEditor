#pragma once
#include "stdafx.h"

struct TextureData;
class CRenderer;

struct Vector2
{
  double x;
  double y;
};

enum RenderType
{
  RT_Article = 0,
  RT_Line,
  RT_Tilemap
};

struct IRenderElement
{
  float depth; 
  RenderType type;
};

struct Article : IRenderElement
{
  TextureData* texture;
  Vector2 translate;
  Vector2 scale;
  int color;
};

struct Line : IRenderElement
{
  Vector2 start;
  Vector2 end;
  float width;
  int color;
};

struct Tilemap : IRenderElement
{
  TextureData* texture;
  int tile_width;
  int tile_height;
  Vector2 translate;
  int** map;
};

namespace RenderElement
{
  void RenderArticle(ID3D11Device* device, Article *art);
  void RenderLine(ID3D11Device* device, Line* line);
  void RenderTilemap(ID3D11Device* device, CRenderer* renderer, Tilemap* tilemap);
};