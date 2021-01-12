#include <d3d11.h>
#include <fstream>
#include <set>
#include "SpriteRenderer.h"
#include "IRenderElement.h"

const int C_TILEMAP_SIZE = 128;

struct CUSTOMVERTEX
{
  FLOAT x, y, z;
  DWORD color;
  FLOAT u, v;
};

CUSTOMVERTEX quad_strip[] = {
 {0,0,1, 0xFFFFFFFF, 0,0}, {1,0,1, 0xFFFFFFFF, 1,0},{0,1,1, 0xFFFFFFFF, 0,1},
 {1,1,1, 0xFFFFFFFF, 1,1}
};

D3D11_INPUT_ELEMENT_DESC layout[] =
{
  {"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0,
    D3D11_INPUT_PER_VERTEX_DATA, 0},
  {"COLOR", 0, DXGI_FORMAT_R8G8B8A8_UINT, 0, 12,
    D3D11_INPUT_PER_VERTEX_DATA, 0},
  {"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 16,
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
  DirectX::XMFLOAT2 tilemap_size;
  int               tileset_span;
  DirectX::XMFLOAT2 tileset_size;
};

void BlobFile(std::ifstream& stream, char** buffer_out, int* size_out)
{
  stream.seekg(0, stream.end);
  (*size_out) = stream.tellg();
  stream.seekg(0, stream.beg);
  (*buffer_out) = new char[*size_out];
  stream.read(*buffer_out, *size_out);
}

void
SpriteRenderer::Init()
{
  // INIT: Initialize device
  D3D_FEATURE_LEVEL featureLevel;
  DXGI_SWAP_CHAIN_DESC swap_chain_desc;
  swap_chain_desc.BufferDesc.Width = 0;
  swap_chain_desc.BufferDesc.Height = 0;
  swap_chain_desc.BufferDesc.RefreshRate.Numerator = 1;
  swap_chain_desc.BufferDesc.RefreshRate.Denominator = 60;
  swap_chain_desc.BufferDesc.Format = DXGI_FORMAT_UNKNOWN;
  swap_chain_desc.BufferDesc.ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
  swap_chain_desc.BufferDesc.Scaling = DXGI_MODE_SCALING_UNSPECIFIED;
  D3D11CreateDeviceAndSwapChain(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, D3D11_CREATE_DEVICE_SINGLETHREADED, NULL, 0, D3D11_SDK_VERSION, &swap_chain_desc, &m_swapChain, &m_device, &featureLevel, &m_deviceContext);

  // INIT: Initialize vertex buffer
  D3D11_BUFFER_DESC bufferDesc;
  bufferDesc.Usage = D3D11_USAGE_DEFAULT;
  bufferDesc.ByteWidth = sizeof(CUSTOMVERTEX) * 4;
  bufferDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
  bufferDesc.CPUAccessFlags = 0;
  bufferDesc.MiscFlags = 0;

  D3D11_SUBRESOURCE_DATA initData;
  initData.pSysMem = quad_strip;
  initData.SysMemPitch = 0;
  initData.SysMemSlicePitch = 0;

  m_device->CreateBuffer(&bufferDesc, &initData, &m_vertexBuffer);

  UINT stride = sizeof(CUSTOMVERTEX);
  UINT offset = 0;
  m_deviceContext->IASetInputLayout(m_spriteLayout);
  m_deviceContext->IASetVertexBuffers(0, 1, &m_vertexBuffer, &stride, &offset);
  m_deviceContext->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);

  // INIT: Initialize shaders
  std::ifstream shader_stream;
  char* buffer;
  int len;

  // Vertex Shader
  shader_stream.open("VertexDumper.cso");
  BlobFile(shader_stream, &buffer, &len);
  m_device->CreateVertexShader(buffer, len, NULL, &m_vertexShader);
  m_device->CreateInputLayout(layout, 3, buffer, len, &m_spriteLayout);
  delete[] buffer;
  shader_stream.close();

  // Pixel Shader: Sprites
  shader_stream.open("Sprite_PixelShader.cso");
  BlobFile(shader_stream, &buffer, &len);
  m_device->CreatePixelShader(buffer, len, NULL, &m_spriteShader);
  delete[] buffer;
  shader_stream.close();

  // Pixel Shader: Tilemap
  shader_stream.open("Tilemap_PixelShader.cso");
  BlobFile(shader_stream, &buffer, &len);
  m_device->CreatePixelShader(buffer, len, NULL, &m_tilemapShader);
  delete[] buffer;
  shader_stream.close();

  // INIT: Constant Buffers
  D3D11_BUFFER_DESC g_VSDesc, g_PSTilemapDesc;
  g_VSDesc.ByteWidth = sizeof(VS_CONSTANT_BUFFER);
  g_VSDesc.Usage = D3D11_USAGE_DYNAMIC;
  g_VSDesc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
  g_VSDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
  g_VSDesc.MiscFlags = 0;
  g_VSDesc.StructureByteStride = 0;
  m_device->CreateBuffer(&g_VSDesc, NULL, &m_VSConstantBuffer);
  m_deviceContext->VSSetConstantBuffers(0, 1, &m_VSConstantBuffer);

  g_PSTilemapDesc.ByteWidth = sizeof(PS_TILEMAP_CONSTANT_BUFFER);
  g_PSTilemapDesc.Usage = D3D11_USAGE_DYNAMIC;
  g_PSTilemapDesc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
  g_PSTilemapDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
  g_PSTilemapDesc.MiscFlags = 0;
  g_PSTilemapDesc.StructureByteStride = 0;
  m_device->CreateBuffer(&g_PSTilemapDesc, NULL, &m_PSTilemapConstantBuffer);
  m_deviceContext->PSSetConstantBuffers(0, 1, &m_PSTilemapConstantBuffer);

  // INIT: Sampler State
  D3D11_SAMPLER_DESC samplerDesc;
  samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
  samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
  samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;

  m_device->CreateSamplerState(&samplerDesc, &m_samplerState);
  // Set both samplers to the same sampler state
  m_deviceContext->PSSetSamplers(0, 1, &m_samplerState);
  m_deviceContext->PSSetSamplers(1, 1, &m_samplerState);

  // INIT: Tilemap texture
  D3D11_TEXTURE2D_DESC texture_desc;
  texture_desc.Width = C_TILEMAP_SIZE;
  texture_desc.Height = C_TILEMAP_SIZE;
  texture_desc.MipLevels = 0;
  texture_desc.Format = DXGI_FORMAT_R32_SINT;
  texture_desc.SampleDesc.Count = 1;
  texture_desc.Usage = D3D11_USAGE_DYNAMIC;
  texture_desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
  texture_desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
  texture_desc.MiscFlags = 0;

  m_device->CreateTexture2D(&texture_desc, NULL, &m_tilemapTex);

  // INIT: Resource Views
  m_device->CreateShaderResourceView(m_tilemapTex, NULL, &m_tilemapTexView);
  m_deviceContext->PSSetShaderResources(1, 1, &m_tilemapTexView);
}

