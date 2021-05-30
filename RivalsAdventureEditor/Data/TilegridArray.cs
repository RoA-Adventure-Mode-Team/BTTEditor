using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    public class TilegridArray : IEnumerable<KeyValuePair<Tuple<int, int>, IntPtr>>
    {
        public const int ChunkSizeX = 128;
        public const int ChunkSizeY = 128;

        [JsonProperty]
        public readonly DictionaryProxy Chunks;
        private readonly Dictionary<Tuple<int, int>, IntPtr> chunks = new Dictionary<Tuple<int, int>, IntPtr>();
        [JsonProperty]
        public int MinX { get; private set; } = 0;
        [JsonProperty]
        public int MaxX { get; private set; } = 0;
        [JsonProperty]
        public int MinY { get; private set; } = 0;
        [JsonProperty]
        public int MaxY { get; private set; } = 0;

        public TilegridArray()
        {
            chunks.Add(new Tuple<int, int>(0, 0), NewChunk());
            Chunks = new DictionaryProxy(chunks);
        }

        public unsafe void SetTileAt(Tuple<int, int> index, int value)
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
            ((int*)chunks[chunk].ToPointer())[localPos.Item1 + localPos.Item2 * ChunkSizeX] = value;
        }

        public unsafe int GetTileAt(Tuple<int, int> index)
        {
            GetChunkFromIndex(index, out var chunk, out var localPos);
            if (!chunks.ContainsKey(chunk))
                return 0;
            else
                return ((int*)chunks[chunk].ToPointer())[localPos.Item1 + localPos.Item2 * ChunkSizeX];
        }

        public void GetChunkFromIndex(Tuple<int, int> index, out Tuple<int, int> chunk)
        {
            chunk = new Tuple<int, int>((int)MathF.Floor(index.Item1 / (float)ChunkSizeX), (int)MathF.Floor(index.Item2 / (float)ChunkSizeY));
        }

        public void GetChunkFromIndex(Tuple<int, int> index, out Tuple<int, int> chunk, out Tuple<int, int> localPos)
        {
            GetChunkFromIndex(index, out chunk);
            localPos = new Tuple<int, int>(index.Item1 - chunk.Item1 * ChunkSizeX, index.Item2 - chunk.Item2 * ChunkSizeY);
        }

        private unsafe IntPtr NewChunk()
        {
            IntPtr array = Marshal.AllocHGlobal(ChunkSizeX * ChunkSizeY * sizeof(int));
            RtlZeroMemory(array, ChunkSizeX * ChunkSizeY * sizeof(int));
            return array;
        }

        public IEnumerator<KeyValuePair<Tuple<int, int>, IntPtr>> GetEnumerator()
        {
            return chunks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return chunks.GetEnumerator();
        }

        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, uint length);
    }

    public class DictionaryProxy : IDictionary<string, int[,]>
    {
        public DictionaryProxy(Dictionary<Tuple<int, int>, IntPtr> _chunks)
        {
            chunks = _chunks;
        }

        private readonly Dictionary<Tuple<int, int>, IntPtr> chunks = new Dictionary<Tuple<int, int>, IntPtr>();

        public unsafe int[,] this[string key]
        {
            get
            {
                IntPtr chunkPtr = chunks[ToTuple(key)];
                int[,] chunk = new int[TilegridArray.ChunkSizeX, TilegridArray.ChunkSizeY];
                for (int y = 0; y < TilegridArray.ChunkSizeY; y++)
                {
                    for (int x = 0; x < TilegridArray.ChunkSizeX; x++)
                        chunk[y, x] = ((int*)chunkPtr.ToPointer())[x + y * TilegridArray.ChunkSizeX];
                }
                return chunk;
            }
            set
            {
                var chunk = ToTuple(key);
                if (!chunks.ContainsKey(chunk))
                {
                    chunks.Add(chunk, NewChunk());
                }
                IntPtr chunkPtr = chunks[ToTuple(key)];
                for (int y = 0; y < TilegridArray.ChunkSizeY; y++)
                {
                    for (int x = 0; x < TilegridArray.ChunkSizeX; x++)
                        ((int*)chunkPtr.ToPointer())[x + y * TilegridArray.ChunkSizeX] = value[y, x];
                }
            }
        }

        public ICollection<string> Keys => (from key in chunks.Keys select $"{key.Item1},{key.Item2}").ToList();

        public ICollection<int[,]> Values
        {
            get
            {
                List<int[,]> values = new List<int[,]>();
                foreach(var key in chunks.Keys)
                {
                    values.Add(this[$"{key.Item1},{key.Item2}"]);
                }
                return values;
            }
        }

        public int Count => chunks.Count;

        public bool IsReadOnly => false;

        public void Add(string key, int[,] value)
        {
            var tupKey = ToTuple(key);
            if (!chunks.ContainsKey(tupKey))
                chunks.Add(tupKey, NewChunk());
            this[key] = value;
        }

        public void Add(KeyValuePair<string, int[,]> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            chunks.Clear();
        }

        public bool Contains(KeyValuePair<string, int[,]> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            return chunks.ContainsKey(ToTuple(key));
        }

        public void CopyTo(KeyValuePair<string, int[,]>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, int[,]>> GetEnumerator()
        {
            foreach (var key in Keys)
            {
               yield return KeyValuePair.Create(key, this[key]);
            }
        }

        public bool Remove(string key)
        {
            return chunks.Remove(ToTuple(key));
        }

        public bool Remove(KeyValuePair<string, int[,]> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out int[,] value)
        {
            if (chunks.ContainsKey(ToTuple(key)))
            {
                value = this[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IntPtr NewChunk()
        {
            return Marshal.AllocHGlobal(TilegridArray.ChunkSizeX * TilegridArray.ChunkSizeY * sizeof(int));
        }

        private Tuple<int, int>ToTuple(string input)
        {
            string[] strings = input.Split(",");
            int val1 = Int32.Parse(strings[0].Trim());
            int val2 = Int32.Parse(strings[1].Trim());
            return Tuple.Create(val1, val2);
        }
    }
}
