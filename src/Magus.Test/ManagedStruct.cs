using System;
using System.Linq;
using Bogus;
using Magus.Generated;
using Magus.Generated.Builder;
using MemoryPack;
using NUnit.Framework;

namespace Magus.Test;

[MemoryPackable, MagusTable("ManagedStruct")]
public partial class ManagedStructHolder : IEquatable<ManagedStructHolder>
{
    [PrimaryKey]
    public int Id;
    public ManagedStruct Value;

    public bool Equals(ManagedStructHolder? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ManagedStructHolder)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Value);
    }
}

public struct ManagedStruct : IMemoryPackable<ManagedStruct>, IEquatable<ManagedStruct>
{
    public string Value;

#if NET7_0_OR_GREATER
    public static void RegisterFormatter()
    {
        if (!MemoryPackFormatterProvider.IsRegistered<ManagedStruct>())
        {
            MemoryPackFormatterProvider.Register(new MemoryPack.Formatters.MemoryPackableFormatter<ManagedStruct>());
        }
        if (!MemoryPackFormatterProvider.IsRegistered<ManagedStruct[]>())
        {
            MemoryPackFormatterProvider.Register(new MemoryPack.Formatters.ArrayFormatter<ManagedStruct>());
        }
    }

    static ManagedStruct()
    {
        MemoryPackFormatterProvider.Register<ManagedStruct>();
    }

    [global::MemoryPack.Internal.Preserve]
    static void IMemoryPackable<ManagedStruct>.Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref ManagedStruct value)
    {
        writer.WriteString(value.Value);
    }

    [global::MemoryPack.Internal.Preserve]
    static void IMemoryPackable<ManagedStruct>.Deserialize(ref MemoryPackReader reader,
        scoped ref ManagedStruct value)
    {
        var str = reader.ReadString()!;
        value = new ManagedStruct { Value = str };
    }
#else
    static ManagedStruct()
    {
        RegisterFormatter();
    }

    [global::MemoryPack.Internal.Preserve]
    public static void RegisterFormatter()
    {
        if (!MemoryPackFormatterProvider.IsRegistered<ManagedStruct>())
        {
            MemoryPackFormatterProvider.Register(new ManagedStructFormatter());
        }

        if (!MemoryPackFormatterProvider.IsRegistered<ManagedStruct[]>())
        {
            MemoryPackFormatterProvider.Register(
                new MemoryPack.Formatters.ArrayFormatter<ManagedStruct>());
        }
    }

    [global::MemoryPack.Internal.Preserve]
    public static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref ManagedStruct value) where TBufferWriter : class, System.Buffers.IBufferWriter<byte>
    {
        writer.WriteString(value.Value);
    }

    [global::MemoryPack.Internal.Preserve]
    public static void Deserialize(ref MemoryPackReader reader, scoped ref ManagedStruct value)
    {
        var str = reader.ReadString()!;
        value = new ManagedStruct { Value = str };
    }

    [global::MemoryPack.Internal.Preserve]
    sealed class ManagedStructFormatter : MemoryPackFormatter<ManagedStruct>
    {
        [global::MemoryPack.Internal.Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref ManagedStruct value)
        {
            ManagedStruct.Serialize(ref writer, ref value);
        }

        [global::MemoryPack.Internal.Preserve]
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ManagedStruct value)
        {
            ManagedStruct.Deserialize(ref reader, ref value);
        }
    }
#endif
    public bool Equals(ManagedStruct other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ManagedStruct other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

[TestFixture]
public class ManagedStructFixture
{
    private readonly Faker<ManagedStructHolder> _faker = new Faker<ManagedStructHolder>()
        .StrictMode(true)
        .RuleFor(v => v.Id, f => f.UniqueIndex)
        .RuleFor(v => v.Value, f => new ManagedStruct { Value = f.Random.String2(12) });

    [Test]
    public void ManagedStruct()
    {
        var values = Enumerable.Range(0, 10).Select(_ => _faker.Generate());
        var target = _faker.Generate();
        var builder = new DatabaseBuilder();
        builder.Append(values.Append(target));
        var binary = builder.Build();
        var master = new MagusDatabase(binary, 1);
        var actual = master.ManagedStructHolderTable.FindById(target.Id);
        Assert.That(target, Is.EqualTo(actual));
    }
}