void
SpriteRenderer::PrepareDraw()
{
  m_deviceContext->VSSetShader(m_vertexShader, nullptr, 0);
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

  m_swapChain->Present(0, 0);
}

void
SpriteRenderer::RenderArticle(Article* article)
{
  m_deviceContext->PSSetShader(m_spriteShader, nullptr, 0);

  DirectX::XMMATRIX transmat, scalemat, transform;
  transmat = DirectX::XMMatrixTranslation(article->translate.x, article->translate.y, 1);
  scalemat = DirectX::XMMatrixScaling(article->scale.x * article->texture->width, article->scale.y * article->texture->height, 1);
  transform = scalemat * transmat;

  VS_CONSTANT_BUFFER VSConstData;
  VSConstData.mWorldView = m_cameraTrans;
  VSConstData.mWorld = transform;
  VSConstData.fColor = DirectX::XMFLOAT4((article->color & 0x00FF0000) / 255.0f, (article->color & 0x0000FF00) / 255.0f, (article->color & 0x000000FF) / 255.0f, (article->color & 0xFF000000) / 255.0f);
  m_deviceContext->UpdateSubresource(m_VSConstantBuffer, 0, NULL, &VSConstData, 0, 0);

  m_device->CreateShaderResourceView(article->texture->tex, NULL, &m_textureView);
  m_deviceContext->PSSetShaderResources(0, 1, &m_textureView);

  m_deviceContext->Draw(4, 0);
  m_textureView->Release();
}

