using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Magus.Core.Internal;
using MemoryPack;

namespace Magus
{
    public class DatabaseBuilderBase
    {
        private ByteBufferWriter _bufferWriter = new();
        private readonly Dictionary<string, (int offset, int count)> _header = new();
        
        protected void AppendCore<T, TKey>(IEnumerable<T> datasource, Func<T,TKey> selector, IComparer<TKey> comparer)
        {
            var tableName = typeof(T).GetCustomAttribute<MagusTableAttribute>();
            if (tableName == null)
            {
                throw new InvalidOperationException("Table type must be marked with [MagusTableAttribute]");
            }

            if (_header.ContainsKey(tableName.TableName))
            {
                throw new InvalidOperationException($"Table {tableName.TableName} already exists");
            }

            if (datasource == null!) return;
            
            var source = FastSort(datasource, selector, comparer);

            using var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Utf8);
            var writer = new MemoryPackWriter(ref _bufferWriter, state);
            
            var offset = _bufferWriter.CurrentOffset;
            MemoryPackSerializer.Serialize(ref writer, source);
            var count = _bufferWriter.CurrentOffset - offset;
            
            _header.Add(tableName.TableName, (offset, count));
        }
        
        static TElement[] FastSort<TElement, TKey>(IEnumerable<TElement> datasource, Func<TElement, TKey> indexSelector, IComparer<TKey> comparer)
        {
            var collection = datasource as ICollection<TElement>;
            if (collection != null)
            {
                var array = new TElement[collection.Count];
                var sortSource = new TKey[collection.Count];
                var i = 0;
                foreach (var item in collection)
                {
                    array[i] = item;
                    sortSource[i] = indexSelector(item);
                    i++;
                }
                Array.Sort(sortSource, array, 0, collection.Count, comparer);
                return array;
            }
            else
            {
                var array = new ExpandableArray<TElement>(null);
                var sortSource = new ExpandableArray<TKey>(null);
                foreach (var item in datasource)
                {
                    array.Add(item);
                    sortSource.Add(indexSelector(item));
                }

                Array.Sort(sortSource.items, array.items, 0, array.count, comparer);

                Array.Resize(ref array.items, array.count);
                return array.items;
            }
        }

        public async ValueTask<byte[]> BuildAsync()
        {
            using var ms = new MemoryStream();
            await WriteToStreamAsync(ms);
            return ms.ToArray();
        }
        
        public byte[] Build()
        {
            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }

        public async ValueTask WriteToStreamAsync(Stream stream)
        {
            await MemoryPackSerializer.SerializeAsync(stream, _header);
            MemoryMarshal.TryGetArray(_bufferWriter.WrittenMemory, out var segment);
            await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count);
        }
        
        public void WriteToStream(Stream stream)
        {
            MemoryPackSerializer.SerializeAsync(stream, _header).AsTask().Wait();
            MemoryMarshal.TryGetArray(_bufferWriter.WrittenMemory, out var segment);
            stream.Write(segment.Array!, segment.Offset, segment.Count);
        }
    }
}