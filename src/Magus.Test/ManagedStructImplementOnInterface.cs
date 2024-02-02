using System;
using System.Linq;
using Bogus;
using Magus.Generated;
using Magus.Generated.Builder;
using MemoryPack;
using NUnit.Framework;

namespace Magus.Test;

[MemoryPackable, MagusTable("ManagedStructImplOnI")]
public partial class ManagedStructImplOnIHolder: IEquatable<ManagedStructImplOnIHolder>
{
    [PrimaryKey] public int Id;
    public ManagedStructImplOnI Value;

    public bool Equals(ManagedStructImplOnIHolder? other)
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
        return Equals((ManagedStructImplOnIHolder)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Value);
    }
}

#if !NET7_0_OR_GREATER
public class ManagedStructFormatter<T> : MemoryPackFormatter<T>
    where T : struct, IManagedStruct<T>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
    {
        IManagedStruct<T>.Serialize(ref writer, ref value);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref T value)
    {
        IManagedStruct<T>.Deserialize(ref reader, ref value);
    }
}
#endif

// TODO: .Net7.0でinterface virtual static memberをinterface default implementationで実装するとHaltになるのでTest出来ない(.Net9.0で直りそう？)
// https://github.com/dotnet/runtime/issues/96855
public interface IManagedStruct<T> : IMemoryPackable<T>
    where T : struct, IManagedStruct<T>
{
    public static T Default { get; } = new T();
    string Value { get; }
    T New(string value);

#if NET7_0_OR_GREATER
    static void IMemoryPackFormatterRegister.RegisterFormatter()
    {
        if (!MemoryPackFormatterProvider.IsRegistered<T>())
        {
            MemoryPackFormatterProvider.Register(new MemoryPack.Formatters.MemoryPackableFormatter<T>());
        }
        if (!MemoryPackFormatterProvider.IsRegistered<T[]>())
        {
            MemoryPackFormatterProvider.Register(new MemoryPack.Formatters.ArrayFormatter<T>());
        }
    }
    
    static IManagedStruct()
    {
        T.RegisterFormatter();
    }
    
    [MemoryPack.Internal.Preserve]
    static void IMemoryPackable<T>.Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref T value)
    {
        writer.WriteString(value!.Value);
    }
    
    [MemoryPack.Internal.Preserve]
    static void IMemoryPackable<T>.Deserialize(ref MemoryPackReader reader,
        scoped ref T value)
    {
        var str = reader.ReadString()!;
        value = Default.New(str);
    }
#else

    static IManagedStruct()
    {
        RegisterFormatter();
    }

    public static void RegisterFormatter()
    {
        if (!MemoryPackFormatterProvider.IsRegistered<T>())
        {
            MemoryPackFormatterProvider.Register(new ManagedStructFormatter<T>());
        }

        if (!MemoryPackFormatterProvider.IsRegistered<T[]>())
        {
            MemoryPackFormatterProvider.Register(new MemoryPack.Formatters.ArrayFormatter<T>());
        }
    }

    [MemoryPack.Internal.Preserve]
    public static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
        where TBufferWriter : class, System.Buffers.IBufferWriter<byte>
    {
        writer.WriteString(value.Value);
    }

    [MemoryPack.Internal.Preserve]
    public static void Deserialize(ref MemoryPackReader reader, scoped ref T value)
    {
        var str = reader.ReadString()!;
        value = Default.New(str);
    }
#endif
}

public struct ManagedStructImplOnI : IManagedStruct<ManagedStructImplOnI>, IEquatable<ManagedStructImplOnI>
{
    public static ManagedStructImplOnI Default { get; } = new ManagedStructImplOnI
    {
        Value = "default"
    };

    public string Value { get; set; }

    public ManagedStructImplOnI New(string value)
    {
        return new ManagedStructImplOnI { Value = value };
    }

    public bool Equals(ManagedStructImplOnI other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ManagedStructImplOnI other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

[TestFixture]
public class ManagedStructImplementOnInterfaceFixture
{
    private readonly Faker<ManagedStructImplOnIHolder> _faker = new Faker<ManagedStructImplOnIHolder>()
        .StrictMode(true)
        .RuleFor(x => x.Id, f => f.UniqueIndex)
        .RuleFor(x => x.Value, f => new ManagedStructImplOnI { Value = f.Random.String2(12) });

    [Test]
    public void ManagedStructImplementOnInterface()
    {
        var values = Enumerable.Range(0, 10).Select(_ => _faker.Generate());
        var target = _faker.Generate();
        var builder = new DatabaseBuilder();
        builder.Append(values.Append(target));
        var binary = builder.Build();
        var master = new MagusDatabase(binary, 1);
        var actual = master.ManagedStructImplOnIHolderTable.FindById(target.Id);
        Assert.That(target, Is.EqualTo(actual));
    }
}