void 
SpriteRenderer::RenderLine(Line* line)
{
  m_deviceContext->PSSetShader(m_spriteShader, nullptr, 0);

  Vector2 lineVec = { line->end.x - line->start.x, line->start.y - line->end.y };
  float len = sqrt(lineVec.x * lineVec.x + lineVec.y * lineVec.y);
  float rot = acos(lineVec.x / len);
  DirectX::XMMATRIX lengthWidth, offset, rotation, transmat, transform;
  lengthWidth = DirectX::XMMatrixScaling(len, line->width, 1);
  offset = DirectX::XMMatrixTranslation(len / 2, 0, 0);
  rotation = DirectX::XMMatrixRotationZ(rot);
  transmat = DirectX::XMMatrixTranslation(line->start.x, line->start.y, 1);
  transform = transmat * rotation * offset * lengthWidth;

  VS_CONSTANT_BUFFER VSConstData;
  VSConstData.mWorldView = m_cameraTrans;
  VSConstData.mWorld = transform;
  VSConstData.fColor = DirectX::XMFLOAT4((line->color & 0x00FF0000) / 255.0f, (line->color & 0x0000FF00) / 255.0f, (line->color & 0x000000FF) / 255.0f, (line->color & 0xFF000000) / 255.0f);
  m_deviceContext->UpdateSubresource(m_VSConstantBuffer, 0, NULL, &VSConstData, 0, 0);

  // Set a NULL texture (may have to do something else this probably crashes)
 /* m_device->CreateShaderResourceView(NULL, NULL, &m_textureView);
  m_deviceContext->PSSetShaderResources(0, 1, &m_textureView);

  m_deviceContext->Draw(4, 0);
  m_textureView->Release();*/

}

void
SpriteRenderer::RenderTilemap(Tilemap* tilemap)
{
  m_deviceContext->PSSetShader(m_tilemapShader, nullptr, 0);
  m_deviceContext->UpdateSubresource(m_tilemapTex, 0, NULL, tilemap->map, C_TILEMAP_SIZE, 0);

  DirectX::XMMATRIX transmat, scalemat, transform;
  transmat = DirectX::XMMatrixTranslation(tilemap->translate.x, tilemap->translate.y, 1);
  scalemat = DirectX::XMMatrixScaling(C_TILEMAP_SIZE * tilemap->tile_width, C_TILEMAP_SIZE * tilemap->tile_height, 1);
  transform = scalemat * transmat;

  VS_CONSTANT_BUFFER VSConstData;
  VSConstData.mWorldView = m_cameraTrans;
  VSConstData.mWorld = transform;
  VSConstData.fColor = DirectX::XMFLOAT4(1, 1, 1, 1);
  m_deviceContext->UpdateSubresource(m_VSConstantBuffer, 0, NULL, &VSConstData, 0, 0);

  PS_TILEMAP_CONSTANT_BUFFER PSConstData;
  PSConstData.tilemap_size = { 1.0f / C_TILEMAP_SIZE, 1.0f / C_TILEMAP_SIZE };
  PSConstData.tileset_span = tilemap->texture->width / tilemap->tile_width;
  PSConstData.tileset_size = { 1.0f / tilemap->tile_width, 1.0f / tilemap->tile_height };
  m_deviceContext->UpdateSubresource(m_PSTilemapConstantBuffer, 0, NULL, &PSConstData, 0, 0);

  m_device->CreateShaderResourceView(tilemap->texture->tex, NULL, &m_textureView);
  m_deviceContext->PSSetShaderResources(0, 1, &m_textureView);

  m_deviceContext->Draw(4, 0);
  m_textureView->Release();
}