using System;
using System.Linq;
using Bogus;
using Magus.Generated;
using Magus.Generated.Builder;
using MemoryPack;
using NUnit.Framework;

namespace Magus.Test;

[MemoryPackable, MagusTable(nameof(PrimaryAndCombinedIndex))]
public partial class PrimaryAndCombinedIndex : IEquatable<PrimaryAndCombinedIndex>
{
    [PrimaryKey, Index(1, 0)]
    public int A;
    [Index(0), Index(1, 1)]
    public int B;

    public bool Equals(PrimaryAndCombinedIndex? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return A == other.A && B == other.B;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PrimaryAndCombinedIndex)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B);
    }
}

[TestFixture]
public class ComplexTests
{
    private readonly Faker<PrimaryAndCombinedIndex> _faker = new Faker<PrimaryAndCombinedIndex>()
        .RuleFor(x => x.A, faker => faker.UniqueIndex)
        .RuleFor(x => x.B, faker => faker.UniqueIndex);

    [Test]
    public void PrimaryKey()
    {
        var data = Enumerable.Range(0, 20).Select(_ => _faker.Generate()).ToArray();
        var expected = data.First();
        var builder = new DatabaseBuilder();
        builder.Append(data);
        var binary = builder.Build();
        var master = new MagusDatabase(binary, 1);
        var actual = master.PrimaryAndCombinedIndexTable.FindByA(expected.A);
        Assert.That(expected, Is.EqualTo(actual));
    }
    
    [Test]
    public void Index()
    {
        var data = Enumerable.Range(0, 20).Select(_ => _faker.Generate()).ToArray();
        var expected = data.First();
        var builder = new DatabaseBuilder();
        builder.Append(data);
        var binary = builder.Build();
        var master = new MagusDatabase(binary, 1);
        var actual = master.PrimaryAndCombinedIndexTable.FindByB(expected.B);
        Assert.That(expected, Is.EqualTo(actual));
    }
    
    [Test]
    public void CombinedIndex()
    {
        var data = Enumerable.Range(0, 20).Select(_ => _faker.Generate()).ToArray();
        var expected = data.First();
        var builder = new DatabaseBuilder();
        builder.Append(data);
        var binary = builder.Build();
        var master = new MagusDatabase(binary, 1);
        var actual = master.PrimaryAndCombinedIndexTable.FindByB_A((expected.B, expected.A));
        Assert.That(expected, Is.EqualTo(actual));
    }
}