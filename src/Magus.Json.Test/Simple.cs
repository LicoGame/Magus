using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Serialization;
using MemoryPack;

namespace Magus.Json.Test;

[MemoryPackable, MagusTable(nameof(SimpleClass01))]
public partial class SimpleClass01(int id, string name, int score) : IEquatable<SimpleClass01>
{
    [PrimaryKey] public int Id { get; } = id;
    public string Name { get; } = name;
    public int Score { get; } = score;

    public bool Equals(SimpleClass01? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name && Score == other.Score;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SimpleClass01)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Score);
    }
}

public class Simple
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        // Data
        var expectedData = new SimpleClass01[]
        {
            new(1, "name1", 20),
            new(2, "name2", 100)
        };

        // Expected Data Json
        var expectedJson =
            """
            [
                {
                    "id": 1,
                    "name": "name1",
                    "score": 20
                },
                {
                    "id": 2,
                    "name": "name2",
                    "score": 100
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
                    ("id", new JsonSchemaBuilder().Type(SchemaValueType.Integer).UniqueItems(true).ReadOnly(true)),
                    ("name", new JsonSchemaBuilder().Type(SchemaValueType.String).ReadOnly(true)),
                    ("score", new JsonSchemaBuilder().Type(SchemaValueType.Integer).ReadOnly(true))
                )
                .AdditionalProperties(false)
            )
            .PrimaryKey("id")
            .Build();

        var schemaText = MagusJsonSchema.GenerateArray<SimpleClass01>();
        var schema = JsonSchema.FromText(schemaText);

        Assert.That(schemaText, Is.EqualTo(expectedSchema.ToJsonString()));

        Console.WriteLine(schemaText);

        // Serialization
        ValidatingJsonConverter.MapType<SimpleClass01[]>(schema);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new ValidatingJsonConverter() }
        };
        var jsonText = JsonSerializer.Serialize(expectedData, options);

        // Evaluation
        var node = JsonNode.Parse(expectedJson);
        var result = schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.That(result.IsValid, Is.True);

        // Deserialization
        var actualData = JsonSerializer.Deserialize<SimpleClass01[]>(jsonText, options)!;
        Assert.That(actualData, Is.EquivalentTo(expectedData));
    }
}