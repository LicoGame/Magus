using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Humanizer;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;
using Json.Schema.Serialization;
using MemoryPack;

namespace Magus.Json.Test;

public interface IStructId
{
    int Id { get; set; }
}

public struct StructId : IEquatable<StructId>, IComparable<StructId>, IStructId
{
    public int Id { get; set; }

    public static implicit operator StructId(int id) => new StructId { Id = id };

    public static implicit operator int(StructId id) => id.Id;

    public bool Equals(StructId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is StructId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public int CompareTo(StructId other)
    {
        return Id.CompareTo(other.Id);
    }
}

public enum Something
{
    TypeA,
    TypeB,
    TypeC
}

[MemoryPackable, MagusTable(nameof(ComplexClass01))]
public partial class ComplexClass01(StructId id, string name, Something something) : IEquatable<ComplexClass01>
{
    [PrimaryKey] public StructId Id { get; set; } = id;
    public string Name { get; set; } = name;

    public Something Something { get; set; } = Something.TypeA;

    public bool Equals(ComplexClass01? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id) && Name == other.Name && Something == other.Something;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ComplexClass01)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Something);
    }
}

public class StructIdJsonGenerator : ISchemaGenerator
{
    public bool Handles(Type type)
    {
        return typeof(IStructId).IsAssignableFrom(type);
    }

    public void AddConstraints(SchemaGenerationContextBase context)
    {
        context.Intents.Add(new TypeIntent(SchemaValueType.String));
    }
}

public class StructIdJsonConverter : JsonConverter<IStructId>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IStructId).IsAssignableFrom(typeToConvert);
    }

    public override IStructId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        var id = int.Parse(reader.GetString()!);
        return new StructId
        {
            Id = id
        };
    }

    public override void Write(Utf8JsonWriter writer, IStructId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id.ToString());
    }
}

public class Complex
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void WithGenerator()
    {
        // Data
        var expectedData = new ComplexClass01[]
        {
            new(1, "name1", Something.TypeB),
            new(2, "name2", Something.TypeC)
        };

        // Expected Data Json
        var expectedJson =
            """
            [
                {
                    "id": "1",
                    "name": "name1",
                    "something": "TypeB"
                },
                {
                    "id": "2",
                    "name": "name2",
                    "something": "TypeC"
                }
            ]
            """;

        // Expected Schema
        var expectedSchema = new JsonSchemaBuilder()
            .Schema(MagusMetaSchemas.RelationExtId)
            .Type(SchemaValueType.Array)
            .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("id", new JsonSchemaBuilder().Type(SchemaValueType.String).UniqueItems(true)),
                    ("name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                    ("something", new JsonSchemaBuilder().Enum(Enum.GetValues<Something>().Select(x => x.ToString())))
                )
                .AdditionalProperties(false)
            )
            .PrimaryKey("id")
            .Build();

        // Generation
        var schemaText =
            MagusJsonSchema.GenerateArray<ComplexClass01>(generators: new[] { new StructIdJsonGenerator() });
        var schema = MagusJsonSchema.FromText(schemaText);

        Assert.That(schemaText, Is.EqualTo(expectedSchema.ToJsonString()));

        Console.WriteLine(schemaText);

        // Serialization
        ValidatingJsonConverter.MapType<ComplexClass01[]>(schema);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new StructIdJsonConverter(),
                new JsonStringEnumConverter(),
                new ValidatingJsonConverter(),
            }
        };
        var jsonText = JsonSerializer.Serialize(expectedData, options);

        // Evaluation
        var node = JsonNode.Parse(expectedJson);
        var result = schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
        if (result.IsValid == false)
        {
            Console.WriteLine(result.ToJsonString());
        }

        Assert.That(result.IsValid, Is.True);

        // Deserialization
        var actualData = JsonSerializer.Deserialize<ComplexClass01[]>(jsonText, options)!;
        Assert.That(actualData, Is.EquivalentTo(expectedData));
    }
}