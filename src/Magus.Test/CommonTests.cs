using System;
using System.Buffers;
using System.Linq;
using Bogus;
using Generator.Equals;
using Magus.Generated;
using Magus.Generated.Builder;
using Magus.Test.Internal;
using NUnit.Framework;
using MemoryPack;

namespace Magus.Test
{
    public enum Vitamin
    {
        A,
        B,
        C
    }
    
    [MemoryPackable, MagusTable(nameof(User)), Equatable]
    public partial class User
    {
        [PrimaryKey()] public int Id;
        [Index(0)] public Another Another;
        [Index(1, combinedOrder: 0)] public int CombinedId0;
        [Index(1, combinedOrder: 1)] public int CombinedId1;
        public int[] Array;
        public Box Box;
        public ManagedStruct ManagedStruct;
        public Vitamin Vitamin;
    }

    [MemoryPackable, MagusTable(nameof(Item))]
    public partial class Item
    {
        [PrimaryKey] public int Id;
        public string Name;
    }

    [MemoryPackable]
    public partial struct Box
    {
        public int Width;
        public int Height;
    }

    [TestFixture]
    public class CommonTests
    {
        private readonly Faker<User> _userFaker = new Faker<User>()
            .StrictMode(true)
            .RuleFor(v => v.Id, f => f.UniqueIndex)
            .RuleFor(v => v.CombinedId0, f => f.UniqueIndex)
            .RuleFor(v => v.CombinedId1, f => f.UniqueIndex)
            .RuleFor(v => v.Another, f => new Another())
            .RuleFor(v => v.Array, f => new int[] { })
            .RuleFor(v => v.Box, f => new Box { Width = f.Random.Int(), Height = f.Random.Int() })
            .RuleFor(v => v.ManagedStruct, f => new ManagedStruct { Value = f.Random.String2(12) })
            .RuleFor(v => v.Vitamin, f => f.PickRandom(Vitamin.A, Vitamin.B, Vitamin.C));

        [Test]
        public void Test1()
        {
            var users = Enumerable.Range(0, 10).Select(v => _userFaker.Generate());
            var user = _userFaker.Generate();
            var builder = new DatabaseBuilder();
            builder.Append(users.Append(user).ToArray());
            var binary = builder.Build();
            var master = new MagusDatabase(binary, 1);
            var actual = master.UserTable.FindById(user.Id);
            Assert.That(user, Is.EqualTo(actual));
        }

        [Test]
        public void Lazy()
        {
            var builder = new LazyDatabaseBuilder();
            var user = _userFaker.Generate();
            builder.Append(Enumerable.Empty<User>().Append(user));
            builder.Append(Enumerable.Range(0, 10).Select(_ => _userFaker.Generate()));
            var binary = builder.Build();
            var master = new MagusDatabase(binary, 1);
            var actual = master.UserTable.FindById(user.Id);
            Assert.That(user, Is.EqualTo(actual));
        }
    }
}