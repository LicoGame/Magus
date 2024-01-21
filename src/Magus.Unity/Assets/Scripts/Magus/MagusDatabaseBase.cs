using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace Magus
{
    using Header = System.Collections.Generic.Dictionary<string, (int offset, int count)>;
    public abstract class MagusDatabaseBase
    {
        public MagusDatabaseBase() {}
        public MagusDatabaseBase(byte[] binaryData, int workerSize = 1)
        {
            using var state = MemoryPackReaderOptionalStatePool.Rent(MemoryPackSerializerOptions.Utf8);
            var reader = new MemoryPackReader(binaryData, state);
            var header = reader.ReadValue<Header>()!;
            workerSize = workerSize < 1 ? 1 : workerSize;
            // ReSharper disable once VirtualMemberCallInConstructor
            Init(header, binaryData.AsMemory((int)reader.Consumed), workerSize);
        }

        protected static TView ExtractTable<T, TView>(Header header, ReadOnlyMemory<byte> binary,
            Func<T[], TView> createView)
        {
            var tableName = typeof(T).GetCustomAttribute<MagusTableAttribute>();
            if (tableName == null)
            {
                throw new InvalidOperationException("Table type must be marked with [MagusTableAttribute]");
            }

            if (header.TryGetValue(tableName.TableName, out var segment))
            {
                var slice = binary.Slice(segment.offset, segment.count);
                var data = MemoryPackSerializer.Deserialize<T[]>(slice.Span);
                return createView(data!);
            }

            // empty
            return createView(Array.Empty<T>());
        }
        
        protected abstract void Init(Header header, ReadOnlyMemory<byte> binary, int workerSize);

        public static TableDump<T> Dump<T>(byte[] binary)
        {
            var tableName = typeof(T).GetCustomAttribute<MagusTableAttribute>();
            if (tableName == null)
            {
                throw new InvalidOperationException("Table type must be marked with [MagusTableAttribute]");
            }
            using var state = MemoryPackReaderOptionalStatePool.Rent(MemoryPackSerializerOptions.Utf8);
            var reader = new MemoryPackReader(binary, state);
            var header = reader.ReadValue<Header>()!;

            var (offset, count) = header[tableName.TableName];
            
            return new TableDump<T>(tableName.TableName, count, binary, offset);
        }

        public class TableDump<T>
        {
            public string TableName { get; }
            public int Size { get; }
            private byte[] _data;

            public TableDump(string tableName, int size, byte[]? data, int offset)
            {
                TableName = tableName;
                Size = size;
                if (data != null)
                {
                    _data = new byte[size];
                    Array.Copy(data, offset, _data, 0, size);
                }
            }

            public T Dump()
            {
                return MemoryPackSerializer.Deserialize<T>(_data)!;
            }
        }
    }
}