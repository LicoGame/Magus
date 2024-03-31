using System;
using System.Linq;
using System.Text;
using Magus;
using Magus.Generated;
using Magus.Generated.Builder;
using Magus.Json;
using MemoryPack;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [MemoryPackable, MagusTable(nameof(User))]
    public partial class User : IEquatable<User>
    {
        public User(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [PrimaryKey]
        public int Id { get; }
        
        public string Name { get; }
        
        public int[] Test { get; }

        public bool Equals(User other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((User)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    [MemoryPackable, MagusTable(nameof(Pet))]
    public partial class Pet
    {
        [PrimaryKey]
        public int Id { get; }

        public Toy Toy;
    }

    [MemoryPackable]
    public partial struct Toy
    {
        public string Name;
    }
    
    
    [TestFixture]
    public class SimpleTestFixture
    {
        [Test]
        public void Test01()
        {
            var user = new User(1, "test");
            var users = Enumerable
                .Range(0, 100)
                .Select(x => x + 1000)
                .Select(x => new User(x, $"test{x}"));
            var builder = new DatabaseBuilder();
            builder.Append(users.Append(user).ToArray());
            var bytes = builder.Build();
            var database = new MagusDatabase(bytes, 1);
            var actual = database.UserTable.FindById(user.Id);
            Assert.AreEqual(user, actual);
        }

        [Test]
        public void Test02()
        {
            var pet = new Pet();
            var bytes = MemoryPackSerializer.Serialize(pet);
        }
        
        [Test]
        public void Test03()
        {
            var results = JsonSchemaGenerator.GenerateAll<MagusDatabase>();
            
            foreach (var v in results)
            {
                var text = Encoding.UTF8.GetString(v.Value.ToArray());
                Debug.Log($"Type: {v.Key.Name}\n{text}");
            }
            Assert.Pass();
        }
    }
}