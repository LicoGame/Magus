using System;
// ReSharper disable once RedundantUsingDirective
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
// ReSharper disable once RedundantUsingDirective
using System.Runtime.CompilerServices;
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
            
            var source = Helper.FastSort(datasource, selector, comparer);

            using var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Utf8);
            var writer = new MemoryPackWriter<ByteBufferWriter>(ref _bufferWriter, state);
            
            var offset = _bufferWriter.CurrentOffset;
            MemoryPackSerializer.Serialize(ref writer, source);
            var count = _bufferWriter.CurrentOffset - offset;
            
            _header.Add(tableName.TableName, (offset, count));
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
            await OnPreprocessAsync();
            await MemoryPackSerializer.SerializeAsync(stream, _header);
            MemoryMarshal.TryGetArray(_bufferWriter.WrittenMemory, out var segment);
            await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count);
        }
        
        public void WriteToStream(Stream stream)
        {
            OnPreprocessAsync().AsTask().Wait();
            MemoryPackSerializer.SerializeAsync(stream, _header).AsTask().Wait();
            MemoryMarshal.TryGetArray(_bufferWriter.WrittenMemory, out var segment);
            stream.Write(segment.Array!, segment.Offset, segment.Count);
        }
        
        protected virtual ValueTask OnPreprocessAsync() => default;
    }
}