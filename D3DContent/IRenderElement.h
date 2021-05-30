#pragma once
#include "stdafx.h"

struct TextureData;
class CRenderer;
class ID2D1SolidColorBrush;

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
  Vector2 cropStart;
  Vector2 cropEnd;
};

struct Line : IRenderElement
{
  Vector2 start;
  Vector2 end;
  float width;
  ID2D1SolidColorBrush *color;
};

struct Tilemap : IRenderElement
{
  TextureData* texture;
  int tile_width;
  int tile_height;
  Vector2 translate;
  int* map;
  Vector2 scale;
};