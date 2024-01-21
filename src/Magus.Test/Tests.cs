using System;
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
    [MemoryPackable, MagusTable(nameof(User)), Equatable]
    public partial class User
    {
        [PrimaryKey()] public int Id;
        [Index(0)] public Another Another;
        [Index(1, combinedOrder: 0)] public int CombinedId0;
        [Index(1, combinedOrder: 1)] public int CombinedId1;
    }

    [MemoryPackable, MagusTable(nameof(Item))]
    public partial class Item
    {
        [PrimaryKey] public int Id;
        public string Name;
    }

    [TestFixture]
    public class Tests
    {
        private readonly Faker<User> _userFaker = new Faker<User>()
            .StrictMode(true)
            .RuleFor(v => v.Id, f => f.UniqueIndex)
            .RuleFor(v => v.CombinedId0, f => f.UniqueIndex)
            .RuleFor(v => v.CombinedId1, f => f.UniqueIndex)
            .RuleFor(v => v.Another, f => new Another());
        
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
            Assert.AreEqual(user, actual);
        }
    }
}