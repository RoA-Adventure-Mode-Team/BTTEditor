using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RivalsAdventureEditor.Data
{
    public class TilegridArray : IEnumerable<KeyValuePair<Tuple<int, int>, int[,]>>
    {
        public const int ChunkSizeX = 128;
        public const int ChunkSizeY = 128;

        [JsonProperty]
        private readonly Dictionary<Tuple<int, int>, int[,]> chunks = new Dictionary<Tuple<int, int>, int[,]>();
        [JsonProperty]
        private int MinX = 0;
        [JsonProperty]
        private int MaxX = 0;
        [JsonProperty]
        private int MinY = 0;
        [JsonProperty]
        private int MaxY = 0;

        public TilegridArray()
        {
            chunks.Add(new Tuple<int, int>(0, 0), NewChunk());
        }

        public void SetTileAt(Tuple<int, int> index, int value)
        {
            GetChunkFromIndex(index, out var chunk, out var localPos);
            if (!chunks.ContainsKey(chunk))
            {
                chunks.Add(chunk, NewChunk());
                MinX = Math.Min(MinX, chunk.Item1);
                MaxX = Math.Max(MaxX, chunk.Item1);
                MinY = Math.Min(MinY, chunk.Item2);
                MaxY = Math.Max(MaxY, chunk.Item2);
            }
            chunks[chunk][localPos.Item1,localPos.Item2] = value;
        }

        public int GetTileAt(Tuple<int, int> index)
        {
            GetChunkFromIndex(index, out var chunk, out var localPos);
            if (!chunks.ContainsKey(chunk))
                return 0;
            else
                return chunks[chunk][localPos.Item1,localPos.Item2];
        }

        public void GetChunkFromIndex(Tuple<int, int> index, out Tuple<int, int> chunk)
        {
            chunk = new Tuple<int, int>(index.Item1 / ChunkSizeX, index.Item2 / ChunkSizeY);
        }

        public void GetChunkFromIndex(Tuple<int, int> index, out Tuple<int, int> chunk, out Tuple<int, int> localPos)
        {
            GetChunkFromIndex(index, out chunk);
            localPos = new Tuple<int, int>(index.Item1 - chunk.Item1 * ChunkSizeX, index.Item2 - chunk.Item2 * ChunkSizeY);
        }

        private int[,] NewChunk()
        {
            int[,] array = new int[ChunkSizeX,ChunkSizeY];
            return array;
        }

        public IEnumerator<KeyValuePair<Tuple<int, int>, int[,]>> GetEnumerator()
        {
            return chunks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return chunks.GetEnumerator();
        }
    }
}
